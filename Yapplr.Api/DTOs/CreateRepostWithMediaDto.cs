using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for creating a repost with optional content and media
/// </summary>
public class CreateRepostWithMediaDto
{
    /// <summary>
    /// User's commentary on the reposted post (optional - empty for simple reposts)
    /// </summary>
    [StringLength(1024, ErrorMessage = "Content cannot exceed 1024 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// ID of the post being reposted
    /// </summary>
    [Required(ErrorMessage = "RepostedPostId is required")]
    public int RepostedPostId { get; set; }

    /// <summary>
    /// Privacy setting for the repost
    /// </summary>
    public PostPrivacy Privacy { get; set; } = PostPrivacy.Public;

    /// <summary>
    /// Optional group ID if posting to a group
    /// </summary>
    public int? GroupId { get; set; }

    /// <summary>
    /// Media files to attach to the repost (up to 10 files)
    /// </summary>
    [MaxLength(10, ErrorMessage = "Maximum 10 media files allowed")]
    public List<MediaFileDto>? MediaFiles { get; set; } = null;
}
