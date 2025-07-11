using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IContentManagementService
{
    // Content Pages
    Task<IEnumerable<ContentPageDto>> GetContentPagesAsync();
    Task<ContentPageDto?> GetContentPageAsync(int id);
    Task<ContentPageDto?> GetContentPageBySlugAsync(string slug);
    Task<ContentPageDto?> GetContentPageByTypeAsync(ContentPageType type);
    
    // Content Page Versions
    Task<IEnumerable<ContentPageVersionDto>> GetContentPageVersionsAsync(int contentPageId);
    Task<ContentPageVersionDto?> GetContentPageVersionAsync(int versionId);
    Task<ContentPageVersionDto> CreateContentPageVersionAsync(int contentPageId, CreateContentPageVersionDto createDto, int createdByUserId);
    
    // Publishing
    Task<bool> PublishContentPageVersionAsync(int contentPageId, int versionId, int publishedByUserId);
    Task<ContentPageVersionDto?> GetPublishedVersionAsync(int contentPageId);
    Task<ContentPageVersionDto?> GetPublishedVersionBySlugAsync(string slug);
    Task<ContentPageVersionDto?> GetPublishedVersionByTypeAsync(ContentPageType type);
    
    // Content Page Management
    Task<ContentPageDto> CreateContentPageAsync(string title, string slug, ContentPageType type);
    Task<ContentPageDto?> UpdateContentPageAsync(int id, string title, string slug);
    Task<bool> DeleteContentPageAsync(int id);
}
