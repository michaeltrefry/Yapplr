using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for comprehensive content discovery and exploration features
/// </summary>
public interface IExploreService
{
    /// <summary>
    /// Get comprehensive explore page with all discovery content
    /// </summary>
    /// <param name="userId">User ID for personalization (optional)</param>
    /// <param name="config">Explore configuration options</param>
    /// <returns>Complete explore page data</returns>
    Task<ExplorePageDto> GetExplorePageAsync(int? userId = null, ExploreConfigDto? config = null);

    /// <summary>
    /// Get user recommendations based on similarity and network analysis
    /// </summary>
    /// <param name="userId">User ID for personalized recommendations</param>
    /// <param name="limit">Maximum number of recommendations</param>
    /// <param name="minSimilarityScore">Minimum similarity threshold</param>
    /// <returns>List of recommended users with scoring</returns>
    Task<IEnumerable<UserRecommendationDto>> GetUserRecommendationsAsync(int userId, int limit = 10, double minSimilarityScore = 0.1);

    /// <summary>
    /// Discover similar users based on interaction patterns
    /// </summary>
    /// <param name="userId">User ID to find similar users for</param>
    /// <param name="limit">Maximum number of similar users</param>
    /// <returns>List of similar users with similarity metrics</returns>
    Task<IEnumerable<SimilarUserDto>> GetSimilarUsersAsync(int userId, int limit = 10);

    /// <summary>
    /// Get content clusters for topic-based discovery
    /// </summary>
    /// <param name="userId">User ID for personalization (optional)</param>
    /// <param name="limit">Maximum number of clusters</param>
    /// <returns>List of content clusters</returns>
    Task<IEnumerable<ContentClusterDto>> GetContentClustersAsync(int? userId = null, int limit = 5);

    /// <summary>
    /// Get interest-based content recommendations
    /// </summary>
    /// <param name="userId">User ID for interest analysis</param>
    /// <param name="limit">Maximum number of recommendations per interest</param>
    /// <returns>Content recommendations grouped by interest</returns>
    Task<IEnumerable<InterestBasedContentDto>> GetInterestBasedContentAsync(int userId, int limit = 5);

    /// <summary>
    /// Get trending topics with cross-content analysis
    /// </summary>
    /// <param name="timeWindow">Time window for trending analysis (hours)</param>
    /// <param name="limit">Maximum number of trending topics</param>
    /// <param name="userId">User ID for personalization (optional)</param>
    /// <returns>List of trending topics</returns>
    Task<IEnumerable<TrendingTopicDto>> GetTrendingTopicsAsync(int timeWindow = 24, int limit = 10, int? userId = null);

    /// <summary>
    /// Get network-based user discoveries
    /// </summary>
    /// <param name="userId">User ID for network analysis</param>
    /// <param name="maxDegrees">Maximum degrees of separation</param>
    /// <param name="limit">Maximum number of discoveries</param>
    /// <returns>Users discovered through network analysis</returns>
    Task<IEnumerable<NetworkBasedUserDto>> GetNetworkBasedUsersAsync(int userId, int maxDegrees = 3, int limit = 10);

    /// <summary>
    /// Get explained content recommendations with reasoning
    /// </summary>
    /// <param name="userId">User ID for personalized explanations</param>
    /// <param name="limit">Maximum number of recommendations</param>
    /// <returns>Content recommendations with explanations</returns>
    Task<IEnumerable<ExplainedContentDto>> GetExplainedContentRecommendationsAsync(int userId, int limit = 20);

    /// <summary>
    /// Get modular explore sections for flexible UI composition
    /// </summary>
    /// <param name="userId">User ID for personalization (optional)</param>
    /// <param name="sectionTypes">Specific section types to include (optional)</param>
    /// <returns>List of explore sections</returns>
    Task<IEnumerable<ExploreSectionDto>> GetExploreSectionsAsync(int? userId = null, IEnumerable<string>? sectionTypes = null);

    /// <summary>
    /// Calculate user similarity score based on interaction patterns and interests
    /// </summary>
    /// <param name="userId1">First user ID</param>
    /// <param name="userId2">Second user ID</param>
    /// <returns>Similarity score between 0 and 1</returns>
    Task<double> CalculateUserSimilarityAsync(int userId1, int userId2);
}
