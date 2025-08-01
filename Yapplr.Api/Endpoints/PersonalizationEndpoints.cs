using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class PersonalizationEndpoints
{
    public static void MapPersonalizationEndpoints(this IEndpointRouteBuilder app)
    {
        var personalization = app.MapGroup("/api/personalization")
            .WithTags("Personalization")
            .WithOpenApi();

        #region User Profiling

        // Get user personalization profile
        personalization.MapGet("/profile", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user) =>
        {
            var userId = user.GetUserId();
            var profile = await service.GetUserProfileAsync(userId);
            
            if (profile == null)
                return Results.NotFound("Personalization profile not found");
                
            return Results.Ok(profile);
        })
        .WithName("GetPersonalizationProfile")
        .WithSummary("Get user's personalization profile")
        .Produces<UserPersonalizationProfileDto>(200)
        .Produces(401)
        .Produces(404);

        // Update user personalization profile
        personalization.MapPost("/profile/update", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user,
            bool forceRebuild = false) =>
        {
            var userId = user.GetUserId();
            var updatedProfile = await service.UpdateUserProfileAsync(userId, forceRebuild);
            return Results.Ok(updatedProfile);
        })
        .WithName("UpdatePersonalizationProfile")
        .WithSummary("Update user's personalization profile")
        .Produces<UserPersonalizationProfileDto>(200)
        .Produces(401);

        // Get personalization insights
        personalization.MapGet("/insights", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user) =>
        {
            var userId = user.GetUserId();
            var insights = await service.GetPersonalizationInsightsAsync(userId);
            return Results.Ok(insights);
        })
        .WithName("GetPersonalizationInsights")
        .WithSummary("Get personalization insights for user dashboard")
        .Produces<PersonalizationInsightsDto>(200)
        .Produces(401);

        // Track user interaction
        personalization.MapPost("/interactions", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user,
            UserInteractionEventDto interactionEvent) =>
        {
            var userId = user.GetUserId();
            interactionEvent = interactionEvent with { UserId = userId };
            
            var success = await service.TrackInteractionAsync(interactionEvent);
            return success ? Results.Ok() : Results.BadRequest("Failed to track interaction");
        })
        .WithName("TrackUserInteraction")
        .WithSummary("Track user interaction for personalization learning")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        #endregion

        #region Content Recommendations

        // Get personalized recommendations
        personalization.MapGet("/recommendations/{contentType}", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user,
            string contentType,
            int limit = 20) =>
        {
            var userId = user.GetUserId();
            var recommendations = await service.GetPersonalizedRecommendationsAsync(userId, contentType, limit);
            return Results.Ok(recommendations);
        })
        .WithName("GetPersonalizedRecommendations")
        .WithSummary("Get personalized content recommendations")
        .Produces<IEnumerable<PersonalizedRecommendationDto>>(200)
        .Produces(401);

        // Get personalized feed
        personalization.MapGet("/feed", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user,
            int postLimit = 20,
            float diversityWeight = 0.3f,
            float noveltyWeight = 0.2f,
            float socialWeight = 0.25f,
            float qualityThreshold = 0.3f,
            bool includeExperimental = false) =>
        {
            var userId = user.GetUserId();
            var config = new PersonalizedFeedConfigDto(
                UserId: userId,
                PostLimit: postLimit,
                DiversityWeight: diversityWeight,
                NoveltyWeight: noveltyWeight,
                SocialWeight: socialWeight,
                QualityThreshold: qualityThreshold,
                IncludeExperimental: includeExperimental,
                PreferredContentTypes: new[] { "text", "image", "video" },
                ExcludedTopics: Enumerable.Empty<string>(),
                FeedType: "main"
            );
            
            var feed = await service.GetPersonalizedFeedAsync(userId, config);
            return Results.Ok(feed);
        })
        .WithName("GetPersonalizedFeed")
        .WithSummary("Get personalized feed with advanced algorithms")
        .Produces<IEnumerable<PersonalizedRecommendationDto>>(200)
        .Produces(401);

        // Get personalized search results
        personalization.MapGet("/search", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user,
            string query,
            string? contentTypes = null,
            int limit = 20) =>
        {
            var userId = user.GetUserId();
            var contentTypeList = contentTypes?.Split(',') ?? new[] { "posts", "users", "topics", "hashtags" };
            
            var searchResults = await service.GetPersonalizedSearchAsync(userId, query, contentTypeList, limit);
            return Results.Ok(searchResults);
        })
        .WithName("GetPersonalizedSearch")
        .WithSummary("Get personalized search results")
        .Produces<PersonalizedSearchResultDto>(200)
        .Produces(401);

        #endregion

        #region Similarity & Clustering

        // Calculate user similarity
        personalization.MapGet("/similarity/{targetUserId}", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user,
            int targetUserId) =>
        {
            var userId = user.GetUserId();
            var similarity = await service.CalculateUserSimilarityAsync(userId, targetUserId);
            return Results.Ok(new { userId, targetUserId, similarity });
        })
        .WithName("CalculateUserSimilarity")
        .WithSummary("Calculate similarity between current user and target user")
        .Produces<object>(200)
        .Produces(401);

        // Find similar users
        personalization.MapGet("/similar-users", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user,
            int limit = 10,
            float minSimilarity = 0.1f) =>
        {
            var userId = user.GetUserId();
            var similarUsers = await service.FindSimilarUsersAsync(userId, limit, minSimilarity);
            return Results.Ok(similarUsers);
        })
        .WithName("FindSimilarUsers")
        .WithSummary("Find users similar to the current user")
        .Produces<IEnumerable<UserSimilarityDto>>(200)
        .Produces(401);

        // Calculate content similarity
        personalization.MapGet("/content-similarity/{contentType}/{contentId}", async (IAdvancedPersonalizationService service,
            string contentType,
            int contentId,
            string candidateIds) =>
        {
            var candidateIdList = candidateIds.Split(',').Select(int.Parse);
            var similarities = await service.CalculateContentSimilarityAsync(contentType, contentId, candidateIdList);
            return Results.Ok(similarities);
        })
        .WithName("CalculateContentSimilarity")
        .WithSummary("Calculate similarity between content items")
        .Produces<IEnumerable<ContentSimilarityDto>>(200)
        .AllowAnonymous();

        // Generate content embedding
        personalization.MapPost("/embeddings/{contentType}/{contentId}", async (IAdvancedPersonalizationService service,
            string contentType,
            int contentId) =>
        {
            var embedding = await service.GenerateContentEmbeddingAsync(contentType, contentId);
            
            if (embedding == null)
                return Results.NotFound("Content not found or embedding generation failed");
                
            return Results.Ok(embedding);
        })
        .WithName("GenerateContentEmbedding")
        .WithSummary("Generate content embedding for similarity calculations")
        .Produces<ContentEmbeddingDto>(200)
        .Produces(404)
        .AllowAnonymous();

        #endregion

        #region Experimentation

        // Get user experiments
        personalization.MapGet("/experiments", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user) =>
        {
            var userId = user.GetUserId();
            var experiments = await service.GetUserExperimentsAsync(userId);
            return Results.Ok(experiments);
        })
        .WithName("GetUserExperiments")
        .WithSummary("Get active experiments for the user")
        .Produces<IEnumerable<UserExperimentParticipationDto>>(200)
        .Produces(401);

        // Assign experiment variant
        personalization.MapPost("/experiments/{experimentName}/assign", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user,
            string experimentName) =>
        {
            var userId = user.GetUserId();
            var variant = await service.AssignExperimentVariantAsync(userId, experimentName);
            
            if (variant == null)
                return Results.NotFound("Experiment not found or user not eligible");
                
            return Results.Ok(new { experimentName, variant });
        })
        .WithName("AssignExperimentVariant")
        .WithSummary("Assign user to experiment variant")
        .Produces<object>(200)
        .Produces(401)
        .Produces(404);

        // Get experiment results (admin only)
        personalization.MapGet("/experiments/{experimentName}/results", [Authorize(Policy = "Admin")] async (IAdvancedPersonalizationService service,
            string experimentName) =>
        {
            var results = await service.GetExperimentResultsAsync(experimentName);
            return Results.Ok(results);
        })
        .WithName("GetExperimentResults")
        .WithSummary("Get A/B test results for an experiment")
        .Produces<IEnumerable<PersonalizationABTestResultDto>>(200)
        .Produces(401)
        .Produces(403);

        #endregion

        #region Analytics & Monitoring

        // Get personalization metrics (admin only)
        personalization.MapGet("/metrics", [Authorize(Policy = "Admin")] async (IAdvancedPersonalizationService service,
            int timeWindowHours = 24) =>
        {
            var timeWindow = TimeSpan.FromHours(timeWindowHours);
            var metrics = await service.GetPersonalizationMetricsAsync(timeWindow);
            return Results.Ok(metrics);
        })
        .WithName("GetPersonalizationMetrics")
        .WithSummary("Get personalization algorithm performance metrics")
        .Produces<PersonalizationMetricsDto>(200)
        .Produces(401)
        .Produces(403);

        // Get personalization updates
        personalization.MapGet("/updates", [Authorize] async (IAdvancedPersonalizationService service, ClaimsPrincipal user,
            DateTime? since = null) =>
        {
            var userId = user.GetUserId();
            var sinceDate = since ?? DateTime.UtcNow.AddHours(-24);
            var updates = await service.GetPersonalizationUpdatesAsync(userId, sinceDate);
            return Results.Ok(updates);
        })
        .WithName("GetPersonalizationUpdates")
        .WithSummary("Get recent personalization updates for the user")
        .Produces<IEnumerable<PersonalizationUpdateDto>>(200)
        .Produces(401);

        // Validate model performance (admin only)
        personalization.MapPost("/validate", [Authorize(Policy = "Admin")] async (IAdvancedPersonalizationService service,
            IEnumerable<int> testUserIds) =>
        {
            var validationMetrics = await service.ValidateModelPerformanceAsync(testUserIds);
            return Results.Ok(validationMetrics);
        })
        .WithName("ValidateModelPerformance")
        .WithSummary("Validate personalization model performance")
        .Produces<PersonalizationMetricsDto>(200)
        .Produces(401)
        .Produces(403);

        #endregion

        #region Configuration

        // Get algorithm configuration (admin only)
        personalization.MapGet("/config", [Authorize(Policy = "Admin")] async (IAdvancedPersonalizationService service) =>
        {
            var config = await service.GetAlgorithmConfigurationAsync();
            return Results.Ok(config);
        })
        .WithName("GetAlgorithmConfiguration")
        .WithSummary("Get current algorithm configuration")
        .Produces<Dictionary<string, object>>(200)
        .Produces(401)
        .Produces(403);

        // Update algorithm configuration (admin only)
        personalization.MapPut("/config", [Authorize(Policy = "Admin")] async (IAdvancedPersonalizationService service,
            Dictionary<string, object> parameters) =>
        {
            var success = await service.UpdateAlgorithmParametersAsync(parameters);
            return success ? Results.Ok() : Results.BadRequest("Failed to update parameters");
        })
        .WithName("UpdateAlgorithmConfiguration")
        .WithSummary("Update algorithm parameters")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403);

        #endregion
    }
}
