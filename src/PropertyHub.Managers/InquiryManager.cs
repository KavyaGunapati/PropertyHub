using Microsoft.EntityFrameworkCore;
using PropertyHub.DataAccess.Context;
using PropertyHub.DataAccess.Entities;
using PropertyHub.DataAccess.Enums;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Models.DTOs;
using PropertyHub.Services.Validators;

namespace PropertyHub.Managers
{
    public class InquiryManager : IInquiryManager
    {
        private const int MaxPageSize=30;
        private readonly AppDbContext _context;
        private readonly CreateInquiryValidator _createInquiryValidator;
        private readonly ResponseInquiryValidator _responseInquiryValidator;
        public InquiryManager(CreateInquiryValidator createInquiryValidator,ResponseInquiryValidator responseInquiryValidator,AppDbContext context)
        {
            _context=context;
            _createInquiryValidator=createInquiryValidator;
            _responseInquiryValidator=responseInquiryValidator;
        }
        public async Task<Result<InquiryResponse>> SubmitAsync(int propertyId, int buyerId, CreateInquiryRequest request,CancellationToken cancellationToken = default)
        {
            var validation=await _createInquiryValidator.ValidateAsync(request,cancellationToken);
            if (!validation.IsValid)
            {
                return Result<InquiryResponse>.Failure(string.Join(", ",validation.Errors.Select(e=>e.ErrorMessage)));
            }
            var property=await _context.Properties.FirstOrDefaultAsync(p=>p.Id==propertyId,cancellationToken);
            if(property==null || property.Status != PropertyStatus.Published)
            {
                return Result<InquiryResponse>.Failure("Property Not found");
            }
            if (property.AgentId == buyerId)
            {
                return Result<InquiryResponse>.Failure("You cannot inquire about your own property");
            }
            var inquiry = new Inquiry
            {
            PropertyId = propertyId,
            BuyerId = buyerId,
            Message = request.Message.Trim(),
            Status = InquiryStatus.New,
            CreatedAt = DateTime.UtcNow
            };

           _context.Inquiries.Add(inquiry);
           await _context.SaveChangesAsync(cancellationToken);

           return Result<InquiryResponse>.Success(await GetDtoAsync(inquiry.Id, cancellationToken));
        }
        public async Task<Result<List<InquiryResponse>>> GetForPropertyAsync(int propertyId,int agentId,int pageNumber,int pageSize,CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                return Result<List<InquiryResponse>>.Failure("Page number must be greater than or equal to 1");
            }
            pageSize=Math.Clamp(pageSize,1,MaxPageSize);
            var isOwner=await _context.Properties.AnyAsync(p=>p.Id==propertyId && p.AgentId==agentId,cancellationToken);
            if (!isOwner)
            {
                return Result<List<InquiryResponse>>.Failure("Property not found");
            }
            var inquiries=await _context.Inquiries.Where(i=>i.PropertyId==propertyId)
            .OrderByDescending(i=>i.CreatedAt)
            .ThenByDescending(i=>i.Id)
            .Skip((pageNumber-1)*pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();
            return Result<List<InquiryResponse>>.Success(inquiries);
        }
        public async Task<Result<List<InquiryResponse>>> GetMineAsync(int agentId,int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                return Result<List<InquiryResponse>>.Failure("Page number must be greater than or equal to 1");
            }
            pageSize=Math.Clamp(pageSize,1,MaxPageSize);
            var inquiries=await _context.Inquiries.Where(i=>i.BuyerId==agentId)
            .OrderByDescending(i=>i.CreatedAt)
            .ThenByDescending(i=>i.Id)
            .Skip((pageNumber-1)*pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();
            return Result<List<InquiryResponse>>.Success(inquiries);
        }
        public async Task<Result<InquiryResponse>> RespondAsync(int inquiryId,int agentId, RespondInquiryRequest request,CancellationToken cancellationToken = default)
        {
            var validation=await _responseInquiryValidator.ValidateAsync(request,cancellationToken);
            if (!validation.IsValid)
            {
                return Result<InquiryResponse>.Failure(string.Join(", ",validation.Errors.Select(i=>i.ErrorMessage)));
            }
            var rowUpdated=await _context.Inquiries.Where(i=>i.Id==inquiryId && i.Status==InquiryStatus.New && i.Property.AgentId==agentId)
            .ExecuteUpdateAsync(s=>s
            .SetProperty(p=>p.Status,InquiryStatus.Responded)
            .SetProperty(p=>p.Response,request.Response.Trim())
            .SetProperty(i=>i.RespondedAt,DateTime.UtcNow),cancellationToken);
            if (rowUpdated == 1)
            {
                return Result<InquiryResponse>.Success(await GetDtoAsync(inquiryId,cancellationToken));
            }
            var current=await _context.Inquiries.Where(i=>i.Id==inquiryId)
            .Select(i=>new {i.Property.AgentId,i.Status})
            .FirstOrDefaultAsync(cancellationToken);
            if (current == null || current.AgentId != agentId)
            return Result<InquiryResponse>.Failure("Inquiry not found");
             return Result<InquiryResponse>.Failure("Inquiry has already been responded to");
        }
        private static readonly System.Linq.Expressions.Expression<Func<Inquiry,InquiryResponse>> ToDto=
        i=>new InquiryResponse
        {
             Id = i.Id,
            PropertyId = i.PropertyId,
            PropertyTitle = i.Property.Title,
            BuyerName = i.Buyer.UserName,
            Message = i.Message,
            Response = i.Response,
            Status = i.Status,
            CreatedAt = i.CreatedAt,
            RespondedAt = i.RespondedAt
        };
        private async Task<InquiryResponse> GetDtoAsync(int id, CancellationToken cancellationToken)
           =>await _context.Inquiries.Where(i=>i.Id==id)
           .Select(ToDto)
           .FirstAsync(cancellationToken);
    }
}