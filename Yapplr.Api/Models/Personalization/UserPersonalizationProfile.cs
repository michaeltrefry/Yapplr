using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models.Personalization;

/// <summary>
/// Comprehensive user personalization profile with AI-driven insights
/// </summary>
public class UserPersonalizationProfile
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    /// <summary>
    /// JSON-serialized interest scores for different topics/hashtags
    /// Format: {"technology": 0.85, "sports": 0.32, "music": 0.67}
    /// </summary>
    [StringLength(5000)]
    public string InterestScores { get; set; } = "{}";
    
    /// <summary>
    /// JSON-serialized content type preferences
    /// Format: {"text": 0.8, "image": 0.9, "video": 0.6}
    /// </summary>
    [StringLength(1000)]
    public string ContentTypePreferences { get; set; } = "{}";
    
    /// <summary>
    /// JSON-serialized engagement patterns
    /// Format: {"morning": 0.7, "afternoon": 0.5, "evening": 0.9}
    /// </summary>
    [StringLength(1000)]
    public string EngagementPatterns { get; set; } = "{}";
    
    /// <summary>
    /// JSON-serialized user similarity scores with other users
    /// Format: {"user123": 0.75, "user456": 0.62}
    /// </summary>
    [StringLength(10000)]
    public string SimilarUsers { get; set; } = "{}";
    
    /// <summary>
    /// Overall personalization confidence score (0.0 to 1.0)
    /// </summary>
    public float PersonalizationConfidence { get; set; } = 0.0f;
    
    /// <summary>
    /// Diversity preference (0.0 = very focused, 1.0 = very diverse)
    /// </summary>
    public float DiversityPreference { get; set; } = 0.5f;
    
    /// <summary>
    /// Novelty preference (0.0 = familiar content, 1.0 = new/trending content)
    /// </summary>
    public float NoveltyPreference { get; set; } = 0.5f;
    
    /// <summary>
    /// Social influence factor (0.0 = independent, 1.0 = highly influenced by network)
    /// </summary>
    public float SocialInfluenceFactor { get; set; } = 0.5f;
    
    /// <summary>
    /// Quality threshold (minimum content quality score to show)
    /// </summary>
    public float QualityThreshold { get; set; } = 0.3f;
    
    /// <summary>
    /// Last time the profile was updated by the ML engine
    /// </summary>
    public DateTime LastMLUpdate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Number of data points used to build this profile
    /// </summary>
    public int DataPointCount { get; set; } = 0;
    
    /// <summary>
    /// Version of the personalization algorithm used
    /// </summary>
    [StringLength(20)]
    public string AlgorithmVersion { get; set; } = "v1.0";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
}

/// <summary>
/// User interaction events for building personalization profiles
/// </summary>
public class UserInteractionEvent
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    /// <summary>
    /// Type of interaction (view, like, comment, share, click, dwell_time, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string InteractionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Target entity type (post, user, hashtag, topic, etc.)
    /// </summary>
    [StringLength(50)]
    public string? TargetEntityType { get; set; }
    
    /// <summary>
    /// Target entity ID
    /// </summary>
    public int? TargetEntityId { get; set; }
    
    /// <summary>
    /// Interaction strength/weight (0.0 to 1.0)
    /// </summary>
    public float InteractionStrength { get; set; } = 1.0f;
    
    /// <summary>
    /// Duration of interaction in milliseconds (for dwell time, video watch time, etc.)
    /// </summary>
    public int? DurationMs { get; set; }
    
    /// <summary>
    /// Context information (hashtags, topic, source, etc.)
    /// </summary>
    [StringLength(1000)]
    public string? Context { get; set; }
    
    /// <summary>
    /// Device/platform information
    /// </summary>
    [StringLength(100)]
    public string? DeviceInfo { get; set; }
    
    /// <summary>
    /// Session ID for grouping related interactions
    /// </summary>
    [StringLength(100)]
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Whether this interaction was implicit (scroll, dwell) or explicit (like, comment)
    /// </summary>
    public bool IsImplicit { get; set; } = false;
    
    /// <summary>
    /// Sentiment of the interaction (-1.0 to 1.0, where negative indicates dislike/skip)
    /// </summary>
    public float Sentiment { get; set; } = 0.0f;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
}

/// <summary>
/// Content embeddings for similarity calculations
/// </summary>
public class ContentEmbedding
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Type of content (post, user, hashtag, topic)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the content entity
    /// </summary>
    [Required]
    public int ContentId { get; set; }
    
    /// <summary>
    /// High-dimensional embedding vector (JSON array of floats)
    /// </summary>
    [StringLength(10000)]
    public string EmbeddingVector { get; set; } = "[]";
    
    /// <summary>
    /// Dimensionality of the embedding vector
    /// </summary>
    public int Dimensions { get; set; } = 128;
    
    /// <summary>
    /// Model version used to generate the embedding
    /// </summary>
    [StringLength(50)]
    public string ModelVersion { get; set; } = "v1.0";
    
    /// <summary>
    /// Quality score of the embedding (0.0 to 1.0)
    /// </summary>
    public float QualityScore { get; set; } = 1.0f;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Personalization experiments for A/B testing
/// </summary>
public class PersonalizationExperiment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Experiment configuration (JSON)
    /// </summary>
    [StringLength(2000)]
    public string Configuration { get; set; } = "{}";
    
    /// <summary>
    /// Percentage of users to include in experiment (0.0 to 1.0)
    /// </summary>
    public float TrafficAllocation { get; set; } = 0.1f;
    
    /// <summary>
    /// Whether the experiment is currently active
    /// </summary>
    public bool IsActive { get; set; } = false;
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<UserExperimentParticipation> Participants { get; set; } = new List<UserExperimentParticipation>();
}

/// <summary>
/// User participation in personalization experiments
/// </summary>
public class UserExperimentParticipation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public int ExperimentId { get; set; }
    
    /// <summary>
    /// Experiment variant assigned to the user
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Variant { get; set; } = string.Empty;
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public PersonalizationExperiment Experiment { get; set; } = null!;
}
