using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PropertyHub.Interfaces.IManager;

namespace PropertyHub.Managers
{
    public class FileUploadManager : IFileUploadManager
    {
        private readonly string _uploadPath;
        private static readonly string[] allowedExtentions={".jpg", ".jpeg", ".png", ".webp"};
        private const long MaxFileSize=5*1024*1024;
        private readonly ILogger<FileUploadManager> _logger;
        public FileUploadManager(ILogger<FileUploadManager> logger)
        {
           _logger=logger;
           _uploadPath=Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","uploads","properties");
        }
        public async Task<string> UploadPropertyImageAsync(IFormFile file, int propertyId,CancellationToken cancellationToken = default)
        {
            if(file==null || file.Length==0)
            {
               _logger.LogWarning("Image upload rejected for property {PropertyId}: file is empty", propertyId);
               throw new ArgumentException("File is empty");
            }
            var extension=Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtentions.Contains(extension))
            {
                _logger.LogWarning("Image upload rejected for property {PropertyId}: extension {Extension} not allowed", propertyId, extension);
                throw new ArgumentException("File format not allowed. Use: jpg, jpeg, png, webp");
            }
            if (file.Length > MaxFileSize)
            {
                _logger.LogWarning("Image upload rejected for property {PropertyId}: size {Size} bytes exceeds limit", propertyId, file.Length);
                throw new ArgumentException("File size exceeds 5MB limit");
            }
            var filename=$"{propertyId}_{Guid.NewGuid()}{extension}";
            var filePath=Path.Combine(_uploadPath,filename);
            Directory.CreateDirectory(_uploadPath);
            using (var stream=new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }
            _logger.LogInformation("Saved image {FileName} for property {PropertyId}", filename, propertyId);
            return "/uploads/properties/{filename}";
        }
        public async Task DeletePropertyImageAsync(string fileName, CancellationToken cancellationToken)
        {
            var filePath=Path.Combine(_uploadPath,fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted image file {FileName}", fileName);
            }
            else
            {
                _logger.LogWarning("Image file {FileName} not found for deletion", fileName);
            }
            await Task.CompletedTask;
        }
    }
}