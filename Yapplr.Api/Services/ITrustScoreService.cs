namespace Yapplr.Api.Services;

/// <summary>
/// Service for calculating and managing user trust scores
/// </summary>
public interface ITrustScoreService
{
    /// <summary>
    /// Calculate a comprehensive trust score for a user based on their behavior history
    /// </summary>
    /// <param name="userId">The user ID to calculate trust score for</param>
    /// <param name="recalculateFromScratch">If true, recalculates from all historical data. If false, uses incremental updates.</param>
    /// <returns>The calculated trust score (0.0 to 1.0)</returns>
    Task<float> CalculateUserTrustScoreAsync(int userId, bool recalculateFromScratch = false);

    /// <summary>
    /// Update trust score based on a specific user action
    /// </summary>
    /// <param name="userId">The user who performed the action</param>
    /// <param name="action">The type of action performed</param>
    /// <param name="relatedEntityType">Type of entity the action was performed on (e.g., "post", "comment")</param>
    /// <param name="relatedEntityId">ID of the entity the action was performed on</param>
    /// <param name="metadata">Additional context about the action</param>
    /// <returns>The new trust score after the update</returns>
    Task<float> UpdateTrustScoreForActionAsync(int userId, TrustScoreAction action, 
        string? relatedEntityType = null, int? relatedEntityId = null, string? metadata = null);

    /// <summary>
    /// Get trust score factors breakdown for a user (for debugging/admin purposes)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Dictionary of factors and their contributions to the trust score</returns>
    Task<Dictionary<string, object>> GetTrustScoreFactorsAsync(int userId);

    /// <summary>
    /// Apply automatic trust score decay for inactive users
    /// </summary>
    /// <param name="inactiveDays">Number of days of inactivity before decay starts</param>
    /// <param name="decayRate">Daily decay rate (e.g., 0.01 for 1% per day)</param>
    /// <returns>Number of users whose scores were decayed</returns>
    Task<int> ApplyInactivityDecayAsync(int inactiveDays = 30, float decayRate = 0.005f);

    /// <summary>
    /// Recalculate trust scores for all users (background job)
    /// </summary>
    /// <param name="batchSize">Number of users to process in each batch</param>
    /// <returns>Number of users processed</returns>
    Task<int> RecalculateAllTrustScoresAsync(int batchSize = 100);

    /// <summary>
    /// Get users with trust scores below a threshold
    /// </summary>
    /// <param name="threshold">Trust score threshold (0.0 to 1.0)</param>
    /// <param name="limit">Maximum number of users to return</param>
    /// <returns>List of user IDs with low trust scores</returns>
    Task<List<int>> GetUsersWithLowTrustScoresAsync(float threshold = 0.3f, int limit = 100);

    /// <summary>
    /// Get trust score statistics for the platform
    /// </summary>
    /// <returns>Dictionary containing trust score statistics</returns>
    Task<Dictionary<string, object>> GetTrustScoreStatisticsAsync();
}