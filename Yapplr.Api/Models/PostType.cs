namespace Yapplr.Api.Models;

/// <summary>
/// Represents the type of post in the unified post model
/// </summary>
public enum PostType
{
    /// <summary>
    /// Regular post (top-level content)
    /// </summary>
    Post = 0,

    /// <summary>
    /// Comment (reply to another post)
    /// </summary>
    Comment = 1,

    /// <summary>
    /// Repost (sharing another post with optional commentary and media)
    /// </summary>
    Repost = 2
}
