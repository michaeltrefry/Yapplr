namespace Yapplr.Api.Models;

/// <summary>
/// Represents the different types of reactions users can give to posts and comments
/// </summary>
public enum ReactionType
{
    /// <summary>
    /// Heart reaction ❤️
    /// </summary>
    Heart = 1,
    
    /// <summary>
    /// Thumbs up reaction 👍
    /// </summary>
    ThumbsUp = 2,
    
    /// <summary>
    /// Laughing reaction 😂
    /// </summary>
    Laugh = 3,
    
    /// <summary>
    /// Surprised reaction 😮
    /// </summary>
    Surprised = 4,
    
    /// <summary>
    /// Sad reaction 😢
    /// </summary>
    Sad = 5,
    
    /// <summary>
    /// Angry reaction 😡
    /// </summary>
    Angry = 6
}

/// <summary>
/// Extension methods for ReactionType enum
/// </summary>
public static class ReactionTypeExtensions
{
    /// <summary>
    /// Gets the emoji representation of the reaction type
    /// </summary>
    public static string GetEmoji(this ReactionType reactionType)
    {
        return reactionType switch
        {
            ReactionType.Heart => "❤️",
            ReactionType.ThumbsUp => "👍",
            ReactionType.Laugh => "😂",
            ReactionType.Surprised => "😮",
            ReactionType.Sad => "😢",
            ReactionType.Angry => "😡",
            _ => "❤️" // Default to heart
        };
    }
    
    /// <summary>
    /// Gets the display name of the reaction type
    /// </summary>
    public static string GetDisplayName(this ReactionType reactionType)
    {
        return reactionType switch
        {
            ReactionType.Heart => "Love",
            ReactionType.ThumbsUp => "React",
            ReactionType.Laugh => "Laugh",
            ReactionType.Surprised => "Surprised",
            ReactionType.Sad => "Sad",
            ReactionType.Angry => "Angry",
            _ => "Love" // Default to love
        };
    }
}
