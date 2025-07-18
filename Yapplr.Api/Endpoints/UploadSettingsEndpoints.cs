using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;
using Yapplr.Api.Extensions;
using Yapplr.Api.Common;

namespace Yapplr.Api.Endpoints;

public static class UploadSettingsEndpoints
{
    public static void MapUploadSettingsEndpoints(this WebApplication app)
    {
        var uploadSettings = app.MapGroup("/api/admin/upload-settings").WithTags("Admin - Upload Settings");

        // Get current upload settings
        uploadSettings.MapGet("/", async (IUploadSettingsService uploadSettingsService) =>
        {
            return await EndpointUtilities.HandleAsync(
                async () => await uploadSettingsService.GetUploadSettingsAsync()
            );
        })
        .WithName("GetUploadSettings")
        .WithSummary("Get current upload settings")
        .RequireAuthorization("Admin")
        .Produces<UploadSettingsDto>(200)
        .Produces(401)
        .Produces(403);

        // Update upload settings
        uploadSettings.MapPut("/", async ([FromBody] UpdateUploadSettingsDto updateDto, ClaimsPrincipal user, IUploadSettingsService uploadSettingsService) =>
        {
            var userId = user.GetUserId(true);
            return await EndpointUtilities.HandleAsync(
                async () => await uploadSettingsService.UpdateUploadSettingsAsync(updateDto, userId)
            );
        })
        .WithName("UpdateUploadSettings")
        .WithSummary("Update upload settings")
        .RequireAuthorization("Admin")
        .Produces<UploadSettingsDto>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403);

        // Reset to defaults
        uploadSettings.MapPost("/reset", async ([FromBody] ResetUploadSettingsDto resetDto, ClaimsPrincipal user, IUploadSettingsService uploadSettingsService) =>
        {
            var userId = user.GetUserId(true);
            return await EndpointUtilities.HandleAsync(
                async () => await uploadSettingsService.ResetToDefaultsAsync(userId, resetDto.Reason)
            );
        })
        .WithName("ResetUploadSettings")
        .WithSummary("Reset upload settings to defaults")
        .RequireAuthorization("Admin")
        .Produces<UploadSettingsDto>(200)
        .Produces(401)
        .Produces(403);

        // Get max video size (public endpoint for validation)
        uploadSettings.MapGet("/max-video-size", async (IUploadSettingsService uploadSettingsService) =>
        {
            var maxSize = await uploadSettingsService.GetMaxVideoSizeBytesAsync();
            return Results.Ok(new { maxVideoSizeBytes = maxSize });
        })
        .WithName("GetMaxVideoSize")
        .WithSummary("Get maximum video file size")
        .AllowAnonymous()
        .Produces<object>(200);

        // Get max image size (public endpoint for validation)
        uploadSettings.MapGet("/max-image-size", async (IUploadSettingsService uploadSettingsService) =>
        {
            var maxSize = await uploadSettingsService.GetMaxImageSizeBytesAsync();
            return Results.Ok(new { maxImageSizeBytes = maxSize });
        })
        .WithName("GetMaxImageSize")
        .WithSummary("Get maximum image file size")
        .AllowAnonymous()
        .Produces<object>(200);

        // Get allowed extensions (public endpoint for validation)
        uploadSettings.MapGet("/allowed-extensions", async (IUploadSettingsService uploadSettingsService) =>
        {
            var imageExtensions = await uploadSettingsService.GetAllowedImageExtensionsAsync();
            var videoExtensions = await uploadSettingsService.GetAllowedVideoExtensionsAsync();
            
            return Results.Ok(new 
            { 
                allowedImageExtensions = imageExtensions,
                allowedVideoExtensions = videoExtensions
            });
        })
        .WithName("GetAllowedExtensions")
        .WithSummary("Get allowed file extensions")
        .AllowAnonymous()
        .Produces<object>(200);
    }
}

/// <summary>
/// DTO for resetting upload settings
/// </summary>
public class ResetUploadSettingsDto
{
    /// <summary>
    /// Reason for resetting settings
    /// </summary>
    public string? Reason { get; set; }
}
