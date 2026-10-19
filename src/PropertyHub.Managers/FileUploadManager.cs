using Microsoft.AspNetCore.Http;
using PropertyHub.Interfaces.IManager;

namespace PropertyHub.Managers
{
    public class FileUploadManager : IFileUploadManager
    {
        private readonly string _uploadPath;
        private static readonly string[] allowedExtentions={".jpg", ".jpeg", ".png", ".webp"};
        private const long MaxFileSize=5*1024*1024;
        public FileUploadManager()
        {
           _uploadPath=Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","uploads","properties");
        }
        public async Task<string> UploadPropertyImageAsync(IFormFile file, int propertyId,CancellationToken cancellationToken = default)
        {
            if(file==null || file.Length==0)
               throw new ArgumentException("File is empty");
            var extension=Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtentions.Contains(extension))
            {
                throw new ArgumentException("File format not allowed. Use: jpg, jpeg, png, webp");
            }
            if (file.Length > MaxFileSize)
            {
                throw new ArgumentException("File size exceeds 5MB limit");
            }
            var filename=$"{propertyId}_{Guid.NewGuid()}{extension}";
            var filePath=Path.Combine(_uploadPath,filename);
            Directory.CreateDirectory(_uploadPath);
            using (var stream=new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }
            return "/uploads/properties/{filename}";
        }
        public async Task DeletePropertyImageAsync(string fileName, CancellationToken cancellationToken)
        {
            var filePath=Path.Combine(_uploadPath,fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            await Task.CompletedTask;
        }
    }
}