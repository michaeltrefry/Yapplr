using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class TopicEndpoints
{
    public static void MapTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var topics = app.MapGroup("/api/topics")
            .WithTags("Topics")
            .WithOpenApi();

        #region Topic Management

        // Get all topics
        topics.MapGet("/", async (ITopicService topicService, ClaimsPrincipal user,
            string? category = null,
            bool? featured = null) =>
        {
            var userId = user.GetUserIdOrNull();
            var topicList = await topicService.GetTopicsAsync(category, featured, userId);
            return Results.Ok(topicList);
        })
        .WithName("GetTopics")
        .WithSummary("Get all available topics")
        .Produces<IEnumerable<TopicDto>>(200)
        .AllowAnonymous();

        // Get topic by ID or slug
        topics.MapGet("/{identifier}", async (ITopicService topicService, ClaimsPrincipal user, string identifier) =>
        {
            var userId = user.GetUserIdOrNull();
            var topic = await topicService.GetTopicAsync(identifier, userId);
            
            if (topic == null)
                return Results.NotFound($"Topic '{identifier}' not found");
                
            return Results.Ok(topic);
        })
        .WithName("GetTopic")
        .WithSummary("Get topic by ID or slug")
        .Produces<TopicDto>(200)
        .Produces(404)
        .AllowAnonymous();

        // Search topics
        topics.MapGet("/search", async (ITopicService topicService, ClaimsPrincipal user,
            string query,
            int limit = 20) =>
        {
            var userId = user.GetUserIdOrNull();
            var searchResults = await topicService.SearchTopicsAsync(query, userId, limit);
            return Results.Ok(searchResults);
        })
        .WithName("SearchTopics")
        .WithSummary("Search topics by name or hashtags")
        .Produces<TopicSearchResultDto>(200)
        .AllowAnonymous();

        // Get topic recommendations
        topics.MapGet("/recommendations", [Authorize] async (ITopicService topicService, ClaimsPrincipal user,
            int limit = 10) =>
        {
            var userId = user.GetUserId();
            var recommendations = await topicService.GetTopicRecommendationsAsync(userId, limit);
            return Results.Ok(recommendations);
        })
        .WithName("GetTopicRecommendations")
        .WithSummary("Get personalized topic recommendations")
        .Produces<IEnumerable<TopicRecommendationDto>>(200)
        .Produces(401);

        #endregion

        #region Topic Following

        // Follow a topic
        topics.MapPost("/follow", [Authorize] async (ITopicService topicService, ClaimsPrincipal user,
            CreateTopicFollowDto createDto) =>
        {
            var userId = user.GetUserId();
            
            try
            {
                var topicFollow = await topicService.FollowTopicAsync(userId, createDto);
                return Results.Created($"/api/topics/following/{topicFollow.TopicName}", topicFollow);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        })
        .WithName("FollowTopic")
        .WithSummary("Follow a topic")
        .Produces<TopicFollowDto>(201)
        .Produces(401)
        .Produces(409);

        // Unfollow a topic
        topics.MapDelete("/follow/{topicName}", [Authorize] async (ITopicService topicService, ClaimsPrincipal user,
            string topicName) =>
        {
            var userId = user.GetUserId();
            var success = await topicService.UnfollowTopicAsync(userId, topicName);
            
            if (!success)
                return Results.NotFound($"You are not following topic '{topicName}'");
                
            return Results.NoContent();
        })
        .WithName("UnfollowTopic")
        .WithSummary("Unfollow a topic")
        .Produces(204)
        .Produces(401)
        .Produces(404);

        // Update topic follow preferences
        topics.MapPatch("/follow/{topicName}", [Authorize] async (ITopicService topicService, ClaimsPrincipal user,
            string topicName,
            UpdateTopicFollowDto updateDto) =>
        {
            var userId = user.GetUserId();
            var updatedFollow = await topicService.UpdateTopicFollowAsync(userId, topicName, updateDto);
            
            if (updatedFollow == null)
                return Results.NotFound($"You are not following topic '{topicName}'");
                
            return Results.Ok(updatedFollow);
        })
        .WithName("UpdateTopicFollow")
        .WithSummary("Update topic follow preferences")
        .Produces<TopicFollowDto>(200)
        .Produces(401)
        .Produces(404);

        // Get user's followed topics
        topics.MapGet("/following", [Authorize] async (ITopicService topicService, ClaimsPrincipal user,
            bool? includeInMainFeed = null) =>
        {
            var userId = user.GetUserId();
            var followedTopics = await topicService.GetUserTopicsAsync(userId, includeInMainFeed);
            return Results.Ok(followedTopics);
        })
        .WithName("GetUserTopics")
        .WithSummary("Get user's followed topics")
        .Produces<IEnumerable<TopicFollowDto>>(200)
        .Produces(401);

        // Check if following a topic
        topics.MapGet("/following/{topicName}/status", [Authorize] async (ITopicService topicService, ClaimsPrincipal user,
            string topicName) =>
        {
            var userId = user.GetUserId();
            var isFollowing = await topicService.IsFollowingTopicAsync(userId, topicName);
            return Results.Ok(new { topicName, isFollowing });
        })
        .WithName("GetTopicFollowStatus")
        .WithSummary("Check if user is following a topic")
        .Produces<object>(200)
        .Produces(401);

        #endregion

        #region Topic Feeds

        // Get topic feed
        topics.MapGet("/{topicName}/feed", async (ITopicService topicService, ClaimsPrincipal user,
            string topicName,
            int postsPerTopic = 10,
            int timeWindowHours = 24,
            string sortBy = "personalized") =>
        {
            var userId = user.GetUserIdOrNull();
            var config = new TopicFeedConfigDto(
                PostsPerTopic: postsPerTopic,
                MaxTopics: 1,
                IncludeTrendingContent: true,
                IncludePersonalizedContent: userId.HasValue,
                MinInterestLevel: 0.1f,
                TimeWindowHours: timeWindowHours,
                SortBy: sortBy
            );
            
            var topicFeed = await topicService.GetTopicFeedAsync(topicName, userId, config);
            return Results.Ok(topicFeed);
        })
        .WithName("GetTopicFeed")
        .WithSummary("Get feed for a specific topic")
        .Produces<TopicFeedDto>(200)
        .AllowAnonymous();

        // Get personalized topic feed
        topics.MapGet("/feed/personalized", [Authorize] async (ITopicService topicService, ClaimsPrincipal user,
            int postsPerTopic = 10,
            int maxTopics = 5,
            int timeWindowHours = 24,
            string sortBy = "personalized") =>
        {
            var userId = user.GetUserId();
            var config = new TopicFeedConfigDto(
                PostsPerTopic: postsPerTopic,
                MaxTopics: maxTopics,
                IncludeTrendingContent: true,
                IncludePersonalizedContent: true,
                MinInterestLevel: 0.1f,
                TimeWindowHours: timeWindowHours,
                SortBy: sortBy
            );
            
            var personalizedFeed = await topicService.GetPersonalizedTopicFeedAsync(userId, config);
            return Results.Ok(personalizedFeed);
        })
        .WithName("GetPersonalizedTopicFeed")
        .WithSummary("Get personalized feed based on followed topics")
        .Produces<PersonalizedTopicFeedDto>(200)
        .Produces(401);

        // Get mixed topic feed
        topics.MapGet("/feed/mixed", [Authorize] async (ITopicService topicService, ClaimsPrincipal user,
            int postsPerTopic = 10,
            int maxTopics = 5,
            int timeWindowHours = 24) =>
        {
            var userId = user.GetUserId();
            var config = new TopicFeedConfigDto(
                PostsPerTopic: postsPerTopic,
                MaxTopics: maxTopics,
                IncludeTrendingContent: true,
                IncludePersonalizedContent: true,
                MinInterestLevel: 0.1f,
                TimeWindowHours: timeWindowHours,
                SortBy: "personalized"
            );
            
            var mixedFeed = await topicService.GetMixedTopicFeedAsync(userId, config);
            return Results.Ok(mixedFeed);
        })
        .WithName("GetMixedTopicFeed")
        .WithSummary("Get mixed feed combining followed topics")
        .Produces<IEnumerable<PostDto>>(200)
        .Produces(401);

        #endregion

        #region Topic Analytics

        // Get topic analytics
        topics.MapGet("/{topicName}/analytics", async (ITopicService topicService, string topicName,
            int days = 7) =>
        {
            var analytics = await topicService.GetTopicAnalyticsAsync(topicName, days);
            
            if (analytics == null)
                return Results.NotFound($"No analytics found for topic '{topicName}'");
                
            return Results.Ok(analytics);
        })
        .WithName("GetTopicAnalytics")
        .WithSummary("Get analytics for a topic")
        .Produces<TopicAnalyticsDto>(200)
        .Produces(404)
        .AllowAnonymous();

        // Get trending topics
        topics.MapGet("/trending", async (ITopicService topicService,
            int timeWindow = 24,
            int limit = 10,
            string? category = null) =>
        {
            var trendingTopics = await topicService.GetTrendingTopicsAsync(timeWindow, limit, category);
            return Results.Ok(trendingTopics);
        })
        .WithName("GetTrendingTopics")
        .WithSummary("Get trending topics")
        .Produces<IEnumerable<TopicTrendingDto>>(200)
        .AllowAnonymous();

        // Get topic statistics
        topics.MapGet("/{topicName}/stats", async (ITopicService topicService, string topicName) =>
        {
            var stats = await topicService.GetTopicStatsAsync(topicName);
            
            if (stats == null)
                return Results.NotFound($"No statistics found for topic '{topicName}'");
                
            return Results.Ok(stats);
        })
        .WithName("GetTopicStats")
        .WithSummary("Get topic statistics")
        .Produces<TopicStatsDto>(200)
        .Produces(404)
        .AllowAnonymous();

        #endregion

        #region Topic Clustering

        // Get topic clusters
        topics.MapGet("/clusters", async (ITopicService topicService, int limit = 10) =>
        {
            var clusters = await topicService.GetTopicClustersAsync(limit);
            return Results.Ok(clusters);
        })
        .WithName("GetTopicClusters")
        .WithSummary("Get topic clusters based on hashtag similarity")
        .Produces<IEnumerable<TopicClusterDto>>(200)
        .AllowAnonymous();

        // Get related topics
        topics.MapGet("/{topicName}/related", async (ITopicService topicService, string topicName,
            int limit = 5) =>
        {
            var relatedTopics = await topicService.GetRelatedTopicsAsync(topicName, limit);
            return Results.Ok(relatedTopics);
        })
        .WithName("GetRelatedTopics")
        .WithSummary("Get topics related to the specified topic")
        .Produces<IEnumerable<TopicDto>>(200)
        .AllowAnonymous();

        #endregion

        #region Bulk Operations

        // Bulk topic operations
        topics.MapPost("/bulk", [Authorize] async (ITopicService topicService, ClaimsPrincipal user,
            BulkTopicOperationDto operation) =>
        {
            var userId = user.GetUserId();
            var results = await topicService.BulkTopicOperationAsync(userId, operation);
            return Results.Ok(results);
        })
        .WithName("BulkTopicOperation")
        .WithSummary("Perform bulk operations on topics")
        .Produces<IEnumerable<TopicFollowDto>>(200)
        .Produces(401);

        // Import topics from hashtag usage
        topics.MapPost("/import", [Authorize] async (ITopicService topicService, ClaimsPrincipal user,
            int minUsageCount = 3) =>
        {
            var userId = user.GetUserId();
            var importedTopics = await topicService.ImportTopicsFromHashtagUsageAsync(userId, minUsageCount);
            return Results.Ok(importedTopics);
        })
        .WithName("ImportTopicsFromHashtagUsage")
        .WithSummary("Import topics based on user's hashtag usage")
        .Produces<IEnumerable<TopicFollowDto>>(200)
        .Produces(401);

        #endregion
    }
}
