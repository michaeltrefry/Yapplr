using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this WebApplication app)
    {
        var tags = app.MapGroup("/api/tags").WithTags("Tags");

        // Search tags
        tags.MapGet("/search/{query}", async (string query, ITagService tagService, int limit = 20) =>
        {
            var tags = await tagService.SearchTagsAsync(query, limit);
            return Results.Ok(tags);
        })
        .WithName("SearchTags")
        .WithSummary("Search for hashtags")
        .Produces<IEnumerable<TagDto>>(200);

        // Get trending tags
        tags.MapGet("/trending", async (ITagService tagService, int limit = 10) =>
        {
            var tags = await tagService.GetTrendingTagsAsync(limit);
            return Results.Ok(tags);
        })
        .WithName("GetTrendingTags")
        .WithSummary("Get trending hashtags")
        .Produces<IEnumerable<TagDto>>(200);

        // Get tag by name
        tags.MapGet("/{tagName}", async (string tagName, ITagService tagService) =>
        {
            var tag = await tagService.GetTagByNameAsync(tagName);
            return tag == null ? Results.NotFound() : Results.Ok(tag);
        })
        .WithName("GetTag")
        .WithSummary("Get hashtag by name")
        .Produces<TagDto>(200)
        .Produces(404);

        // Get posts by tag
        tags.MapGet("/{tagName}/posts", async (string tagName, ClaimsPrincipal? user, ITagService tagService, int page = 1, int pageSize = 25) =>
        {
            var currentUserId = user?.Identity?.IsAuthenticated == true
                ? int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                : (int?)null;

            var posts = await tagService.GetPostsByTagAsync(tagName, currentUserId, page, pageSize);
            return Results.Ok(posts);
        })
        .WithName("GetPostsByTag")
        .WithSummary("Get posts containing a specific hashtag")
        .Produces<IEnumerable<PostDto>>(200);

        // Analytics endpoints
        tags.MapGet("/analytics/trending", async (ITagAnalyticsService analyticsService, int days = 7, int limit = 10) =>
        {
            var trendingTags = await analyticsService.GetTrendingTagsAsync(days, limit);
            return Results.Ok(trendingTags);
        })
        .WithName("GetTrendingTagsAnalytics")
        .WithSummary("Get trending hashtags based on recent activity")
        .Produces<IEnumerable<TagDto>>(200);

        tags.MapGet("/analytics/top", async (ITagAnalyticsService analyticsService, int limit = 20) =>
        {
            var topTags = await analyticsService.GetTopTagsAsync(limit);
            return Results.Ok(topTags);
        })
        .WithName("GetTopTags")
        .WithSummary("Get top hashtags by total post count")
        .Produces<IEnumerable<TagDto>>(200);

        tags.MapGet("/{tagName}/analytics", async (string tagName, ITagAnalyticsService analyticsService) =>
        {
            var analytics = await analyticsService.GetTagAnalyticsAsync(tagName);
            return analytics == null ? Results.NotFound() : Results.Ok(analytics);
        })
        .WithName("GetTagAnalytics")
        .WithSummary("Get detailed analytics for a specific hashtag")
        .Produces<TagAnalyticsDto>(200)
        .Produces(404);

        tags.MapGet("/{tagName}/usage", async (string tagName, ITagAnalyticsService analyticsService, int days = 30) =>
        {
            var usage = await analyticsService.GetTagUsageOverTimeAsync(tagName, days);
            return Results.Ok(usage);
        })
        .WithName("GetTagUsageOverTime")
        .WithSummary("Get hashtag usage over time")
        .Produces<IEnumerable<TagUsageDto>>(200);
    }
}
