using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Yapplr.Api.Services;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Endpoints;

public static class TrendingEndpoints
{
    public static void MapTrendingEndpoints(this IEndpointRouteBuilder app)
    {
        var trending = app.MapGroup("/api/trending")
            .WithTags("Trending")
            .WithOpenApi();

        // Get trending posts
        trending.MapGet("/posts", async (ITrendingService trendingService, ClaimsPrincipal user,
            int timeWindow = 24, int limit = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var posts = await trendingService.GetTrendingPostsAsync(timeWindow, limit, currentUserId);
            return Results.Ok(posts);
        })
        .WithName("GetTrendingPosts")
        .WithSummary("Get trending posts based on engagement velocity and quality")
        .Produces<IEnumerable<PostDto>>(200)
        .AllowAnonymous(); // Allow anonymous access to trending posts

        // Get trending posts with detailed scores (for analytics/debugging)
        trending.MapGet("/posts/detailed", [Authorize] async (ITrendingService trendingService, ClaimsPrincipal user,
            int timeWindow = 24, int limit = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var posts = await trendingService.GetTrendingPostsWithScoresAsync(timeWindow, limit, currentUserId);
            return Results.Ok(posts);
        })
        .WithName("GetTrendingPostsDetailed")
        .WithSummary("Get trending posts with detailed scoring information")
        .Produces<IEnumerable<TrendingPostDto>>(200);

        // Get trending posts by hashtag
        trending.MapGet("/posts/hashtag/{hashtag?}", async (ITrendingService trendingService, ClaimsPrincipal user,
            string? hashtag = null, int timeWindow = 24, int limit = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var posts = await trendingService.GetTrendingPostsByHashtagAsync(hashtag, timeWindow, limit, currentUserId);
            return Results.Ok(posts);
        })
        .WithName("GetTrendingPostsByHashtag")
        .WithSummary("Get trending posts filtered by hashtag")
        .Produces<IEnumerable<PostDto>>(200)
        .AllowAnonymous();

        // Get personalized trending posts
        trending.MapGet("/posts/personalized", [Authorize] async (ITrendingService trendingService, ClaimsPrincipal user,
            int timeWindow = 24, int limit = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            if (!currentUserId.HasValue)
            {
                return Results.Unauthorized();
            }

            var posts = await trendingService.GetPersonalizedTrendingPostsAsync(currentUserId.Value, timeWindow, limit);
            return Results.Ok(posts);
        })
        .WithName("GetPersonalizedTrendingPosts")
        .WithSummary("Get personalized trending posts based on user interests and following")
        .Produces<IEnumerable<PostDto>>(200)
        .Produces(401);

        // Calculate trending score for a specific post
        trending.MapGet("/posts/{postId:int}/score", [Authorize] async (ITrendingService trendingService, 
            int postId, int timeWindow = 24) =>
        {
            try
            {
                var score = await trendingService.CalculatePostTrendingScoreAsync(postId, timeWindow);
                return Results.Ok(score);
            }
            catch (ArgumentException)
            {
                return Results.NotFound($"Post with ID {postId} not found");
            }
        })
        .WithName("GetPostTrendingScore")
        .WithSummary("Calculate trending score for a specific post")
        .Produces<TrendingScoreDto>(200)
        .Produces(404);

        // Get trending analytics (admin only)
        trending.MapGet("/analytics", [Authorize(Policy = "Admin")] async (ITrendingService trendingService,
            int timeWindow = 24) =>
        {
            var analytics = await trendingService.GetTrendingAnalyticsAsync(timeWindow);
            return Results.Ok(analytics);
        })
        .WithName("GetTrendingAnalytics")
        .WithSummary("Get trending posts analytics for admin dashboard")
        .Produces<TrendingAnalyticsDto>(200)
        .Produces(403);

        // Alternative endpoints with different time windows for convenience
        trending.MapGet("/posts/now", async (ITrendingService trendingService, ClaimsPrincipal user, int limit = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var posts = await trendingService.GetTrendingPostsAsync(6, limit, currentUserId); // Last 6 hours
            return Results.Ok(posts);
        })
        .WithName("GetTrendingPostsNow")
        .WithSummary("Get trending posts from the last 6 hours")
        .Produces<IEnumerable<PostDto>>(200)
        .AllowAnonymous();

        trending.MapGet("/posts/today", async (ITrendingService trendingService, ClaimsPrincipal user, int limit = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var posts = await trendingService.GetTrendingPostsAsync(24, limit, currentUserId); // Last 24 hours
            return Results.Ok(posts);
        })
        .WithName("GetTrendingPostsToday")
        .WithSummary("Get trending posts from the last 24 hours")
        .Produces<IEnumerable<PostDto>>(200)
        .AllowAnonymous();

        trending.MapGet("/posts/week", async (ITrendingService trendingService, ClaimsPrincipal user, int limit = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var posts = await trendingService.GetTrendingPostsAsync(168, limit, currentUserId); // Last 7 days
            return Results.Ok(posts);
        })
        .WithName("GetTrendingPostsWeek")
        .WithSummary("Get trending posts from the last 7 days")
        .Produces<IEnumerable<PostDto>>(200)
        .AllowAnonymous();
    }
}
