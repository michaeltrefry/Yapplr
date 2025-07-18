using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for managing upload settings with database storage and caching
/// </summary>
public class UploadSettingsService : IUploadSettingsService
{
    private readonly YapplrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UploadSettingsService> _logger;
    private const string CACHE_KEY = "upload_settings";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(30);

    public UploadSettingsService(
        YapplrDbContext context,
        IMemoryCache cache,
        ILogger<UploadSettingsService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<UploadSettingsDto> GetUploadSettingsAsync()
    {
        // Try to get from cache first
        if (_cache.TryGetValue(CACHE_KEY, out UploadSettingsDto? cachedSettings) && cachedSettings != null)
        {
            return cachedSettings;
        }

        // Get from database or create defaults
        var settings = await GetOrCreateSettingsAsync();
        var dto = MapToDto(settings);
        
        // Cache the result
        _cache.Set(CACHE_KEY, dto, CACHE_DURATION);
        
        return dto;
    }

    public async Task<UploadSettingsDto> UpdateUploadSettingsAsync(UpdateUploadSettingsDto updateDto, int updatedByUserId)
    {
        var settings = await GetOrCreateSettingsAsync();
        
        // Update settings
        settings.MaxImageSizeBytes = updateDto.MaxImageSizeBytes;
        settings.MaxVideoSizeBytes = updateDto.MaxVideoSizeBytes;
        settings.MaxVideoDurationSeconds = updateDto.MaxVideoDurationSeconds;
        settings.MaxMediaFilesPerPost = updateDto.MaxMediaFilesPerPost;
        settings.AllowedImageExtensions = updateDto.AllowedImageExtensions;
        settings.AllowedVideoExtensions = updateDto.AllowedVideoExtensions;
        settings.DeleteOriginalAfterProcessing = updateDto.DeleteOriginalAfterProcessing;
        settings.VideoTargetBitrate = updateDto.VideoTargetBitrate;
        settings.VideoMaxWidth = updateDto.VideoMaxWidth;
        settings.VideoMaxHeight = updateDto.VideoMaxHeight;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedByUserId = updatedByUserId;
        settings.UpdateReason = updateDto.UpdateReason;

        await _context.SaveChangesAsync();
        
        // Clear cache
        _cache.Remove(CACHE_KEY);
        
        _logger.LogInformation("Upload settings updated by user {UserId}. Reason: {Reason}", 
            updatedByUserId, updateDto.UpdateReason ?? "No reason provided");
        
        return MapToDto(settings);
    }

    public async Task<long> GetMaxVideoSizeBytesAsync()
    {
        var settings = await GetUploadSettingsAsync();
        return settings.MaxVideoSizeBytes;
    }

    public async Task<long> GetMaxImageSizeBytesAsync()
    {
        var settings = await GetUploadSettingsAsync();
        return settings.MaxImageSizeBytes;
    }

    public async Task<int> GetMaxVideoDurationSecondsAsync()
    {
        var settings = await GetUploadSettingsAsync();
        return settings.MaxVideoDurationSeconds;
    }

    public async Task<int> GetMaxMediaFilesPerPostAsync()
    {
        var settings = await GetUploadSettingsAsync();
        return settings.MaxMediaFilesPerPost;
    }

    public async Task<string[]> GetAllowedImageExtensionsAsync()
    {
        var settings = await GetUploadSettingsAsync();
        return settings.AllowedImageExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim().ToLowerInvariant())
            .ToArray();
    }

    public async Task<string[]> GetAllowedVideoExtensionsAsync()
    {
        var settings = await GetUploadSettingsAsync();
        return settings.AllowedVideoExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim().ToLowerInvariant())
            .ToArray();
    }

    public async Task<bool> ShouldDeleteOriginalAfterProcessingAsync()
    {
        var settings = await GetUploadSettingsAsync();
        return settings.DeleteOriginalAfterProcessing;
    }

    public async Task<(int targetBitrate, int maxWidth, int maxHeight)> GetVideoProcessingConfigAsync()
    {
        var settings = await GetUploadSettingsAsync();
        return (settings.VideoTargetBitrate, settings.VideoMaxWidth, settings.VideoMaxHeight);
    }

    public async Task<UploadSettingsDto> ResetToDefaultsAsync(int updatedByUserId, string? reason = null)
    {
        var settings = await GetOrCreateSettingsAsync();
        
        // Reset to defaults
        settings.MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB
        settings.MaxVideoSizeBytes = 1024 * 1024 * 1024; // 1GB
        settings.MaxVideoDurationSeconds = 1800; // 30 minutes
        settings.MaxMediaFilesPerPost = 10;
        settings.AllowedImageExtensions = ".jpg,.jpeg,.png,.gif,.webp";
        settings.AllowedVideoExtensions = ".mp4,.avi,.mov,.wmv,.flv,.webm,.mkv,.3gp";
        settings.DeleteOriginalAfterProcessing = true;
        settings.VideoTargetBitrate = 2000;
        settings.VideoMaxWidth = 1920;
        settings.VideoMaxHeight = 1080;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedByUserId = updatedByUserId;
        settings.UpdateReason = reason ?? "Reset to defaults";

        await _context.SaveChangesAsync();
        
        // Clear cache
        _cache.Remove(CACHE_KEY);
        
        _logger.LogInformation("Upload settings reset to defaults by user {UserId}. Reason: {Reason}", 
            updatedByUserId, reason ?? "No reason provided");
        
        return MapToDto(settings);
    }

    private async Task<UploadSettings> GetOrCreateSettingsAsync()
    {
        var settings = await _context.UploadSettings.FirstOrDefaultAsync();
        
        if (settings == null)
        {
            // Create default settings
            settings = new UploadSettings();
            _context.UploadSettings.Add(settings);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created default upload settings");
        }
        
        return settings;
    }

    private static UploadSettingsDto MapToDto(UploadSettings settings)
    {
        return new UploadSettingsDto
        {
            MaxImageSizeBytes = settings.MaxImageSizeBytes,
            MaxVideoSizeBytes = settings.MaxVideoSizeBytes,
            MaxVideoDurationSeconds = settings.MaxVideoDurationSeconds,
            MaxMediaFilesPerPost = settings.MaxMediaFilesPerPost,
            AllowedImageExtensions = settings.AllowedImageExtensions,
            AllowedVideoExtensions = settings.AllowedVideoExtensions,
            DeleteOriginalAfterProcessing = settings.DeleteOriginalAfterProcessing,
            VideoTargetBitrate = settings.VideoTargetBitrate,
            VideoMaxWidth = settings.VideoMaxWidth,
            VideoMaxHeight = settings.VideoMaxHeight,
            UpdatedAt = settings.UpdatedAt,
            UpdatedByUsername = settings.UpdatedByUser?.Username,
            UpdateReason = settings.UpdateReason
        };
    }
}
