using Yapplr.Api.Services;
using Yapplr.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Yapplr.Api.Endpoints;

public static class VideoEndpoints
{
    public static void MapVideoEndpoints(this IEndpointRouteBuilder app)
    {
        var videos = app.MapGroup("/api/videos")
            .WithTags("Videos")
            .WithOpenApi();

        videos.MapPost("/upload", async (IFormFile? file, IVideoService videoService, HttpContext context) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { message = "No file provided" });
                }

                if (!await videoService.IsValidVideoFileAsync(file))
                {
                    return Results.BadRequest(new { message = "Invalid video file. Please check file format and size limits." });
                }

                var fileName = await videoService.SaveVideoAsync(file);
                var response = await videoService.GetVideoUploadResponseAsync(fileName, context);

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UploadVideo")
        .WithSummary("Upload a video file")
        .RequireAuthorization("ActiveUser")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        videos.MapGet("/{fileName}", async (string fileName, IOptions<UploadsConfiguration> uploadsConfig) =>
        {
            var config = uploadsConfig.Value;
            var uploadsPath = Path.GetFullPath(config.GetVideosFullPath());
            var filePath = Path.Combine(uploadsPath, fileName);

            if (!File.Exists(filePath))
            {
                return Results.NotFound();
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".mp4" => "video/mp4",
                ".avi" => "video/avi",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".flv" => "video/x-flv",
                ".webm" => "video/webm",
                ".mkv" => "video/x-matroska",
                _ => "application/octet-stream"
            };

            // Use PhysicalFileResult for proper video streaming with range request support
            return Results.File(filePath, contentType, enableRangeProcessing: true);
        })
        .WithName("GetVideo")
        .WithSummary("Get an uploaded video")
        .Produces(200)
        .Produces(404);

        videos.MapGet("/processed/{fileName}", async (string fileName, IOptions<UploadsConfiguration> uploadsConfig, HttpContext context) =>
        {
            var config = uploadsConfig.Value;
            var processedPath = Path.GetFullPath(config.GetProcessedFullPath());
            var filePath = Path.Combine(processedPath, fileName);

            if (!File.Exists(filePath))
            {
                return Results.NotFound();
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".mp4" => "video/mp4",
                ".avi" => "video/avi",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".flv" => "video/x-flv",
                ".webm" => "video/webm",
                ".mkv" => "video/x-matroska",
                _ => "application/octet-stream"
            };

            // Use PhysicalFileResult for proper video streaming with range request support
            return Results.File(filePath, contentType, enableRangeProcessing: true);
        })
        .WithName("GetProcessedVideo")
        .WithSummary("Get a processed video")
        .Produces(200)
        .Produces(404);

        videos.MapGet("/thumbnails/{fileName}", async (string fileName, IOptions<UploadsConfiguration> uploadsConfig) =>
        {
            var config = uploadsConfig.Value;
            var thumbnailPath = Path.GetFullPath(config.GetThumbnailsFullPath());
            var filePath = Path.Combine(thumbnailPath, fileName);

            if (!File.Exists(filePath))
            {
                return Results.NotFound();
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var fileBytes = await File.ReadAllBytesAsync(filePath);
            return Results.File(fileBytes, contentType);
        })
        .WithName("GetVideoThumbnail")
        .WithSummary("Get a video thumbnail")
        .Produces(200)
        .Produces(404);

        videos.MapDelete("/{fileName}", (string fileName, IVideoService videoService) =>
        {
            var success = videoService.DeleteVideo(fileName);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteVideo")
        .WithSummary("Delete an uploaded video")
        .RequireAuthorization("ActiveUser")
        .Produces(204)
        .Produces(401)
        .Produces(404);
    }
}
