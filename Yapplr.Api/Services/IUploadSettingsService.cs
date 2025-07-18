using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for managing upload settings
/// </summary>
public interface IUploadSettingsService
{
    /// <summary>
    /// Get current upload settings
    /// </summary>
    Task<UploadSettingsDto> GetUploadSettingsAsync();
    
    /// <summary>
    /// Update upload settings
    /// </summary>
    Task<UploadSettingsDto> UpdateUploadSettingsAsync(UpdateUploadSettingsDto updateDto, int updatedByUserId);
    
    /// <summary>
    /// Get maximum video size in bytes
    /// </summary>
    Task<long> GetMaxVideoSizeBytesAsync();
    
    /// <summary>
    /// Get maximum image size in bytes
    /// </summary>
    Task<long> GetMaxImageSizeBytesAsync();
    
    /// <summary>
    /// Get maximum video duration in seconds
    /// </summary>
    Task<int> GetMaxVideoDurationSecondsAsync();
    
    /// <summary>
    /// Get maximum media files per post
    /// </summary>
    Task<int> GetMaxMediaFilesPerPostAsync();
    
    /// <summary>
    /// Get allowed image extensions as array
    /// </summary>
    Task<string[]> GetAllowedImageExtensionsAsync();
    
    /// <summary>
    /// Get allowed video extensions as array
    /// </summary>
    Task<string[]> GetAllowedVideoExtensionsAsync();
    
    /// <summary>
    /// Check if video processing should delete originals
    /// </summary>
    Task<bool> ShouldDeleteOriginalAfterProcessingAsync();
    
    /// <summary>
    /// Get video processing configuration
    /// </summary>
    Task<(int targetBitrate, int maxWidth, int maxHeight)> GetVideoProcessingConfigAsync();
    
    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    Task<UploadSettingsDto> ResetToDefaultsAsync(int updatedByUserId, string? reason = null);
}
