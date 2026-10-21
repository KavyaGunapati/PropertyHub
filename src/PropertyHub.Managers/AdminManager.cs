using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
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
        public AdminManager(AppDbContext context)
        {
            _context=context;
        }
        public async Task<Result<AdminDashboardResponse>> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            var usersByRole=await _context.Users.GroupBy(u=>u.Role)
            .Select(g=>new {Role=g.Key,Count=g.Count()})
            .ToListAsync(cancellationToken);

            var propertiesByStatus=await _context.Properties.GroupBy(p=>p.Status)
            .Select(g=>new {Stauts=g.Key,Count=g.Count()})
            .ToListAsync(cancellationToken);

            var awaitingResponse=await _context.Inquiries.CountAsync(i=>i.Status==InquiryStatus.New,cancellationToken);
            var totalReviews=await _context.Reviews.CountAsync(cancellationToken);
            return Result<AdminDashboardResponse>.Success(new AdminDashboardResponse
            {
                TotalUsers = usersByRole.Sum(x => x.Count),
                UsersByRole = usersByRole.ToDictionary(x => x.Role.ToString(), x => x.Count),
                TotalProperties = propertiesByStatus.Sum(x => x.Count),
                PropertiesByStatus = propertiesByStatus.ToDictionary(x => x.Stauts.ToString(), x => x.Count),
                InquiriesAwaitingResponse = awaitingResponse,
                TotalReviews = totalReviews
            });

        }
        public async Task<Result<List<AdminPropertyResponse>>> GetPropertiesAsync(PropertyStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1)
                return Result<List<AdminPropertyResponse>>.Failure("pageNumber must be 1 or greater");

            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = _context.Properties.AsNoTracking().AsQueryable();

            if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

            var properties = await query
               .OrderByDescending(p => p.CreatedAt)
               .ThenByDescending(p => p.Id)
               .Skip((pageNumber - 1) * pageSize)
               .Take(pageSize)
               .Select(ToDto)
               .ToListAsync(cancellationToken);

            return Result<List<AdminPropertyResponse>>.Success(properties);
        }
        public async Task<Result<AdminPropertyResponse>> ChangePropertyStatusAsync(int propertyId, PropertyStatus newStatus, CancellationToken cancellationToken = default)
        {
             var property = await _context.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken);

            if (property == null)
                return Result<AdminPropertyResponse>.Failure("Property not found");
            if (newStatus == PropertyStatus.Draft)
                return Result<AdminPropertyResponse>.Failure("Admins cannot return a property to draft");
            if (property.Status == newStatus)
                return Result<AdminPropertyResponse>.Success(await GetDtoAsync(propertyId, cancellationToken));

            if (!IsAllowedTransition(property.Status, newStatus))
                return Result<AdminPropertyResponse>.Failure($"Cannot change status from {property.Status} to {newStatus}");
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
                return Result<AdminPropertyResponse>.Failure("Property status changed while this request was running. Refresh and try again.");
            return Result<AdminPropertyResponse>.Success(await GetDtoAsync(propertyId,cancellationToken));
        }
        private static bool IsAllowedTransition(PropertyStatus from, PropertyStatus to) => (from, to) switch
        {
            (PropertyStatus.Draft,PropertyStatus.Delisted)=>true,
            (PropertyStatus.Published,PropertyStatus.Delisted)=>true,
            (PropertyStatus.Delisted,PropertyStatus.Published)=>true,
            _=>false
        };
        private static readonly Expression<Func<Property,AdminPropertyResponse>> ToDto=
           p=>new AdminPropertyResponse
           {
               Id = p.Id,
               Title = p.Title,
               City = p.City,
               Price = p.Price,
               Status = p.Status,
               AgentId = p.AgentId,
               AgentName = p.Agent.UserName,
               CreatedAt = p.CreatedAt,
               UpdatedAt = p.UpdatedAt,
               PublishedAt = p.CreatedAt
           };
        private async Task<AdminPropertyResponse> GetDtoAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.Properties
            .AsNoTracking()
            .Where(p=>p.Id==id)
            .Select(ToDto)
            .FirstAsync(cancellationToken);
        }
    }
}