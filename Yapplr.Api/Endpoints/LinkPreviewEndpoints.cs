using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.Authorization;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class LinkPreviewEndpoints
{
    public static void MapLinkPreviewEndpoints(this WebApplication app)
    {
        var linkPreviews = app.MapGroup("/api/link-previews").WithTags("Link Previews");

        // Get link preview by URL
        linkPreviews.MapGet("/", async ([FromQuery] string url, ILinkPreviewService linkPreviewService) =>
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return Results.BadRequest(new { message = "URL is required" });
            }

            var linkPreview = await linkPreviewService.GetLinkPreviewByUrlAsync(url);
            
            return linkPreview == null ? Results.NotFound() : Results.Ok(linkPreview);
        })
        .WithName("GetLinkPreview")
        .WithSummary("Get link preview by URL")
        .Produces<LinkPreviewDto>(200)
        .Produces(400)
        .Produces(404);

        // Create/fetch link preview for URL
        linkPreviews.MapPost("/", async ([FromBody] CreateLinkPreviewDto createDto, ILinkPreviewService linkPreviewService) =>
        {
            var linkPreview = await linkPreviewService.GetOrCreateLinkPreviewAsync(createDto.Url);

            return linkPreview == null ? Results.BadRequest() : Results.Ok(linkPreview);
        })
        .WithName("CreateLinkPreview")
        .WithSummary("Create or get existing link preview for URL")
        .RequireAuthorization("ActiveUser")
        .Produces<LinkPreviewDto>(200)
        .Produces(400)
        .Produces(401);

        // Process multiple URLs from post content
        linkPreviews.MapPost("/process", async ([FromBody] ProcessLinksDto processDto, ILinkPreviewService linkPreviewService) =>
        {
            var linkPreviews = await linkPreviewService.ProcessPostLinksAsync(processDto.Content);

            return Results.Ok(linkPreviews);
        })
        .WithName("ProcessPostLinks")
        .WithSummary("Process and create link previews for URLs found in post content")
        .RequireAuthorization("ActiveUser")
        .Produces<IEnumerable<LinkPreviewDto>>(200)
        .Produces(401);
    }
}

public record ProcessLinksDto(string Content);
