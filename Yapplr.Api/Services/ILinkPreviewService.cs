using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface ILinkPreviewService
{
    /// <summary>
    /// Extracts URLs from post content and creates link previews
    /// </summary>
    Task<IEnumerable<LinkPreviewDto>> ProcessPostLinksAsync(string content);
    
    /// <summary>
    /// Gets or creates a link preview for a specific URL
    /// </summary>
    Task<LinkPreviewDto?> GetOrCreateLinkPreviewAsync(string url);
    
    /// <summary>
    /// Gets an existing link preview by URL
    /// </summary>
    Task<LinkPreviewDto?> GetLinkPreviewByUrlAsync(string url);
    
    /// <summary>
    /// Fetches and parses metadata from a URL
    /// </summary>
    Task<LinkPreview> FetchLinkMetadataAsync(string url);
}
