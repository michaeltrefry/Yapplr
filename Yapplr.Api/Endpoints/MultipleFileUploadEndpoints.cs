using Yapplr.Api.DTOs;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class MultipleFileUploadEndpoints
{
    public static void MapMultipleFileUploadEndpoints(this WebApplication app)
    {
        var uploads = app.MapGroup("/api/uploads").WithTags("Multiple File Uploads");

        // Upload multiple media files
        uploads.MapPost("/media", async (
            IFormFileCollection files,
            IMultipleFileUploadService uploadService,
            HttpContext context) =>
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return Results.BadRequest(new { message = "No files provided" });
                }

                // Validate files
                var validation = await uploadService.ValidateMultipleFilesAsync(files);
                if (!validation.IsValid)
                {
                    return Results.BadRequest(new { 
                        message = "File validation failed", 
                        errors = validation.Errors 
                    });
                }

                // Upload files
                var result = await uploadService.UploadMultipleMediaFilesAsync(files);

                // Generate full URLs for uploaded files
                var uploadedFilesWithUrls = result.UploadedFiles.Select(file => file with
                {
                    FileUrl = file.MediaType == Models.MediaType.Image
                        ? $"{context.Request.Scheme}://{context.Request.Host}/api/images/{file.FileName}"
                        : $"{context.Request.Scheme}://{context.Request.Host}/api/videos/{file.FileName}"
                }).ToList();

                var response = result with { UploadedFiles = uploadedFilesWithUrls };

                // Return partial success if some files failed
                if (result.FailedUploads > 0 && result.SuccessfulUploads > 0)
                {
                    return Results.Json(response, statusCode: 207); // Multi-Status
                }

                // Return success if all files uploaded
                if (result.FailedUploads == 0)
                {
                    return Results.Ok(response);
                }

                // Return bad request if all files failed
                return Results.BadRequest(response);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Multiple File Upload Error"
                );
            }
        })
        .WithName("UploadMultipleMediaFiles")
        .WithSummary("Upload multiple image and video files")
        .WithDescription(@"Upload up to 10 image and video files at once.

**Supported Image Formats:** JPG, JPEG, PNG, GIF, WebP (max 5MB each)
**Supported Video Formats:** MP4, AVI, MOV, WMV, FLV, WebM, MKV (max 100MB each)

**Validation Rules:**
- Maximum 10 files per request
- Each image file must be ≤ 5MB
- Each video file must be ≤ 100MB
- Only supported file formats are allowed

**Response Codes:**
- 200: All files uploaded successfully
- 207: Partial success (some files uploaded, some failed)
- 400: Validation failed or all files failed to upload
- 401: Authentication required")
        .RequireAuthorization("ActiveUser")
        .Accepts<IFormFileCollection>("multipart/form-data")
        .Produces<MultipleFileUploadResponseDto>(200)
        .Produces<MultipleFileUploadResponseDto>(207)
        .Produces(400)
        .Produces(401)
        .DisableAntiforgery();

        // Get upload limits and supported formats
        uploads.MapGet("/limits", (IMultipleFileUploadService uploadService) =>
        {
            return Results.Ok(new
            {
                maxFiles = uploadService.MaxFilesAllowed,
                maxImageSizeMB = uploadService.MaxImageSizeBytes / (1024 * 1024),
                maxVideoSizeMB = uploadService.MaxVideoSizeBytes / (1024 * 1024),
                supportedImageFormats = new[] { "JPG", "JPEG", "PNG", "GIF", "WebP" },
                supportedVideoFormats = new[] { "MP4", "AVI", "MOV", "WMV", "FLV", "WebM", "MKV" }
            });
        })
        .WithName("GetUploadLimits")
        .WithSummary("Get upload limits and supported file formats")
        .WithDescription(@"Returns the current upload limits and supported file formats for multiple file uploads.

**Response includes:**
- Maximum number of files allowed per upload
- Maximum file size for images (in MB)
- Maximum file size for videos (in MB)
- List of supported image formats
- List of supported video formats")
        .Produces<object>(200);
    }
}
