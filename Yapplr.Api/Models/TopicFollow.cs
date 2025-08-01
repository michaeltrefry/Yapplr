using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

/// <summary>
/// Represents a user following a topic (collection of related hashtags)
/// </summary>
public class TopicFollow
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string TopicName { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? TopicDescription { get; set; }
    
    /// <summary>
    /// Category of the topic (Technology, Sports, Arts & Entertainment, etc.)
    /// </summary>
    [StringLength(50)]
    public string Category { get; set; } = "General";
    
    /// <summary>
    /// Comma-separated list of hashtags that define this topic
    /// </summary>
    [StringLength(1000)]
    public string RelatedHashtags { get; set; } = string.Empty;
    
    /// <summary>
    /// User's interest level in this topic (0.0 to 1.0)
    /// </summary>
    public float InterestLevel { get; set; } = 1.0f;
    
    /// <summary>
    /// Whether to include this topic in the main feed
    /// </summary>
    public bool IncludeInMainFeed { get; set; } = true;
    
    /// <summary>
    /// Whether to receive notifications for trending content in this topic
    /// </summary>
    public bool EnableNotifications { get; set; } = false;
    
    /// <summary>
    /// Minimum trending score required for notifications
    /// </summary>
    public float NotificationThreshold { get; set; } = 0.7f;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
}

/// <summary>
/// Predefined topics that users can follow
/// </summary>
public class Topic
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string Category { get; set; } = "General";
    
    /// <summary>
    /// Comma-separated list of hashtags that define this topic
    /// </summary>
    [StringLength(1000)]
    public string RelatedHashtags { get; set; } = string.Empty;
    
    /// <summary>
    /// URL-friendly slug for the topic
    /// </summary>
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;
    
    /// <summary>
    /// Icon or emoji representing the topic
    /// </summary>
    [StringLength(10)]
    public string? Icon { get; set; }
    
    /// <summary>
    /// Color theme for the topic (hex color)
    /// </summary>
    [StringLength(7)]
    public string? Color { get; set; }
    
    /// <summary>
    /// Whether this topic is featured/promoted
    /// </summary>
    public bool IsFeatured { get; set; } = false;
    
    /// <summary>
    /// Number of users following this topic
    /// </summary>
    public int FollowerCount { get; set; } = 0;
    
    /// <summary>
    /// Whether this topic is active and visible to users
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<TopicFollow> Followers { get; set; } = new List<TopicFollow>();
}

/// <summary>
/// Analytics for topic engagement and performance
/// </summary>
public class TopicAnalytics
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int TopicId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string TopicName { get; set; } = string.Empty;
    
    /// <summary>
    /// Date for which these analytics are calculated
    /// </summary>
    public DateTime AnalyticsDate { get; set; }
    
    /// <summary>
    /// Number of posts in this topic on this date
    /// </summary>
    public int PostCount { get; set; } = 0;
    
    /// <summary>
    /// Total engagement (likes, comments, reposts) for this topic
    /// </summary>
    public int TotalEngagement { get; set; } = 0;
    
    /// <summary>
    /// Number of unique users who posted in this topic
    /// </summary>
    public int UniquePosters { get; set; } = 0;
    
    /// <summary>
    /// Average engagement rate for posts in this topic
    /// </summary>
    public float AvgEngagementRate { get; set; } = 0.0f;
    
    /// <summary>
    /// Trending score for this topic on this date
    /// </summary>
    public float TrendingScore { get; set; } = 0.0f;
    
    /// <summary>
    /// Growth rate compared to previous period
    /// </summary>
    public float GrowthRate { get; set; } = 0.0f;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Topic? Topic { get; set; }
}
