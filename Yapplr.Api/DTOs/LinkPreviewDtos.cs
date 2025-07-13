using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record LinkPreviewDto(
    int Id,
    string Url,
    string? Title,
    string? Description,
    string? ImageUrl,
    string? SiteName,
    string? YouTubeVideoId,
    LinkPreviewStatus Status,
    string? ErrorMessage,
    DateTime CreatedAt
);

public record CreateLinkPreviewDto(
    string Url
);
