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
    public class InquiryManager : IInquiryManager
    {
        private const int MaxPageSize=30;
        private readonly AppDbContext _context;
        private readonly CreateInquiryValidator _createInquiryValidator;
        private readonly ResponseInquiryValidator _responseInquiryValidator;
        private readonly IMapper _mapper;
        private readonly ILogger<InquiryManager> _logger;
        public InquiryManager(CreateInquiryValidator createInquiryValidator,ResponseInquiryValidator responseInquiryValidator,AppDbContext context,IMapper mapper,ILogger<InquiryManager> logger)
        {
            _context=context;
            _mapper=mapper;
            _logger=logger;
            _createInquiryValidator=createInquiryValidator;
            _responseInquiryValidator=responseInquiryValidator;
        }
        public async Task<Result<InquiryResponse>> SubmitAsync(int propertyId, int buyerId, CreateInquiryRequest request,CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Buyer {BuyerId} submitting inquiry for property {PropertyId}", buyerId, propertyId);
            var validation=await _createInquiryValidator.ValidateAsync(request,cancellationToken);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Inquiry submission by buyer {BuyerId} failed validation for property {PropertyId}", buyerId, propertyId);
                return Result<InquiryResponse>.Failure(string.Join(", ",validation.Errors.Select(e=>e.ErrorMessage)));
            }
            var property=await _context.Properties.FirstOrDefaultAsync(p=>p.Id==propertyId,cancellationToken);
            if(property==null || property.Status != PropertyStatus.Published)
            {
                _logger.LogWarning("Inquiry rejected: property {PropertyId} not found or not published", propertyId);
                return Result<InquiryResponse>.Failure("Property Not found");
            }
            if (property.AgentId == buyerId)
            {
                _logger.LogWarning("Inquiry rejected: user {BuyerId} tried to inquire about their own property {PropertyId}", buyerId, propertyId);
                return Result<InquiryResponse>.Failure("You cannot inquire about your own property");
            }
            var inquiry = _mapper.Map<Inquiry>(request);
            inquiry.PropertyId = propertyId;
            inquiry.BuyerId = buyerId;

           _context.Inquiries.Add(inquiry);
           await _context.SaveChangesAsync(cancellationToken);
           _logger.LogInformation("Inquiry {InquiryId} created by buyer {BuyerId} for property {PropertyId}", inquiry.Id, buyerId, propertyId);

           return Result<InquiryResponse>.Success(await GetDtoAsync(inquiry.Id, cancellationToken));
        }
        public async Task<Result<List<InquiryResponse>>> GetForPropertyAsync(int propertyId,int agentId,int pageNumber,int pageSize,CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                _logger.LogWarning("Inquiry listing for property {PropertyId} rejected: invalid page number {PageNumber}", propertyId, pageNumber);
                return Result<List<InquiryResponse>>.Failure("Page number must be greater than or equal to 1");
            }
            pageSize=Math.Clamp(pageSize,1,MaxPageSize);
            var isOwner=await _context.Properties.AnyAsync(p=>p.Id==propertyId && p.AgentId==agentId,cancellationToken);
            if (!isOwner)
            {
                _logger.LogWarning("Agent {AgentId} tried to list inquiries for property {PropertyId} they do not own", agentId, propertyId);
                return Result<List<InquiryResponse>>.Failure("Property not found");
            }
            var inquiries=await _context.Inquiries.Where(i=>i.PropertyId==propertyId)
            .OrderByDescending(i=>i.CreatedAt)
            .ThenByDescending(i=>i.Id)
            .Skip((pageNumber-1)*pageSize)
            .Take(pageSize)
            .ProjectTo<InquiryResponse>(_mapper.ConfigurationProvider)
            .ToListAsync();
            _logger.LogInformation("Returned {Count} inquiries for property {PropertyId}", inquiries.Count, propertyId);
            return Result<List<InquiryResponse>>.Success(inquiries);
        }
        public async Task<Result<List<InquiryResponse>>> GetMineAsync(int agentId,int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
            {
                _logger.LogWarning("Own-inquiry listing for user {UserId} rejected: invalid page number {PageNumber}", agentId, pageNumber);
                return Result<List<InquiryResponse>>.Failure("Page number must be greater than or equal to 1");
            }
            pageSize=Math.Clamp(pageSize,1,MaxPageSize);
            var inquiries=await _context.Inquiries.Where(i=>i.BuyerId==agentId)
            .OrderByDescending(i=>i.CreatedAt)
            .ThenByDescending(i=>i.Id)
            .Skip((pageNumber-1)*pageSize)
            .Take(pageSize)
            .ProjectTo<InquiryResponse>(_mapper.ConfigurationProvider)
            .ToListAsync();
            _logger.LogInformation("Returned {Count} inquiries for user {UserId}", inquiries.Count, agentId);
            return Result<List<InquiryResponse>>.Success(inquiries);
        }
        public async Task<Result<InquiryResponse>> RespondAsync(int inquiryId,int agentId, RespondInquiryRequest request,CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Agent {AgentId} responding to inquiry {InquiryId}", agentId, inquiryId);
            var validation=await _responseInquiryValidator.ValidateAsync(request,cancellationToken);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Response to inquiry {InquiryId} failed validation", inquiryId);
                return Result<InquiryResponse>.Failure(string.Join(", ",validation.Errors.Select(i=>i.ErrorMessage)));
            }
            var rowUpdated=await _context.Inquiries.Where(i=>i.Id==inquiryId && i.Status==InquiryStatus.New && i.Property.AgentId==agentId)
            .ExecuteUpdateAsync(s=>s
            .SetProperty(p=>p.Status,InquiryStatus.Responded)
            .SetProperty(p=>p.Response,request.Response.Trim())
            .SetProperty(i=>i.RespondedAt,DateTime.UtcNow),cancellationToken);
            if (rowUpdated == 1)
            {
                _logger.LogInformation("Inquiry {InquiryId} answered by agent {AgentId}", inquiryId, agentId);
                return Result<InquiryResponse>.Success(await GetDtoAsync(inquiryId,cancellationToken));
            }
            var current=await _context.Inquiries.Where(i=>i.Id==inquiryId)
            .Select(i=>new {i.Property.AgentId,i.Status})
            .FirstOrDefaultAsync(cancellationToken);
            if (current == null || current.AgentId != agentId)
            {
                _logger.LogWarning("Agent {AgentId} cannot respond to inquiry {InquiryId}: not found or not theirs", agentId, inquiryId);
                return Result<InquiryResponse>.Failure("Inquiry not found");
            }
            _logger.LogWarning("Inquiry {InquiryId} was already responded to", inquiryId);
             return Result<InquiryResponse>.Failure("Inquiry has already been responded to");
        }
        private async Task<InquiryResponse> GetDtoAsync(int id, CancellationToken cancellationToken)
           =>await _context.Inquiries.Where(i=>i.Id==id)
           .ProjectTo<InquiryResponse>(_mapper.ConfigurationProvider)
           .FirstAsync(cancellationToken);
    }
}