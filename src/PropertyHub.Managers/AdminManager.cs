using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyHub.DataAccess.Context;
using PropertyHub.DataAccess.Entities;
using PropertyHub.DataAccess.Enums;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Models.DTOs;

namespace PropertyHub.Managers
{
    public class AdminManager:IAdminManager
    {
        private readonly AppDbContext _context;
        private const int MaxPageSize=35;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminManager> _logger;
        private readonly ICacheService _cacheService;
        public AdminManager(AppDbContext context, IMapper mapper, ILogger<AdminManager> logger, ICacheService cacheService)
        {
            _context=context;
            _mapper=mapper;
            _logger=logger;
            _cacheService=cacheService;
        }
        public async Task<Result<AdminDashboardResponse>> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            var cachedDashboard=_cacheService.Get<AdminDashboardResponse>(CacheKeys.AdminDashboard);
            if (cachedDashboard != null)
            {
                _logger.LogInformation("Admin dashboard returned from cache");
                return Result<AdminDashboardResponse>.Success(cachedDashboard);
            }
            _logger.LogInformation("Loading admin dashboard");
            var usersByRole=await _context.Users.GroupBy(u=>u.Role)
            .Select(g=>new {Role=g.Key,Count=g.Count()})
            .ToListAsync(cancellationToken);

            var propertiesByStatus=await _context.Properties.GroupBy(p=>p.Status)
            .Select(g=>new {Stauts=g.Key,Count=g.Count()})
            .ToListAsync(cancellationToken);

            var awaitingResponse=await _context.Inquiries.CountAsync(i=>i.Status==InquiryStatus.New,cancellationToken);
            var totalReviews=await _context.Reviews.CountAsync(cancellationToken);
            _logger.LogInformation("Admin dashboard loaded: {TotalUsers} users, {AwaitingResponse} inquiries awaiting response, {TotalReviews} reviews", usersByRole.Sum(x => x.Count), awaitingResponse, totalReviews);
            var dashboard=new AdminDashboardResponse
            {
                TotalUsers = usersByRole.Sum(x => x.Count),
                UsersByRole = usersByRole.ToDictionary(x => x.Role.ToString(), x => x.Count),
                TotalProperties = propertiesByStatus.Sum(x => x.Count),
                PropertiesByStatus = propertiesByStatus.ToDictionary(x => x.Stauts.ToString(), x => x.Count),
                InquiriesAwaitingResponse = awaitingResponse,
                TotalReviews = totalReviews
            };
            _cacheService.Set(CacheKeys.AdminDashboard,dashboard,TimeSpan.FromSeconds(30));
            return Result<AdminDashboardResponse>.Success(dashboard);

        }
        public async Task<Result<List<AdminPropertyResponse>>> GetPropertiesAsync(PropertyStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Admin listing properties: status {Status}, page {PageNumber}, size {PageSize}", status, pageNumber, pageSize);
            if (pageNumber < 1)
            {
                _logger.LogWarning("Admin property listing rejected: invalid page number {PageNumber}", pageNumber);
                return Result<List<AdminPropertyResponse>>.Failure("pageNumber must be 1 or greater");
            }

            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.Properties.AsNoTracking().AsQueryable();

            if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

            var properties = await query
               .OrderByDescending(p => p.CreatedAt)
               .ThenByDescending(p => p.Id)
               .Skip((pageNumber - 1) * pageSize)
               .Take(pageSize)
               .ProjectTo<AdminPropertyResponse>(_mapper.ConfigurationProvider)
               .ToListAsync(cancellationToken);

            _logger.LogInformation("Admin property listing returned {Count} properties", properties.Count);
            return Result<List<AdminPropertyResponse>>.Success(properties);
        }
        public async Task<Result<AdminPropertyResponse>> ChangePropertyStatusAsync(int propertyId, PropertyStatus newStatus, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Admin changing status of property {PropertyId} to {NewStatus}", propertyId, newStatus);
             var property = await _context.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken);

            if (property == null)
            {
                _logger.LogWarning("Admin status change failed: property {PropertyId} not found", propertyId);
                return Result<AdminPropertyResponse>.Failure("Property not found");
            }
            if (newStatus == PropertyStatus.Draft)
            {
                _logger.LogWarning("Admin status change rejected: property {PropertyId} cannot return to draft", propertyId);
                return Result<AdminPropertyResponse>.Failure("Admins cannot return a property to draft");
            }
            if (property.Status == newStatus)
            {
                _logger.LogInformation("Property {PropertyId} is already {Status}; no change made", propertyId, newStatus);
                return Result<AdminPropertyResponse>.Success(await GetDtoAsync(propertyId, cancellationToken));
            }

            if (!IsAllowedTransition(property.Status, newStatus))
            {
                _logger.LogWarning("Admin status change rejected: transition {From} -> {To} not allowed for property {PropertyId}", property.Status, newStatus, propertyId);
                return Result<AdminPropertyResponse>.Failure($"Cannot change status from {property.Status} to {newStatus}");
            }
            // Conditional update: only applies if the status is still what we just read.
            // If an agent or another admin changed it in between, nothing is written.
            var from=property.Status;
            var rowsUpdated=await _context.Properties
            .Where(p=>p.Id==propertyId && p.Status==from)
            .ExecuteUpdateAsync(s=>s
            .SetProperty(p=>p.Status,newStatus)
            .SetProperty(p=>p.UpdatedAt,DateTime.UtcNow),
            cancellationToken);
            if (rowsUpdated == 0)
            {
                _logger.LogWarning("Admin status change for property {PropertyId} lost a concurrent update (expected {From})", propertyId, from);
                return Result<AdminPropertyResponse>.Failure("Property status changed while this request was running. Refresh and try again.");
            }
            _cacheService.Remove(CacheKeys.Property(propertyId));
            _cacheService.RemoveGroup(CacheKeys.PropertySearchGroup);
            _cacheService.RemoveGroup(CacheKeys.ReviewsGroup(propertyId));
            _cacheService.Remove(CacheKeys.AdminDashboard);
            _logger.LogInformation("Property {PropertyId} status changed from {From} to {To} by admin", propertyId, from, newStatus);
            return Result<AdminPropertyResponse>.Success(await GetDtoAsync(propertyId,cancellationToken));
        }
        private static bool IsAllowedTransition(PropertyStatus from, PropertyStatus to) => (from, to) switch
        {
            (PropertyStatus.Draft,PropertyStatus.Delisted)=>true,
            (PropertyStatus.Published,PropertyStatus.Delisted)=>true,
            (PropertyStatus.Delisted,PropertyStatus.Published)=>true,
            _=>false
        };
        private async Task<AdminPropertyResponse> GetDtoAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.Properties
            .AsNoTracking()
            .Where(p=>p.Id==id)
            .ProjectTo<AdminPropertyResponse>(_mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
        }
    }
}