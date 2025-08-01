using Yapplr.Api.DTOs;
using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for calculating and retrieving trending content across the platform
/// </summary>
public interface ITrendingService
{
    /// <summary>
    /// Get trending posts based on engagement velocity and quality metrics
    /// </summary>
    /// <param name="timeWindow">Time window for trending calculation (hours)</param>
    /// <param name="limit">Maximum number of trending posts to return</param>
    /// <param name="currentUserId">Current user ID for personalization and filtering</param>
    /// <returns>List of trending posts ordered by trending score</returns>
    Task<IEnumerable<PostDto>> GetTrendingPostsAsync(int timeWindow = 24, int limit = 20, int? currentUserId = null);

    /// <summary>
    /// Get trending posts with detailed scoring information for analytics
    /// </summary>
    /// <param name="timeWindow">Time window for trending calculation (hours)</param>
    /// <param name="limit">Maximum number of trending posts to return</param>
    /// <param name="currentUserId">Current user ID for personalization and filtering</param>
    /// <returns>List of trending posts with score breakdown</returns>
    Task<IEnumerable<TrendingPostDto>> GetTrendingPostsWithScoresAsync(int timeWindow = 24, int limit = 20, int? currentUserId = null);

    /// <summary>
    /// Calculate trending score for a specific post
    /// </summary>
    /// <param name="postId">Post ID to calculate score for</param>
    /// <param name="timeWindow">Time window for calculation (hours)</param>
    /// <returns>Trending score and breakdown</returns>
    Task<TrendingScoreDto> CalculatePostTrendingScoreAsync(int postId, int timeWindow = 24);

    /// <summary>
    /// Get trending posts by category/topic
    /// </summary>
    /// <param name="hashtag">Hashtag to filter by (optional)</param>
    /// <param name="timeWindow">Time window for trending calculation (hours)</param>
    /// <param name="limit">Maximum number of trending posts to return</param>
    /// <param name="currentUserId">Current user ID for personalization and filtering</param>
    /// <returns>List of trending posts in the specified category</returns>
    Task<IEnumerable<PostDto>> GetTrendingPostsByHashtagAsync(string? hashtag = null, int timeWindow = 24, int limit = 20, int? currentUserId = null);

    /// <summary>
    /// Get personalized trending posts based on user's interests and following
    /// </summary>
    /// <param name="userId">User ID for personalization</param>
    /// <param name="timeWindow">Time window for trending calculation (hours)</param>
    /// <param name="limit">Maximum number of trending posts to return</param>
    /// <returns>List of personalized trending posts</returns>
    Task<IEnumerable<PostDto>> GetPersonalizedTrendingPostsAsync(int userId, int timeWindow = 24, int limit = 20);

    /// <summary>
    /// Get trending posts analytics for admin dashboard
    /// </summary>
    /// <param name="timeWindow">Time window for analysis (hours)</param>
    /// <returns>Analytics data about trending posts</returns>
    Task<TrendingAnalyticsDto> GetTrendingAnalyticsAsync(int timeWindow = 24);

    /// <summary>
    /// Get enhanced trending categories using velocity-based hashtag analytics
    /// </summary>
    /// <param name="timeWindow">Time window for analysis (hours)</param>
    /// <param name="limit">Maximum number of categories to return</param>
    /// <returns>Enhanced trending categories with velocity metrics</returns>
    Task<IEnumerable<CategoryTrendingDto>> GetEnhancedTrendingCategoriesAsync(int timeWindow = 24, int limit = 10);
}
