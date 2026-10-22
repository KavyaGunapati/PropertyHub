using PropertyHub;
using PropertyHub.DataAccess.Enums;
using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;

namespace PropertyHub.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminManager _manager;
        public AdminService(IAdminManager manager)
        {
            _manager=manager;
        }
        public Task<Result<AdminDashboardResponse>> GetDashboardAsync(CancellationToken cancellationToken = default)
            =>_manager.GetDashboardAsync(cancellationToken);
        public Task<Result<List<AdminPropertyResponse>>> GetPropertiesAsync(PropertyStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
            =>_manager.GetPropertiesAsync(status,pageNumber,pageSize,cancellationToken);
        public Task<Result<AdminPropertyResponse>> ChangePropertyStatusAsync(int propertyId, PropertyStatus newStatus, CancellationToken cancellationToken = default)
            =>_manager.ChangePropertyStatusAsync(propertyId,newStatus,cancellationToken);
    }
}