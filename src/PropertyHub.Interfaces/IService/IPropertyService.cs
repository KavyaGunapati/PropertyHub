using PropertyHub.Models.DTOs;
using Microsoft.AspNetCore.Http;
using PropertyHub.DataAccess.Enums;
namespace PropertyHub.Interfaces.IService
{
    public interface IPropertyService
    {
        Task<Result<PropertyResponse>> CreatePropertyAsync(CreatePropertyRequest request,int agentId, CancellationToken cancellationToken=default);
        Task<Result<PropertyResponse>> UpdatePropertyAsync(int propertyId, UpdatePropertyRequest request,int agentId,CancellationToken cancellationToken=default);
        Task<Result<string>> DeletePropertyAsync(int propertyId, int agentId,CancellationToken cancellationToken=default);
        Task<Result<PropertySearchResponse>> SearchPropertiesAsync(SearchPropertyRequest request, CancellationToken cancellationToken=default);
        Task<Result<PropertyResponse>> GetPropertyByIdAsync(int propertyId,CancellationToken cancellationToken=default);
        Task<Result<PropertyResponse>> PublishPropertyAsync(int propertyId, int agentId, CancellationToken cancellationToken=default);
        Task<Result<PropertyImageResponse>> UploadPropertyImageAsync(int propertyId, IFormFile file, int agentId, CancellationToken cancellationToken);
        Task<Result<string>> DeletePropertyImageAsync(int imageId, int agentId, CancellationToken cancellationToken);
        Task<Result<List<PropertyResponse>>> GetMineAsync(int agentId, PropertyStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}