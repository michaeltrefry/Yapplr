using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class ExploreEndpoints
{
    public static void MapExploreEndpoints(this IEndpointRouteBuilder app)
    {
        var explore = app.MapGroup("/api/explore")
            .WithTags("Explore")
            .WithOpenApi();

        // Main explore page endpoint
        explore.MapGet("/", async (IExploreService exploreService, ClaimsPrincipal user,
            int trendingPostsLimit = 20,
            int trendingHashtagsLimit = 15,
            int recommendedUsersLimit = 10,
            int timeWindowHours = 24,
            bool includePersonalized = true,
            bool includeUserRecommendations = true,
            double minSimilarityScore = 0.1) =>
        {
            var userId = user.GetUserIdOrNull();
            
            var config = new ExploreConfigDto(
                TrendingPostsLimit: trendingPostsLimit,
                TrendingHashtagsLimit: trendingHashtagsLimit,
                RecommendedUsersLimit: recommendedUsersLimit,
                TimeWindowHours: timeWindowHours,
                IncludePersonalizedContent: includePersonalized && userId.HasValue,
                IncludeUserRecommendations: includeUserRecommendations && userId.HasValue,
                PreferredCategories: new[] { "Technology", "Sports", "Arts & Entertainment" },
                MinSimilarityScore: minSimilarityScore
            );

            var explorePage = await exploreService.GetExplorePageAsync(userId, config);
            return Results.Ok(explorePage);
        })
        .WithName("GetExplorePage")
        .WithSummary("Get comprehensive explore page with trending content and recommendations")
        .Produces<ExplorePageDto>(200)
        .AllowAnonymous();

        // User recommendations endpoint
        explore.MapGet("/users/recommended", [Authorize] async (IExploreService exploreService, ClaimsPrincipal user,
            int limit = 10,
            double minSimilarityScore = 0.1) =>
        {
            var userId = user.GetUserId();
            var recommendations = await exploreService.GetUserRecommendationsAsync(userId, limit, minSimilarityScore);
            return Results.Ok(recommendations);
        })
        .WithName("GetUserRecommendations")
        .WithSummary("Get personalized user recommendations based on similarity and interests")
        .Produces<IEnumerable<UserRecommendationDto>>(200)
        .Produces(401);

        // Similar users endpoint
        explore.MapGet("/users/similar", [Authorize] async (IExploreService exploreService, ClaimsPrincipal user,
            int limit = 10) =>
        {
            var userId = user.GetUserId();
            var similarUsers = await exploreService.GetSimilarUsersAsync(userId, limit);
            return Results.Ok(similarUsers);
        })
        .WithName("GetSimilarUsers")
        .WithSummary("Discover users with similar interests and interaction patterns")
        .Produces<IEnumerable<SimilarUserDto>>(200)
        .Produces(401);

        // Content clusters endpoint
        explore.MapGet("/content/clusters", async (IExploreService exploreService, ClaimsPrincipal user,
            int limit = 5) =>
        {
            var userId = user.GetUserIdOrNull();
            var clusters = await exploreService.GetContentClustersAsync(userId, limit);
            return Results.Ok(clusters);
        })
        .WithName("GetContentClusters")
        .WithSummary("Get content clusters for topic-based discovery")
        .Produces<IEnumerable<ContentClusterDto>>(200)
        .AllowAnonymous();

        // Interest-based content endpoint
        explore.MapGet("/content/interests", [Authorize] async (IExploreService exploreService, ClaimsPrincipal user,
            int limit = 5) =>
        {
            var userId = user.GetUserId();
            var interestContent = await exploreService.GetInterestBasedContentAsync(userId, limit);
            return Results.Ok(interestContent);
        })
        .WithName("GetInterestBasedContent")
        .WithSummary("Get content recommendations based on user interests")
        .Produces<IEnumerable<InterestBasedContentDto>>(200)
        .Produces(401);

        // Trending topics endpoint
        explore.MapGet("/topics/trending", async (IExploreService exploreService, ClaimsPrincipal user,
            int timeWindow = 24,
            int limit = 10) =>
        {
            var userId = user.GetUserIdOrNull();
            var trendingTopics = await exploreService.GetTrendingTopicsAsync(timeWindow, limit, userId);
            return Results.Ok(trendingTopics);
        })
        .WithName("GetExploreTrendingTopics")
        .WithSummary("Get trending topics with cross-content analysis")
        .Produces<IEnumerable<TrendingTopicDto>>(200)
        .AllowAnonymous();

        // Network-based user discovery endpoint
        explore.MapGet("/users/network", [Authorize] async (IExploreService exploreService, ClaimsPrincipal user,
            int maxDegrees = 3,
            int limit = 10) =>
        {
            var userId = user.GetUserId();
            var networkUsers = await exploreService.GetNetworkBasedUsersAsync(userId, maxDegrees, limit);
            return Results.Ok(networkUsers);
        })
        .WithName("GetNetworkBasedUsers")
        .WithSummary("Discover users through network analysis (friends of friends)")
        .Produces<IEnumerable<NetworkBasedUserDto>>(200)
        .Produces(401);

        // Explained content recommendations endpoint
        explore.MapGet("/content/explained", [Authorize] async (IExploreService exploreService, ClaimsPrincipal user,
            int limit = 20) =>
        {
            var userId = user.GetUserId();
            var explainedContent = await exploreService.GetExplainedContentRecommendationsAsync(userId, limit);
            return Results.Ok(explainedContent);
        })
        .WithName("GetExplainedContentRecommendations")
        .WithSummary("Get content recommendations with explanations")
        .Produces<IEnumerable<ExplainedContentDto>>(200)
        .Produces(401);

        // Modular explore sections endpoint
        explore.MapGet("/sections", async (IExploreService exploreService, ClaimsPrincipal user,
            string[]? sectionTypes = null) =>
        {
            var userId = user.GetUserIdOrNull();
            var sections = await exploreService.GetExploreSectionsAsync(userId, sectionTypes);
            return Results.Ok(sections);
        })
        .WithName("GetExploreSections")
        .WithSummary("Get modular explore sections for flexible UI composition")
        .Produces<IEnumerable<ExploreSectionDto>>(200)
        .AllowAnonymous();

        // User similarity calculation endpoint (for debugging/analytics)
        explore.MapGet("/users/{targetUserId:int}/similarity", [Authorize] async (IExploreService exploreService, ClaimsPrincipal user,
            int targetUserId) =>
        {
            var userId = user.GetUserId();
            
            if (userId == targetUserId)
                return Results.BadRequest("Cannot calculate similarity with yourself");

            var similarity = await exploreService.CalculateUserSimilarityAsync(userId, targetUserId);
            return Results.Ok(new { userId, targetUserId, similarityScore = similarity });
        })
        .WithName("CalculateExploreUserSimilarity")
        .WithSummary("Calculate similarity score between current user and target user")
        .Produces<object>(200)
        .Produces(400)
        .Produces(401);

        // Quick discovery endpoints for specific content types
        explore.MapGet("/quick/posts", async (IExploreService exploreService, ClaimsPrincipal user) =>
        {
            var userId = user.GetUserIdOrNull();
            var explorePage = await exploreService.GetExplorePageAsync(userId);
            return Results.Ok(explorePage.TrendingPosts);
        })
        .WithName("GetQuickTrendingPosts")
        .WithSummary("Quick access to trending posts only")
        .Produces<IEnumerable<PostDto>>(200)
        .AllowAnonymous();

        explore.MapGet("/quick/hashtags", async (IExploreService exploreService, ClaimsPrincipal user) =>
        {
            var userId = user.GetUserIdOrNull();
            var explorePage = await exploreService.GetExplorePageAsync(userId);
            return Results.Ok(explorePage.TrendingHashtags);
        })
        .WithName("GetQuickTrendingHashtags")
        .WithSummary("Quick access to trending hashtags only")
        .Produces<IEnumerable<TrendingHashtagDto>>(200)
        .AllowAnonymous();

        explore.MapGet("/quick/users", [Authorize] async (IExploreService exploreService, ClaimsPrincipal user) =>
        {
            var userId = user.GetUserId();
            var recommendations = await exploreService.GetUserRecommendationsAsync(userId, 5);
            return Results.Ok(recommendations);
        })
        .WithName("GetQuickUserRecommendations")
        .WithSummary("Quick access to user recommendations only")
        .Produces<IEnumerable<UserRecommendationDto>>(200)
        .Produces(401);
    }
}
