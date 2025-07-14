using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class ContentPageDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ContentPageType Type { get; set; }
    public int? PublishedVersionId { get; set; }
    public ContentPageVersionDto? PublishedVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalVersions { get; set; }
}
