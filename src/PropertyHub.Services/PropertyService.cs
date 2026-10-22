using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;
using Microsoft.AspNetCore.Http;
using PropertyHub.DataAccess.Enums;
namespace PropertyHub.Services
{
    public class PropertyService:IPropertyService
    {
        private readonly IPropertyManager _manager;
        private readonly IFileUploadManager _fileUploadManager;
        public PropertyService(IPropertyManager manager, IFileUploadManager fileUploadManager)
        {
            _manager=manager;
            _fileUploadManager=fileUploadManager;
        }
        public async Task<Result<PropertyResponse>> CreatePropertyAsync(CreatePropertyRequest request,int agentId, CancellationToken cancellationToken=default)
         =>await _manager.CreatePropertyAsync(request,agentId,cancellationToken);
        public async Task<Result<PropertyResponse>> UpdatePropertyAsync(int propertyId, UpdatePropertyRequest request,int agentId,CancellationToken cancellationToken=default)
        =>await _manager.UpdatePropertyAsync(propertyId,request,agentId,cancellationToken);
        public async Task<Result<string>> DeletePropertyAsync(int propertyId, int agentId,CancellationToken cancellationToken=default)
        =>await _manager.DeletePropertyAsync(propertyId,agentId,cancellationToken);
        public async Task<Result<PropertySearchResponse>> SearchPropertiesAsync(SearchPropertyRequest request, CancellationToken cancellationToken=default)
        =>await _manager.SearchPropertiesAsync(request,cancellationToken);
        public async Task<Result<PropertyResponse>> GetPropertyByIdAsync(int propertyId,CancellationToken cancellationToken=default)
        =>await _manager.GetPropertyByIdAsync(propertyId,cancellationToken);
        public async Task<Result<PropertyResponse>> PublishPropertyAsync(int propertyId, int agentId, CancellationToken cancellationToken=default)
        =>await _manager.PublishPropertyAsync(propertyId,agentId,cancellationToken);

        public async Task<Result<PropertyImageResponse>> UploadPropertyImageAsync(
        int propertyId, IFormFile file, int agentId, CancellationToken cancellationToken)
        {
            var canAdd=await _manager.EnsureCanAddImageAsync(propertyId,agentId,cancellationToken);
            if (!canAdd.IsSuccess)
            {
                return Result<PropertyImageResponse>.Failure(canAdd.Error);
            }
            string imageUrl;
            try
            {
                imageUrl=await _fileUploadManager.UploadPropertyImageAsync(file,propertyId,cancellationToken);
            }
            catch (ArgumentException ex)
            {
                return Result<PropertyImageResponse>.Failure(ex.Message);
            }
            // 3. Save DB record; if it fails, remove the orphaned file
            var saved =await _manager.AddImageAsync(propertyId,imageUrl,cancellationToken);
            if (!saved.IsSuccess)
            {
                await _fileUploadManager.DeletePropertyImageAsync(Path.GetFileName(imageUrl),cancellationToken);
                return saved;
            }
            return saved;
        }
        public async Task<Result<string>> DeletePropertyImageAsync(int imageId, int agentId, CancellationToken cancellationToken)
        {
            // 1. Manager validates ownership and returns the file name
            var result=await _manager.DeleteImageAsync(imageId,agentId,cancellationToken);
            if(!result.IsSuccess)
                 return result;
            await _fileUploadManager.DeletePropertyImageAsync(result.Data,cancellationToken);
            return Result<string>.Success("Image deleted successfully");
        } 
        public Task<Result<List<PropertyResponse>>> GetMineAsync(int agentId, PropertyStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
           =>_manager.GetMineAsync(agentId,status,pageNumber,pageSize,cancellationToken);
    }
}