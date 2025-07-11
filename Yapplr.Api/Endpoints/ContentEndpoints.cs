using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class ContentEndpoints
{
    public static void MapContentEndpoints(this WebApplication app)
    {
        var content = app.MapGroup("/api/content").WithTags("Content");

        // Public endpoints for published content
        content.MapGet("/pages/{slug}", async (string slug, IContentManagementService contentService) =>
        {
            var version = await contentService.GetPublishedVersionBySlugAsync(slug);
            return version == null ? Results.NotFound() : Results.Ok(version);
        })
        .WithName("GetPublishedContentBySlug")
        .WithSummary("Get published content by slug")
        .Produces<ContentPageVersionDto>(200)
        .Produces(404);

        content.MapGet("/pages/type/{type}", async (ContentPageType type, IContentManagementService contentService) =>
        {
            var version = await contentService.GetPublishedVersionByTypeAsync(type);
            return version == null ? Results.NotFound() : Results.Ok(version);
        })
        .WithName("GetPublishedContentByType")
        .WithSummary("Get published content by type")
        .Produces<ContentPageVersionDto>(200)
        .Produces(404);

        // Convenience endpoints for specific content types
        content.MapGet("/terms", async (IContentManagementService contentService) =>
        {
            var version = await contentService.GetPublishedVersionByTypeAsync(ContentPageType.TermsOfService);
            return version == null ? Results.NotFound() : Results.Ok(version);
        })
        .WithName("GetTermsOfService")
        .WithSummary("Get published Terms of Service")
        .Produces<ContentPageVersionDto>(200)
        .Produces(404);

        content.MapGet("/privacy", async (IContentManagementService contentService) =>
        {
            var version = await contentService.GetPublishedVersionByTypeAsync(ContentPageType.PrivacyPolicy);
            return version == null ? Results.NotFound() : Results.Ok(version);
        })
        .WithName("GetPrivacyPolicy")
        .WithSummary("Get published Privacy Policy")
        .Produces<ContentPageVersionDto>(200)
        .Produces(404);
    }
}
