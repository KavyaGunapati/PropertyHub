using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        private readonly IMapper _mapper;
        private readonly ILogger<ReviewManager> _logger;
        private readonly ICacheService _cacheService;

        public ReviewManager(AppDbContext context,ReviewValidator validator,IMapper mapper,ILogger<ReviewManager> logger,ICacheService cacheService)
        {
            _context=context;
            _mapper=mapper;
            _logger=logger;
            _cacheService=cacheService;
            _validator=validator;
        }
        public async Task<Result<ReviewResponse>> SubmitReviewAsync(int propertyId, int userId, CreateReviewRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("User {UserId} submitting review for property {PropertyId}", userId, propertyId);
            var validation=await _validator.ValidateAsync(request,cancellationToken);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Review by user {UserId} for property {PropertyId} failed validation", userId, propertyId);
                return Result<ReviewResponse>.Failure(string.Join(", ",validation.Errors.Select(e=>e.ErrorMessage)));
            }
            var property=await _context.Properties.FirstOrDefaultAsync(p=>p.Id==propertyId,cancellationToken);
            if (property == null || property.Status!=PropertyStatus.Published)
            {
                _logger.LogWarning("Review rejected: property {PropertyId} not found or not published", propertyId);
                return Result<ReviewResponse>.Failure("Property Not found");
            }
            if (property.AgentId == userId)
            {
                _logger.LogWarning("Review rejected: user {UserId} tried to review their own property {PropertyId}", userId, propertyId);
                return Result<ReviewResponse>.Failure("You can't review your own property");
            }
            var alreadyReviewed=await _context.Reviews.AnyAsync(p=>p.UserId==userId && p.PropertyId==propertyId,cancellationToken);
            if (alreadyReviewed)
            {
                _logger.LogWarning("Review rejected: user {UserId} already reviewed property {PropertyId}", userId, propertyId);
                return Result<ReviewResponse>.Failure("You have already reviewd this Property");
            }
            var review=_mapper.Map<Review>(request);
            review.PropertyId = propertyId;
            review.UserId = userId;
            _context.Reviews.Add(review);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Concurrent duplicate review by user {UserId} for property {PropertyId}", userId, propertyId);
                _context.ChangeTracker.Clear();
                return Result<ReviewResponse>.Failure("You have already reviewed this property");
            }
            var reviewerName=await _context.Users.Where(u=>u.Id==userId)
            .Select(u=>u.UserName)
            .FirstAsync(cancellationToken);
            _cacheService.RemoveGroup(CacheKeys.ReviewsGroup(propertyId));
            _logger.LogInformation("Review {ReviewId} created by user {UserId} for property {PropertyId}", review.Id, userId, propertyId);
            var response = _mapper.Map<ReviewResponse>(review);
            response.ReviewerName = reviewerName;
            return Result<ReviewResponse>.Success(response);
        }
        public async Task<Result<List<ReviewResponse>>> GetReviewsAsync(int propertyId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                _logger.LogWarning("Review listing for property {PropertyId} rejected: invalid page number {PageNumber}", propertyId, pageNumber);
                return Result<List<ReviewResponse>>.Failure("pageNumber must be 1 or greater");
            }
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            var cacheKey=CacheKeys.Reviews(propertyId,pageNumber,pageSize);
            var cachedReviews=_cacheService.Get<List<ReviewResponse>>(cacheKey);
            if (cachedReviews != null)
            {
                _logger.LogInformation("Reviews for property {PropertyId} returned from cache", propertyId);
                return Result<List<ReviewResponse>>.Success(cachedReviews);
            }
            var propertyPublished=await _context.Properties.AnyAsync(p=>p.Id==propertyId && p.Status==PropertyStatus.Published,cancellationToken);
            if (!propertyPublished)
            {
                _logger.LogWarning("Review listing failed: property {PropertyId} not found or not published", propertyId);
                return Result<List<ReviewResponse>>.Failure("Property not found");
            }
            var reviews= await _context.Reviews.Where(r=>r.PropertyId==propertyId)
            .OrderByDescending(r=>r.CreatedAt)
            .ThenByDescending(r=>r.Id)
            .Skip((pageNumber-1)*pageSize)
            .Take(pageSize)
            .ProjectTo<ReviewResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
            _logger.LogInformation("Returned {Count} reviews for property {PropertyId}", reviews.Count, propertyId);
            _cacheService.Set(cacheKey,reviews,TimeSpan.FromMinutes(5),CacheKeys.ReviewsGroup(propertyId));
            return Result<List<ReviewResponse>>.Success(reviews);
        }
    }
}