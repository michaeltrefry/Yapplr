using Microsoft.AspNetCore.StaticFiles;

namespace Yapplr.Api.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _basePath;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IWebHostEnvironment environment, ILogger<LocalStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        _basePath = Path.Combine(environment.ContentRootPath, "uploads");
        
        // Ensure base uploads directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, string directory, string? fileName = null)
    {
        var directoryPath = Path.Combine(_basePath, directory);
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Generate unique filename if not provided
        if (string.IsNullOrEmpty(fileName))
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            fileName = $"{Guid.NewGuid()}{extension}";
        }

        var filePath = Path.Combine(directoryPath, fileName);

        try
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            
            _logger.LogInformation("File saved successfully: {FilePath}", filePath);
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<string> SaveFileAsync(Stream stream, string directory, string fileName)
    {
        var directoryPath = Path.Combine(_basePath, directory);
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var filePath = Path.Combine(directoryPath, fileName);

        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream);
            
            _logger.LogInformation("File saved successfully: {FilePath}", filePath);
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file: {FilePath}", filePath);
            throw;
        }
    }

    public Task<bool> DeleteFileAsync(string directory, string fileName)
    {
        try
        {
            var filePath = Path.Combine(_basePath, directory, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return Task.FromResult(true);
            }
            
            _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Directory}/{FileName}", directory, fileName);
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(string directory, string fileName)
    {
        var filePath = Path.Combine(_basePath, directory, fileName);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<Stream> GetFileStreamAsync(string directory, string fileName)
    {
        var filePath = Path.Combine(_basePath, directory, fileName);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {directory}/{fileName}");
        }

        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public string GetFileUrl(string directory, string fileName)
    {
        // This will be used by the API to construct URLs
        // The actual URL construction will happen in the controller/endpoint
        return $"/api/files/{directory}/{fileName}";
    }

    public Task<long> GetFileSizeAsync(string directory, string fileName)
    {
        var filePath = Path.Combine(_basePath, directory, fileName);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {directory}/{fileName}");
        }

        var fileInfo = new FileInfo(filePath);
        return Task.FromResult(fileInfo.Length);
    }

    public string GetPhysicalPath(string directory, string fileName)
    {
        return Path.Combine(_basePath, directory, fileName);
    }

    public string GetContentType(string fileName)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (provider.TryGetContentType(fileName, out var contentType))
        {
            return contentType;
        }
        return "application/octet-stream";
    }
}
