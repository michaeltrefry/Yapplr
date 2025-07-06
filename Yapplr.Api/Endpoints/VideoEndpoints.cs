using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class VideoEndpoints
{
    public static void MapVideoEndpoints(this IEndpointRouteBuilder app)
    {
        var videos = app.MapGroup("/api/videos").WithTags("Videos");

        videos.MapPost("/upload", [Authorize] async (
            IFormFile file, 
            IVideoService videoService, 
            HttpContext context,
            ClaimsPrincipal user) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { message = "No file provided" });
                }

                if (!videoService.IsValidVideoFile(file))
                {
                    return Results.BadRequest(new { message = "Invalid video file. Supported formats: MP4, WebM, MOV, AVI, MKV. Max size: 100MB" });
                }

                var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var uploadResult = await videoService.SaveVideoAsync(file);
                
                // Queue the video for processing
                var processingJob = await videoService.QueueVideoProcessingAsync(
                    uploadResult.FileName, 
                    userId);

                var videoUrl = $"{context.Request.Scheme}://{context.Request.Host}/api/videos/{uploadResult.FileName}";

                var response = new VideoUploadResponseDto(
                    uploadResult.FileName,
                    videoUrl,
                    uploadResult.SizeBytes,
                    uploadResult.OriginalFileName,
                    uploadResult.ContentType,
                    processingJob.Id
                );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UploadVideo")
        .WithSummary("Upload a video file")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<VideoUploadResponseDto>(200)
        .Produces(400);

        videos.MapGet("/{fileName}", async (
            string fileName, 
            IStorageService storageService,
            HttpContext context) =>
        {
            try
            {
                if (!await storageService.FileExistsAsync("videos", fileName))
                {
                    return Results.NotFound();
                }

                var stream = await storageService.GetFileStreamAsync("videos", fileName);
                var contentType = GetVideoContentType(fileName);

                // Support for HTTP Range requests (video seeking)
                var rangeHeader = context.Request.Headers.Range.FirstOrDefault();
                if (!string.IsNullOrEmpty(rangeHeader))
                {
                    return await HandleRangeRequest(stream, rangeHeader, contentType);
                }

                return Results.File(stream, contentType, enableRangeProcessing: true);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error serving video: {ex.Message}");
            }
        })
        .WithName("GetVideo")
        .WithSummary("Stream a video file")
        .Produces(200)
        .Produces(206) // Partial Content
        .Produces(404);

        videos.MapGet("/thumbnails/{fileName}", async (
            string fileName, 
            IStorageService storageService) =>
        {
            try
            {
                if (!await storageService.FileExistsAsync("thumbnails", fileName))
                {
                    return Results.NotFound();
                }

                var stream = await storageService.GetFileStreamAsync("thumbnails", fileName);
                var contentType = GetImageContentType(fileName);

                return Results.File(stream, contentType);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error serving thumbnail: {ex.Message}");
            }
        })
        .WithName("GetVideoThumbnail")
        .WithSummary("Get a video thumbnail")
        .Produces(200)
        .Produces(404);

        videos.MapGet("/processing-status/{jobId:int}", [Authorize] async (
            int jobId,
            IMessageQueueService messageQueueService,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var job = await messageQueueService.GetJobAsync(jobId);

                if (job == null)
                {
                    return Results.NotFound(new { message = "Processing job not found" });
                }

                // Ensure user can only check their own jobs
                if (job.UserId != userId)
                {
                    return Results.Forbid();
                }

                var statusDto = new VideoProcessingStatusDto(
                    job.Id,
                    job.Status,
                    job.ErrorMessage
                );

                return Results.Ok(statusDto);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error getting processing status: {ex.Message}");
            }
        })
        .WithName("GetVideoProcessingStatus")
        .WithSummary("Get video processing status")
        .Produces<VideoProcessingStatusDto>(200)
        .Produces(404)
        .Produces(403);

        videos.MapDelete("/{fileName}", [Authorize] async (
            string fileName,
            IStorageService storageService,
            ClaimsPrincipal user) =>
        {
            try
            {
                var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                // TODO: Add authorization check to ensure user owns the video
                
                var deleted = await storageService.DeleteFileAsync("videos", fileName);
                
                if (deleted)
                {
                    // Also try to delete thumbnail if it exists
                    var thumbnailFileName = Path.GetFileNameWithoutExtension(fileName) + "_thumb.jpg";
                    await storageService.DeleteFileAsync("thumbnails", thumbnailFileName);
                    
                    return Results.Ok(new { message = "Video deleted successfully" });
                }
                
                return Results.NotFound(new { message = "Video not found" });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error deleting video: {ex.Message}");
            }
        })
        .WithName("DeleteVideo")
        .WithSummary("Delete a video file")
        .Produces(200)
        .Produces(404);
    }

    private static string GetVideoContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            _ => "application/octet-stream"
        };
    }

    private static string GetImageContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static async Task<IResult> HandleRangeRequest(Stream stream, string rangeHeader, string contentType)
    {
        // Parse Range header (e.g., "bytes=0-1023")
        var range = rangeHeader.Replace("bytes=", "").Split('-');
        var start = long.Parse(range[0]);
        var end = range.Length > 1 && !string.IsNullOrEmpty(range[1]) 
            ? long.Parse(range[1]) 
            : stream.Length - 1;

        var length = end - start + 1;
        
        stream.Seek(start, SeekOrigin.Begin);
        
        var buffer = new byte[length];
        await stream.ReadExactlyAsync(buffer, 0, (int)length);
        
        var responseStream = new MemoryStream(buffer);
        
        return Results.File(responseStream, contentType, enableRangeProcessing: true);
    }
}
