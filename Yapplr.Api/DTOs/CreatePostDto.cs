using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record CreatePostDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content,
    string? ImageFileName = null,
    string? VideoFileName = null,
    PostPrivacy Privacy = PostPrivacy.Public,
    [MaxLength(10, ErrorMessage = "Maximum 10 media files allowed")]
    List<string>? MediaFileNames = null
);