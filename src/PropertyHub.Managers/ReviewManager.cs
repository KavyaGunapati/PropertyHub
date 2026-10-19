using Microsoft.EntityFrameworkCore;
using PropertyHub.DataAccess.Context;
using PropertyHub.DataAccess.Entities;
using PropertyHub.DataAccess.Enums;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Models.DTOs;
using PropertyHub.Services.Validators;

namespace PropertyHub.Managers
{
    public class ReviewManager:IReviewManager
    {
        private readonly AppDbContext _context;
        private const int MaxPageSize=30;
        private readonly ReviewValidator _validator;

        public ReviewManager(AppDbContext context,ReviewValidator validator)
        {
            _context=context;
            _validator=validator;
        }
        public async Task<Result<ReviewResponse>> SubmitReviewAsync(int propertyId, int userId, CreateReviewRequest request, CancellationToken cancellationToken = default)
        {
            var validation=await _validator.ValidateAsync(request,cancellationToken);
            if (!validation.IsValid)
            {
                return Result<ReviewResponse>.Failure(string.Join(", ",validation.Errors.Select(e=>e.ErrorMessage)));
            }
            var property=await _context.Properties.FirstOrDefaultAsync(p=>p.Id==propertyId,cancellationToken);
            if (property == null || property.Status!=PropertyStatus.Published)
            {
                return Result<ReviewResponse>.Failure("Property Not found");
            }
            if (property.AgentId == userId)
            {
                return Result<ReviewResponse>.Failure("You can't review your own property");
            }
            var alreadyReviewed=await _context.Reviews.AnyAsync(p=>p.UserId==userId && p.PropertyId==propertyId,cancellationToken);
            if (alreadyReviewed)
            {
                return Result<ReviewResponse>.Failure("You have already reviewd this Property");
            }
            var review=new Review
            {
                PropertyId = propertyId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
            };
            _context.Reviews.Add(review);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return Result<ReviewResponse>.Failure("You have already reviewed this property");
            }
            var reviewerName=await _context.Users.Where(u=>u.Id==userId)
            .Select(u=>u.UserName)
            .FirstAsync(cancellationToken);
            return Result<ReviewResponse>.Success(new ReviewResponse
            {
                Id = review.Id,
                PropertyId = review.PropertyId,
                ReviewerName = reviewerName,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            });
        }
        public async Task<Result<List<ReviewResponse>>> GetReviewsAsync(int propertyId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
                return Result<List<ReviewResponse>>.Failure("pageNumber must be 1 or greater");
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            var propertyPublished=await _context.Properties.AnyAsync(p=>p.Id==propertyId && p.Status==PropertyStatus.Published,cancellationToken);
            if (!propertyPublished)
            {
                return Result<List<ReviewResponse>>.Failure("Property not found");
            }
            var reviews= await _context.Reviews.Where(r=>r.PropertyId==propertyId)
            .OrderByDescending(r=>r.CreatedAt)
            .ThenByDescending(r=>r.Id)
            .Skip((pageNumber-1)*pageSize)
            .Take(pageSize)
            .Select(r=> new ReviewResponse
            {
                Id = r.Id,
                PropertyId = r.PropertyId,
                ReviewerName = r.User.UserName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToListAsync(cancellationToken);
            return Result<List<ReviewResponse>>.Success(reviews);
        }
    }
}