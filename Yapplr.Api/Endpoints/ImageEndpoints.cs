using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        var images = app.MapGroup("/api/images").WithTags("Images");

        images.MapPost("/upload", [Authorize] async (IFormFile file, IImageService imageService, HttpContext context) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { message = "No file provided" });
                }

                if (!imageService.IsValidImageFile(file))
                {
                    return Results.BadRequest(new { message = "Invalid image file. Supported formats: JPG, PNG, GIF, WebP. Max size: 5MB" });
                }

                var fileName = await imageService.SaveImageAsync(file);
                var imageUrl = $"{context.Request.Scheme}://{context.Request.Host}/api/images/{fileName}";

                return Results.Ok(new { fileName, imageUrl });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("UploadImage")
        .WithSummary("Upload an image file")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<object>(200)
        .Produces(400)
        .Produces(401)
        .DisableAntiforgery();

        images.MapGet("/{fileName}", async (string fileName, IWebHostEnvironment environment) =>
        {
            var uploadsPath = Path.Combine(environment.ContentRootPath, "uploads", "images");
            var filePath = Path.Combine(uploadsPath, fileName);

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
        .WithName("GetImage")
        .WithSummary("Get an uploaded image")
        .Produces(200)
        .Produces(404);

        images.MapDelete("/{fileName}", [Authorize] async (string fileName, IImageService imageService) =>
        {
            var success = imageService.DeleteImage(fileName);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteImage")
        .WithSummary("Delete an uploaded image")
        .Produces(204)
        .Produces(401)
        .Produces(404);
    }
}
