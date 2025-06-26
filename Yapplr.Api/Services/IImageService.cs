namespace Yapplr.Api.Services;

public interface IImageService
{
    Task<string> SaveImageAsync(IFormFile file);
    bool DeleteImage(string fileName);
    bool IsValidImageFile(IFormFile file);
}
