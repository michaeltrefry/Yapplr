using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

/// <summary>
/// Advanced personalization service with AI-driven recommendations and user profiling
/// </summary>
public interface IAdvancedPersonalizationService
{
    #region User Profiling
    
    /// <summary>
    /// Get comprehensive personalization profile for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includePrivateData">Include sensitive personalization data</param>
    /// <returns>User personalization profile</returns>
    Task<UserPersonalizationProfileDto?> GetUserProfileAsync(int userId, bool includePrivateData = false);
    
    /// <summary>
    /// Update user personalization profile based on new interactions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="forceRebuild">Force complete profile rebuild</param>
    /// <returns>Updated profile</returns>
    Task<UserPersonalizationProfileDto> UpdateUserProfileAsync(int userId, bool forceRebuild = false);
    
    /// <summary>
    /// Get personalization insights for user dashboard
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Personalization insights</returns>
    Task<PersonalizationInsightsDto> GetPersonalizationInsightsAsync(int userId);
    
    /// <summary>
    /// Track user interaction for personalization learning
    /// </summary>
    /// <param name="interactionEvent">Interaction event data</param>
    /// <returns>Success status</returns>
    Task<bool> TrackInteractionAsync(UserInteractionEventDto interactionEvent);
    
    #endregion
    
    #region Content Recommendations
    
    /// <summary>
    /// Get personalized content recommendations
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="contentType">Type of content to recommend</param>
    /// <param name="limit">Maximum number of recommendations</param>
    /// <param name="config">Personalization configuration</param>
    /// <returns>Personalized recommendations</returns>
    Task<IEnumerable<PersonalizedRecommendationDto>> GetPersonalizedRecommendationsAsync(
        int userId, 
        string contentType, 
        int limit = 20, 
        PersonalizedFeedConfigDto? config = null);
    
    /// <summary>
    /// Get personalized feed with advanced algorithms
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="config">Feed configuration</param>
    /// <returns>Personalized feed</returns>
    Task<IEnumerable<PersonalizedRecommendationDto>> GetPersonalizedFeedAsync(
        int userId, 
        PersonalizedFeedConfigDto? config = null);
    
    /// <summary>
    /// Get personalized search results
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="query">Search query</param>
    /// <param name="contentTypes">Types of content to search</param>
    /// <param name="limit">Maximum results</param>
    /// <returns>Personalized search results</returns>
    Task<PersonalizedSearchResultDto> GetPersonalizedSearchAsync(
        int userId, 
        string query, 
        IEnumerable<string>? contentTypes = null, 
        int limit = 20);
    
    #endregion
    
    #region Similarity & Clustering
    
    /// <summary>
    /// Calculate similarity between two users
    /// </summary>
    /// <param name="userId1">First user ID</param>
    /// <param name="userId2">Second user ID</param>
    /// <returns>Similarity score (0.0 to 1.0)</returns>
    Task<float> CalculateUserSimilarityAsync(int userId1, int userId2);
    
    /// <summary>
    /// Find similar users for a given user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Maximum number of similar users</param>
    /// <param name="minSimilarity">Minimum similarity threshold</param>
    /// <returns>Similar users with similarity scores</returns>
    Task<IEnumerable<UserSimilarityDto>> FindSimilarUsersAsync(int userId, int limit = 10, float minSimilarity = 0.1f);
    
    /// <summary>
    /// Calculate content similarity
    /// </summary>
    /// <param name="contentType">Type of content</param>
    /// <param name="contentId">Content ID</param>
    /// <param name="candidateIds">Candidate content IDs to compare</param>
    /// <returns>Content similarity results</returns>
    Task<IEnumerable<ContentSimilarityDto>> CalculateContentSimilarityAsync(
        string contentType, 
        int contentId, 
        IEnumerable<int> candidateIds);
    
    /// <summary>
    /// Generate content embeddings for similarity calculations
    /// </summary>
    /// <param name="contentType">Type of content</param>
    /// <param name="contentId">Content ID</param>
    /// <returns>Content embedding</returns>
    Task<ContentEmbeddingDto?> GenerateContentEmbeddingAsync(string contentType, int contentId);
    
    #endregion
    
    #region Experimentation
    
    /// <summary>
    /// Get active experiments for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User's experiment participations</returns>
    Task<IEnumerable<UserExperimentParticipationDto>> GetUserExperimentsAsync(int userId);
    
    /// <summary>
    /// Assign user to experiment variant
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="experimentName">Experiment name</param>
    /// <returns>Assigned variant</returns>
    Task<string?> AssignExperimentVariantAsync(int userId, string experimentName);
    
    /// <summary>
    /// Get experiment results and performance metrics
    /// </summary>
    /// <param name="experimentName">Experiment name</param>
    /// <returns>A/B test results</returns>
    Task<IEnumerable<PersonalizationABTestResultDto>> GetExperimentResultsAsync(string experimentName);
    
    #endregion
    
    #region Batch Operations
    
    /// <summary>
    /// Process batch personalization requests
    /// </summary>
    /// <param name="request">Batch request</param>
    /// <returns>Batch response with recommendations</returns>
    Task<BatchPersonalizationResponseDto> ProcessBatchPersonalizationAsync(BatchPersonalizationRequestDto request);
    
    /// <summary>
    /// Rebuild personalization profiles for multiple users
    /// </summary>
    /// <param name="userIds">User IDs to rebuild</param>
    /// <param name="batchSize">Processing batch size</param>
    /// <returns>Number of profiles rebuilt</returns>
    Task<int> RebuildUserProfilesBatchAsync(IEnumerable<int> userIds, int batchSize = 100);
    
    #endregion
    
    #region Analytics & Monitoring
    
    /// <summary>
    /// Get personalization algorithm performance metrics
    /// </summary>
    /// <param name="timeWindow">Time window for metrics</param>
    /// <returns>Performance metrics</returns>
    Task<PersonalizationMetricsDto> GetPersonalizationMetricsAsync(TimeSpan timeWindow);
    
    /// <summary>
    /// Get real-time personalization updates for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="since">Get updates since this time</param>
    /// <returns>Recent personalization updates</returns>
    Task<IEnumerable<PersonalizationUpdateDto>> GetPersonalizationUpdatesAsync(int userId, DateTime since);
    
    /// <summary>
    /// Generate training data for machine learning models
    /// </summary>
    /// <param name="userIds">User IDs to generate data for</param>
    /// <param name="startDate">Start date for data collection</param>
    /// <param name="endDate">End date for data collection</param>
    /// <returns>Training data points</returns>
    Task<IEnumerable<PersonalizationTrainingDataDto>> GenerateTrainingDataAsync(
        IEnumerable<int> userIds, 
        DateTime startDate, 
        DateTime endDate);
    
    #endregion
    
    #region Configuration & Tuning
    
    /// <summary>
    /// Update personalization algorithm parameters
    /// </summary>
    /// <param name="parameters">Algorithm parameters</param>
    /// <returns>Success status</returns>
    Task<bool> UpdateAlgorithmParametersAsync(Dictionary<string, object> parameters);
    
    /// <summary>
    /// Get current algorithm configuration
    /// </summary>
    /// <returns>Algorithm configuration</returns>
    Task<Dictionary<string, object>> GetAlgorithmConfigurationAsync();
    
    /// <summary>
    /// Validate personalization model performance
    /// </summary>
    /// <param name="testUserIds">User IDs for testing</param>
    /// <returns>Validation metrics</returns>
    Task<PersonalizationMetricsDto> ValidateModelPerformanceAsync(IEnumerable<int> testUserIds);
    
    #endregion
}
