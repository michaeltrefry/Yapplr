using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using HtmlAgilityPack;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class LinkPreviewService : ILinkPreviewService
{
    private readonly YapplrDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LinkPreviewService> _logger;
    
    // URL regex pattern to match HTTP/HTTPS URLs
    private static readonly Regex UrlRegex = new(
        @"https?://(?:[-\w.])+(?:\:[0-9]+)?(?:/(?:[\w/_.-])*(?:\?(?:[\w&=%.~+/-])*)?(?:\#(?:[\w.-])*)?)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
    
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    private const int MaxContentLength = 5 * 1024 * 1024; // 5MB max
    
    public LinkPreviewService(
        YapplrDbContext context,
        HttpClient httpClient,
        ILogger<LinkPreviewService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure HttpClient
        _httpClient.Timeout = RequestTimeout;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (compatible; YapplrBot/1.0; +https://yapplr.com/bot)");
    }
    
    public async Task<IEnumerable<LinkPreviewDto>> ProcessPostLinksAsync(string content)
    {
        var urls = ExtractUrls(content);
        var linkPreviews = new List<LinkPreviewDto>();
        
        foreach (var url in urls)
        {
            try
            {
                var preview = await GetOrCreateLinkPreviewAsync(url);
                if (preview != null)
                {
                    linkPreviews.Add(preview);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process link preview for URL: {Url}", url);
            }
        }
        
        return linkPreviews;
    }
    
    public async Task<LinkPreviewDto?> GetOrCreateLinkPreviewAsync(string url)
    {
        // First check if we already have a preview for this URL
        var existing = await GetLinkPreviewByUrlAsync(url);
        if (existing != null)
        {
            return existing;
        }
        
        // Create new link preview
        var linkPreview = await FetchLinkMetadataAsync(url);
        
        _context.LinkPreviews.Add(linkPreview);
        await _context.SaveChangesAsync();
        
        return MapToDto(linkPreview);
    }
    
    public async Task<LinkPreviewDto?> GetLinkPreviewByUrlAsync(string url)
    {
        var linkPreview = await _context.LinkPreviews
            .FirstOrDefaultAsync(lp => lp.Url == url);
            
        return linkPreview != null ? MapToDto(linkPreview) : null;
    }
    
    public async Task<LinkPreview> FetchLinkMetadataAsync(string url)
    {
        var linkPreview = new LinkPreview
        {
            Url = url,
            Status = LinkPreviewStatus.Pending
        };
        
        try
        {
            // Validate URL
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || 
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                linkPreview.Status = LinkPreviewStatus.InvalidUrl;
                linkPreview.ErrorMessage = "Invalid URL format";
                return linkPreview;
            }
            
            using var response = await _httpClient.GetAsync(uri);
            
            // Handle different HTTP status codes
            switch ((int)response.StatusCode)
            {
                case 404:
                    linkPreview.Status = LinkPreviewStatus.NotFound;
                    linkPreview.ErrorMessage = "Page not found (404)";
                    return linkPreview;
                    
                case 401:
                    linkPreview.Status = LinkPreviewStatus.Unauthorized;
                    linkPreview.ErrorMessage = "Authentication required (401)";
                    return linkPreview;
                    
                case 403:
                    linkPreview.Status = LinkPreviewStatus.Forbidden;
                    linkPreview.ErrorMessage = "Access forbidden (403)";
                    return linkPreview;
                    
                case >= 400:
                    linkPreview.Status = LinkPreviewStatus.Error;
                    linkPreview.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                    return linkPreview;
            }
            
            // Check content type
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null && !contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
            {
                linkPreview.Status = LinkPreviewStatus.UnsupportedContent;
                linkPreview.ErrorMessage = $"Unsupported content type: {contentType}";
                return linkPreview;
            }
            
            // Check content length
            if (response.Content.Headers.ContentLength > MaxContentLength)
            {
                linkPreview.Status = LinkPreviewStatus.TooLarge;
                linkPreview.ErrorMessage = "Content too large";
                return linkPreview;
            }
            
            var html = await response.Content.ReadAsStringAsync();
            ParseHtmlMetadata(html, linkPreview);
            
            linkPreview.Status = LinkPreviewStatus.Success;
        }
        catch (TaskCanceledException)
        {
            linkPreview.Status = LinkPreviewStatus.Timeout;
            linkPreview.ErrorMessage = "Request timed out";
        }
        catch (HttpRequestException ex)
        {
            linkPreview.Status = LinkPreviewStatus.NetworkError;
            linkPreview.ErrorMessage = $"Network error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching link preview for {Url}", url);
            linkPreview.Status = LinkPreviewStatus.Error;
            linkPreview.ErrorMessage = "Unexpected error occurred";
        }
        
        linkPreview.UpdatedAt = DateTime.UtcNow;
        return linkPreview;
    }
    
    private static List<string> ExtractUrls(string content)
    {
        var matches = UrlRegex.Matches(content);
        return matches.Cast<Match>().Select(m => m.Value).Distinct().ToList();
    }
    
    private static void ParseHtmlMetadata(string html, LinkPreview linkPreview)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        // Try Open Graph tags first
        linkPreview.Title = GetMetaContent(doc, "og:title") ?? GetTitleTag(doc);
        linkPreview.Description = GetMetaContent(doc, "og:description") ?? GetMetaContent(doc, "description");
        linkPreview.ImageUrl = GetMetaContent(doc, "og:image");
        linkPreview.SiteName = GetMetaContent(doc, "og:site_name");
        
        // Fallback to Twitter Card tags if Open Graph not available
        linkPreview.Title ??= GetMetaContent(doc, "twitter:title");
        linkPreview.Description ??= GetMetaContent(doc, "twitter:description");
        linkPreview.ImageUrl ??= GetMetaContent(doc, "twitter:image");
        
        // Trim and limit lengths
        if (!string.IsNullOrEmpty(linkPreview.Title))
        {
            linkPreview.Title = linkPreview.Title.Trim();
            if (linkPreview.Title.Length > 500)
                linkPreview.Title = linkPreview.Title.Substring(0, 500);
        }

        if (!string.IsNullOrEmpty(linkPreview.Description))
        {
            linkPreview.Description = linkPreview.Description.Trim();
            if (linkPreview.Description.Length > 1000)
                linkPreview.Description = linkPreview.Description.Substring(0, 1000);
        }

        if (!string.IsNullOrEmpty(linkPreview.SiteName))
        {
            linkPreview.SiteName = linkPreview.SiteName.Trim();
            if (linkPreview.SiteName.Length > 100)
                linkPreview.SiteName = linkPreview.SiteName.Substring(0, 100);
        }
    }
    
    private static string? GetMetaContent(HtmlDocument doc, string property)
    {
        var node = doc.DocumentNode
            .SelectSingleNode($"//meta[@property='{property}']") ??
            doc.DocumentNode
            .SelectSingleNode($"//meta[@name='{property}']");
            
        var content = node?.GetAttributeValue("content", string.Empty);
        return string.IsNullOrEmpty(content) ? null : content;
    }
    
    private static string? GetTitleTag(HtmlDocument doc)
    {
        return doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
    }
    
    private static LinkPreviewDto MapToDto(LinkPreview linkPreview)
    {
        return new LinkPreviewDto(
            linkPreview.Id,
            linkPreview.Url,
            linkPreview.Title,
            linkPreview.Description,
            linkPreview.ImageUrl,
            linkPreview.SiteName,
            linkPreview.Status,
            linkPreview.ErrorMessage,
            linkPreview.CreatedAt
        );
    }
}
