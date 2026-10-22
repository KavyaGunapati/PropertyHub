using PropertyHub.DataAccess.Enums;
using PropertyHub.Models.DTOs;

namespace PropertyHub.Interfaces.IService
{
    public interface IAdminService
    {
        Task<Result<AdminDashboardResponse>> GetDashboardAsync(CancellationToken cancellationToken = default);
        Task<Result<List<AdminPropertyResponse>>> GetPropertiesAsync(PropertyStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<Result<AdminPropertyResponse>> ChangePropertyStatusAsync(int propertyId, PropertyStatus newStatus, CancellationToken cancellationToken = default);
    }
}