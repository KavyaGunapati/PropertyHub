namespace PropertyHub.Interfaces.IManager;

using PropertyHub.DataAccess.Enums;
using PropertyHub.Models.DTOs;

public interface IAdminManager
{
    Task<Result<AdminDashboardResponse>> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<Result<List<AdminPropertyResponse>>> GetPropertiesAsync(PropertyStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<AdminPropertyResponse>> ChangePropertyStatusAsync(int propertyId, PropertyStatus newStatus, CancellationToken cancellationToken = default);
}