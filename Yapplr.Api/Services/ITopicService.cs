using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for topic-based feed functionality
/// </summary>
public interface ITopicService
{
    #region Topic Management
    
    /// <summary>
    /// Get all available topics
    /// </summary>
    /// <param name="category">Filter by category (optional)</param>
    /// <param name="featured">Filter by featured status (optional)</param>
    /// <param name="userId">User ID for personalization (optional)</param>
    /// <returns>List of available topics</returns>
    Task<IEnumerable<TopicDto>> GetTopicsAsync(string? category = null, bool? featured = null, int? userId = null);
    
    /// <summary>
    /// Get topic by ID or slug
    /// </summary>
    /// <param name="identifier">Topic ID or slug</param>
    /// <param name="userId">User ID for follow status (optional)</param>
    /// <returns>Topic details</returns>
    Task<TopicDto?> GetTopicAsync(string identifier, int? userId = null);
    
    /// <summary>
    /// Search topics by name or hashtags
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="userId">User ID for personalization (optional)</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>Search results</returns>
    Task<TopicSearchResultDto> SearchTopicsAsync(string query, int? userId = null, int limit = 20);
    
    /// <summary>
    /// Get topic recommendations for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Maximum number of recommendations</param>
    /// <returns>Recommended topics</returns>
    Task<IEnumerable<TopicRecommendationDto>> GetTopicRecommendationsAsync(int userId, int limit = 10);
    
    #endregion
    
    #region Topic Following
    
    /// <summary>
    /// Follow a topic
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="createDto">Topic follow details</param>
    /// <returns>Created topic follow</returns>
    Task<TopicFollowDto> FollowTopicAsync(int userId, CreateTopicFollowDto createDto);
    
    /// <summary>
    /// Unfollow a topic
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="topicName">Topic name to unfollow</param>
    /// <returns>Success status</returns>
    Task<bool> UnfollowTopicAsync(int userId, string topicName);
    
    /// <summary>
    /// Update topic follow preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="topicName">Topic name</param>
    /// <param name="updateDto">Updated preferences</param>
    /// <returns>Updated topic follow</returns>
    Task<TopicFollowDto?> UpdateTopicFollowAsync(int userId, string topicName, UpdateTopicFollowDto updateDto);
    
    /// <summary>
    /// Get user's followed topics
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeInMainFeed">Filter by main feed inclusion (optional)</param>
    /// <returns>User's followed topics</returns>
    Task<IEnumerable<TopicFollowDto>> GetUserTopicsAsync(int userId, bool? includeInMainFeed = null);
    
    /// <summary>
    /// Check if user follows a topic
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="topicName">Topic name</param>
    /// <returns>Follow status</returns>
    Task<bool> IsFollowingTopicAsync(int userId, string topicName);
    
    #endregion
    
    #region Topic Feeds
    
    /// <summary>
    /// Get feed for a specific topic
    /// </summary>
    /// <param name="topicName">Topic name</param>
    /// <param name="userId">User ID for personalization (optional)</param>
    /// <param name="config">Feed configuration (optional)</param>
    /// <returns>Topic feed</returns>
    Task<TopicFeedDto> GetTopicFeedAsync(string topicName, int? userId = null, TopicFeedConfigDto? config = null);
    
    /// <summary>
    /// Get personalized feed based on user's followed topics
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="config">Feed configuration (optional)</param>
    /// <returns>Personalized topic feed</returns>
    Task<PersonalizedTopicFeedDto> GetPersonalizedTopicFeedAsync(int userId, TopicFeedConfigDto? config = null);
    
    /// <summary>
    /// Get mixed feed combining user follows and topic content
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="config">Feed configuration (optional)</param>
    /// <returns>Mixed feed posts</returns>
    Task<IEnumerable<PostDto>> GetMixedTopicFeedAsync(int userId, TopicFeedConfigDto? config = null);
    
    #endregion
    
    #region Topic Analytics
    
    /// <summary>
    /// Get analytics for a topic
    /// </summary>
    /// <param name="topicName">Topic name</param>
    /// <param name="days">Number of days to analyze</param>
    /// <returns>Topic analytics</returns>
    Task<TopicAnalyticsDto?> GetTopicAnalyticsAsync(string topicName, int days = 7);
    
    /// <summary>
    /// Get trending topics
    /// </summary>
    /// <param name="timeWindow">Time window in hours</param>
    /// <param name="limit">Maximum number of topics</param>
    /// <param name="category">Filter by category (optional)</param>
    /// <returns>Trending topics</returns>
    Task<IEnumerable<TopicTrendingDto>> GetTrendingTopicsAsync(int timeWindow = 24, int limit = 10, string? category = null);
    
    /// <summary>
    /// Get topic statistics
    /// </summary>
    /// <param name="topicName">Topic name</param>
    /// <returns>Topic statistics</returns>
    Task<TopicStatsDto?> GetTopicStatsAsync(string topicName);
    
    #endregion
    
    #region Topic Clustering
    
    /// <summary>
    /// Get topic clusters based on hashtag similarity
    /// </summary>
    /// <param name="limit">Maximum number of clusters</param>
    /// <returns>Topic clusters</returns>
    Task<IEnumerable<TopicClusterDto>> GetTopicClustersAsync(int limit = 10);
    
    /// <summary>
    /// Get related topics for a given topic
    /// </summary>
    /// <param name="topicName">Topic name</param>
    /// <param name="limit">Maximum number of related topics</param>
    /// <returns>Related topics</returns>
    Task<IEnumerable<TopicDto>> GetRelatedTopicsAsync(string topicName, int limit = 5);
    
    #endregion
    
    #region Bulk Operations
    
    /// <summary>
    /// Perform bulk operations on topics
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="operation">Bulk operation details</param>
    /// <returns>Operation results</returns>
    Task<IEnumerable<TopicFollowDto>> BulkTopicOperationAsync(int userId, BulkTopicOperationDto operation);
    
    /// <summary>
    /// Import topics from user's hashtag usage
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="minUsageCount">Minimum hashtag usage count</param>
    /// <returns>Imported topics</returns>
    Task<IEnumerable<TopicFollowDto>> ImportTopicsFromHashtagUsageAsync(int userId, int minUsageCount = 3);

    #endregion

    #region Helper Methods

    /// <summary>
    /// Calculate topic similarity based on hashtag overlap
    /// </summary>
    /// <param name="topic1Hashtags">First topic's hashtags</param>
    /// <param name="topic2Hashtags">Second topic's hashtags</param>
    /// <returns>Similarity score between 0 and 1</returns>
    Task<double> CalculateTopicSimilarityAsync(IEnumerable<string> topic1Hashtags, IEnumerable<string> topic2Hashtags);

    #endregion
}
