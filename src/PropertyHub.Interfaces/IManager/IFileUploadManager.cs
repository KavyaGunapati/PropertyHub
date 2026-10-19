using Microsoft.AspNetCore.Http;
namespace PropertyHub.Interfaces.IManager
{
    public interface IFileUploadManager
    {
        Task<string> UploadPropertyImageAsync(IFormFile file, int propertyId,CancellationToken cancellationToken=default);
        Task DeletePropertyImageAsync(string fileName, CancellationToken cancellationToken);
    }
}