using PropertyHub.DataAccess.Enums;
using PropertyHub.Models.DTOs;
namespace PropertyHub.Interfaces.IManager
{
    public interface IPropertyManager
    {
        Task<Result<PropertyResponse>> CreatePropertyAsync(CreatePropertyRequest request,int agentId, CancellationToken cancellationToken=default);
        Task<Result<PropertyResponse>> UpdatePropertyAsync(int propertyId, UpdatePropertyRequest request,int agentId,CancellationToken cancellationToken=default);
        Task<Result<string>> DeletePropertyAsync(int propertyId, int agentId,CancellationToken cancellationToken=default);
        Task<Result<PropertySearchResponse>> SearchPropertiesAsync(SearchPropertyRequest request, CancellationToken cancellationToken=default);
        Task<Result<PropertyResponse>> GetPropertyByIdAsync(int propertyId,CancellationToken cancellationToken=default);
        Task<Result<PropertyResponse>> PublishPropertyAsync(int propertyId, int agentId, CancellationToken cancellationToken=default);
        Task<Result<string>> EnsureCanAddImageAsync(int propertyId, int agentId, CancellationToken cancellationToken = default);
        Task<Result<PropertyImageResponse>> AddImageAsync(int propertyId, string imageUrl, CancellationToken cancellationToken = default);
        Task<Result<List<PropertyResponse>>> GetMineAsync(int agentId, PropertyStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<Result<string>> DeleteImageAsync(int imageId, int agentId, CancellationToken cancellationToken = default); // returns the file name to delete
    }
}