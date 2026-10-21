using Microsoft.EntityFrameworkCore;
using PropertyHub.DataAccess.Context;
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
            
        }
    }
}