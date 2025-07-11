using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class ContentManagementService : IContentManagementService
{
    private readonly YapplrDbContext _context;

    public ContentManagementService(YapplrDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ContentPageDto>> GetContentPagesAsync()
    {
        return await _context.ContentPages
            .Include(cp => cp.PublishedVersion)
                .ThenInclude(v => v!.CreatedByUser)
            .Select(cp => new ContentPageDto
            {
                Id = cp.Id,
                Title = cp.Title,
                Slug = cp.Slug,
                Type = cp.Type,
                PublishedVersionId = cp.PublishedVersionId,
                PublishedVersion = cp.PublishedVersion != null ? new ContentPageVersionDto
                {
                    Id = cp.PublishedVersion.Id,
                    ContentPageId = cp.PublishedVersion.ContentPageId,
                    Content = cp.PublishedVersion.Content,
                    ChangeNotes = cp.PublishedVersion.ChangeNotes,
                    VersionNumber = cp.PublishedVersion.VersionNumber,
                    IsPublished = cp.PublishedVersion.IsPublished,
                    PublishedAt = cp.PublishedVersion.PublishedAt,
                    PublishedByUsername = cp.PublishedVersion.PublishedByUser != null ? cp.PublishedVersion.PublishedByUser.Username : null,
                    CreatedByUsername = cp.PublishedVersion.CreatedByUser.Username,
                    CreatedAt = cp.PublishedVersion.CreatedAt
                } : null,
                CreatedAt = cp.CreatedAt,
                UpdatedAt = cp.UpdatedAt,
                TotalVersions = cp.Versions.Count
            })
            .OrderBy(cp => cp.Type)
            .ToListAsync();
    }

    public async Task<ContentPageDto?> GetContentPageAsync(int id)
    {
        var contentPage = await _context.ContentPages
            .Include(cp => cp.PublishedVersion)
                .ThenInclude(v => v!.CreatedByUser)
            .Include(cp => cp.PublishedVersion)
                .ThenInclude(v => v!.PublishedByUser)
            .Include(cp => cp.Versions)
            .FirstOrDefaultAsync(cp => cp.Id == id);

        if (contentPage == null) return null;

        return new ContentPageDto
        {
            Id = contentPage.Id,
            Title = contentPage.Title,
            Slug = contentPage.Slug,
            Type = contentPage.Type,
            PublishedVersionId = contentPage.PublishedVersionId,
            PublishedVersion = contentPage.PublishedVersion != null ? new ContentPageVersionDto
            {
                Id = contentPage.PublishedVersion.Id,
                ContentPageId = contentPage.PublishedVersion.ContentPageId,
                Content = contentPage.PublishedVersion.Content,
                ChangeNotes = contentPage.PublishedVersion.ChangeNotes,
                VersionNumber = contentPage.PublishedVersion.VersionNumber,
                IsPublished = contentPage.PublishedVersion.IsPublished,
                PublishedAt = contentPage.PublishedVersion.PublishedAt,
                PublishedByUsername = contentPage.PublishedVersion.PublishedByUser?.Username,
                CreatedByUsername = contentPage.PublishedVersion.CreatedByUser.Username,
                CreatedAt = contentPage.PublishedVersion.CreatedAt
            } : null,
            CreatedAt = contentPage.CreatedAt,
            UpdatedAt = contentPage.UpdatedAt,
            TotalVersions = contentPage.Versions.Count
        };
    }

    public async Task<ContentPageDto?> GetContentPageBySlugAsync(string slug)
    {
        var contentPage = await _context.ContentPages
            .Include(cp => cp.PublishedVersion)
                .ThenInclude(v => v!.CreatedByUser)
            .Include(cp => cp.PublishedVersion)
                .ThenInclude(v => v!.PublishedByUser)
            .Include(cp => cp.Versions)
            .FirstOrDefaultAsync(cp => cp.Slug == slug);

        if (contentPage == null) return null;

        return new ContentPageDto
        {
            Id = contentPage.Id,
            Title = contentPage.Title,
            Slug = contentPage.Slug,
            Type = contentPage.Type,
            PublishedVersionId = contentPage.PublishedVersionId,
            PublishedVersion = contentPage.PublishedVersion != null ? new ContentPageVersionDto
            {
                Id = contentPage.PublishedVersion.Id,
                ContentPageId = contentPage.PublishedVersion.ContentPageId,
                Content = contentPage.PublishedVersion.Content,
                ChangeNotes = contentPage.PublishedVersion.ChangeNotes,
                VersionNumber = contentPage.PublishedVersion.VersionNumber,
                IsPublished = contentPage.PublishedVersion.IsPublished,
                PublishedAt = contentPage.PublishedVersion.PublishedAt,
                PublishedByUsername = contentPage.PublishedVersion.PublishedByUser?.Username,
                CreatedByUsername = contentPage.PublishedVersion.CreatedByUser.Username,
                CreatedAt = contentPage.PublishedVersion.CreatedAt
            } : null,
            CreatedAt = contentPage.CreatedAt,
            UpdatedAt = contentPage.UpdatedAt,
            TotalVersions = contentPage.Versions.Count
        };
    }

    public async Task<ContentPageDto?> GetContentPageByTypeAsync(ContentPageType type)
    {
        var contentPage = await _context.ContentPages
            .Include(cp => cp.PublishedVersion)
                .ThenInclude(v => v!.CreatedByUser)
            .Include(cp => cp.PublishedVersion)
                .ThenInclude(v => v!.PublishedByUser)
            .Include(cp => cp.Versions)
            .FirstOrDefaultAsync(cp => cp.Type == type);

        if (contentPage == null) return null;

        return new ContentPageDto
        {
            Id = contentPage.Id,
            Title = contentPage.Title,
            Slug = contentPage.Slug,
            Type = contentPage.Type,
            PublishedVersionId = contentPage.PublishedVersionId,
            PublishedVersion = contentPage.PublishedVersion != null ? new ContentPageVersionDto
            {
                Id = contentPage.PublishedVersion.Id,
                ContentPageId = contentPage.PublishedVersion.ContentPageId,
                Content = contentPage.PublishedVersion.Content,
                ChangeNotes = contentPage.PublishedVersion.ChangeNotes,
                VersionNumber = contentPage.PublishedVersion.VersionNumber,
                IsPublished = contentPage.PublishedVersion.IsPublished,
                PublishedAt = contentPage.PublishedVersion.PublishedAt,
                PublishedByUsername = contentPage.PublishedVersion.PublishedByUser?.Username,
                CreatedByUsername = contentPage.PublishedVersion.CreatedByUser.Username,
                CreatedAt = contentPage.PublishedVersion.CreatedAt
            } : null,
            CreatedAt = contentPage.CreatedAt,
            UpdatedAt = contentPage.UpdatedAt,
            TotalVersions = contentPage.Versions.Count
        };
    }

    public async Task<IEnumerable<ContentPageVersionDto>> GetContentPageVersionsAsync(int contentPageId)
    {
        return await _context.ContentPageVersions
            .Include(v => v.CreatedByUser)
            .Include(v => v.PublishedByUser)
            .Where(v => v.ContentPageId == contentPageId)
            .Select(v => new ContentPageVersionDto
            {
                Id = v.Id,
                ContentPageId = v.ContentPageId,
                Content = v.Content,
                ChangeNotes = v.ChangeNotes,
                VersionNumber = v.VersionNumber,
                IsPublished = v.IsPublished,
                PublishedAt = v.PublishedAt,
                PublishedByUsername = v.PublishedByUser != null ? v.PublishedByUser.Username : null,
                CreatedByUsername = v.CreatedByUser.Username,
                CreatedAt = v.CreatedAt
            })
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }

    public async Task<ContentPageVersionDto?> GetContentPageVersionAsync(int versionId)
    {
        var version = await _context.ContentPageVersions
            .Include(v => v.CreatedByUser)
            .Include(v => v.PublishedByUser)
            .FirstOrDefaultAsync(v => v.Id == versionId);

        if (version == null) return null;

        return new ContentPageVersionDto
        {
            Id = version.Id,
            ContentPageId = version.ContentPageId,
            Content = version.Content,
            ChangeNotes = version.ChangeNotes,
            VersionNumber = version.VersionNumber,
            IsPublished = version.IsPublished,
            PublishedAt = version.PublishedAt,
            PublishedByUsername = version.PublishedByUser?.Username,
            CreatedByUsername = version.CreatedByUser.Username,
            CreatedAt = version.CreatedAt
        };
    }

    public async Task<ContentPageVersionDto> CreateContentPageVersionAsync(int contentPageId, CreateContentPageVersionDto createDto, int createdByUserId)
    {
        var contentPage = await _context.ContentPages
            .Include(cp => cp.Versions)
            .FirstOrDefaultAsync(cp => cp.Id == contentPageId);

        if (contentPage == null)
            throw new ArgumentException("Content page not found", nameof(contentPageId));

        var nextVersionNumber = contentPage.Versions.Any()
            ? contentPage.Versions.Max(v => v.VersionNumber) + 1
            : 1;

        var version = new ContentPageVersion
        {
            ContentPageId = contentPageId,
            Content = createDto.Content,
            ChangeNotes = createDto.ChangeNotes,
            VersionNumber = nextVersionNumber,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ContentPageVersions.Add(version);

        // Update the content page's UpdatedAt timestamp
        contentPage.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload with user information
        await _context.Entry(version)
            .Reference(v => v.CreatedByUser)
            .LoadAsync();

        return new ContentPageVersionDto
        {
            Id = version.Id,
            ContentPageId = version.ContentPageId,
            Content = version.Content,
            ChangeNotes = version.ChangeNotes,
            VersionNumber = version.VersionNumber,
            IsPublished = version.IsPublished,
            PublishedAt = version.PublishedAt,
            PublishedByUsername = null,
            CreatedByUsername = version.CreatedByUser.Username,
            CreatedAt = version.CreatedAt
        };
    }

    public async Task<bool> PublishContentPageVersionAsync(int contentPageId, int versionId, int publishedByUserId)
    {
        var contentPage = await _context.ContentPages
            .Include(cp => cp.Versions)
            .FirstOrDefaultAsync(cp => cp.Id == contentPageId);

        if (contentPage == null) return false;

        var version = contentPage.Versions.FirstOrDefault(v => v.Id == versionId);
        if (version == null) return false;

        // Unpublish any currently published version
        var currentPublishedVersion = contentPage.Versions.FirstOrDefault(v => v.IsPublished);
        if (currentPublishedVersion != null)
        {
            currentPublishedVersion.IsPublished = false;
        }

        // Publish the new version
        version.IsPublished = true;
        version.PublishedAt = DateTime.UtcNow;
        version.PublishedByUserId = publishedByUserId;

        // Update the content page's published version reference
        contentPage.PublishedVersionId = versionId;
        contentPage.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ContentPageVersionDto?> GetPublishedVersionAsync(int contentPageId)
    {
        var publishedVersion = await _context.ContentPageVersions
            .Include(v => v.CreatedByUser)
            .Include(v => v.PublishedByUser)
            .FirstOrDefaultAsync(v => v.ContentPageId == contentPageId && v.IsPublished);

        if (publishedVersion == null) return null;

        return new ContentPageVersionDto
        {
            Id = publishedVersion.Id,
            ContentPageId = publishedVersion.ContentPageId,
            Content = publishedVersion.Content,
            ChangeNotes = publishedVersion.ChangeNotes,
            VersionNumber = publishedVersion.VersionNumber,
            IsPublished = publishedVersion.IsPublished,
            PublishedAt = publishedVersion.PublishedAt,
            PublishedByUsername = publishedVersion.PublishedByUser?.Username,
            CreatedByUsername = publishedVersion.CreatedByUser.Username,
            CreatedAt = publishedVersion.CreatedAt
        };
    }

    public async Task<ContentPageVersionDto?> GetPublishedVersionBySlugAsync(string slug)
    {
        var publishedVersion = await _context.ContentPageVersions
            .Include(v => v.ContentPage)
            .Include(v => v.CreatedByUser)
            .Include(v => v.PublishedByUser)
            .FirstOrDefaultAsync(v => v.ContentPage.Slug == slug && v.IsPublished);

        if (publishedVersion == null) return null;

        return new ContentPageVersionDto
        {
            Id = publishedVersion.Id,
            ContentPageId = publishedVersion.ContentPageId,
            Content = publishedVersion.Content,
            ChangeNotes = publishedVersion.ChangeNotes,
            VersionNumber = publishedVersion.VersionNumber,
            IsPublished = publishedVersion.IsPublished,
            PublishedAt = publishedVersion.PublishedAt,
            PublishedByUsername = publishedVersion.PublishedByUser?.Username,
            CreatedByUsername = publishedVersion.CreatedByUser.Username,
            CreatedAt = publishedVersion.CreatedAt
        };
    }

    public async Task<ContentPageVersionDto?> GetPublishedVersionByTypeAsync(ContentPageType type)
    {
        var publishedVersion = await _context.ContentPageVersions
            .Include(v => v.ContentPage)
            .Include(v => v.CreatedByUser)
            .Include(v => v.PublishedByUser)
            .FirstOrDefaultAsync(v => v.ContentPage.Type == type && v.IsPublished);

        if (publishedVersion == null) return null;

        return new ContentPageVersionDto
        {
            Id = publishedVersion.Id,
            ContentPageId = publishedVersion.ContentPageId,
            Content = publishedVersion.Content,
            ChangeNotes = publishedVersion.ChangeNotes,
            VersionNumber = publishedVersion.VersionNumber,
            IsPublished = publishedVersion.IsPublished,
            PublishedAt = publishedVersion.PublishedAt,
            PublishedByUsername = publishedVersion.PublishedByUser?.Username,
            CreatedByUsername = publishedVersion.CreatedByUser.Username,
            CreatedAt = publishedVersion.CreatedAt
        };
    }

    public async Task<ContentPageDto> CreateContentPageAsync(string title, string slug, ContentPageType type)
    {
        // Check if a page with this type already exists
        var existingPage = await _context.ContentPages
            .FirstOrDefaultAsync(cp => cp.Type == type);

        if (existingPage != null)
            throw new InvalidOperationException($"A content page of type {type} already exists");

        // Check if a page with this slug already exists
        var existingSlug = await _context.ContentPages
            .FirstOrDefaultAsync(cp => cp.Slug == slug);

        if (existingSlug != null)
            throw new InvalidOperationException($"A content page with slug '{slug}' already exists");

        var contentPage = new ContentPage
        {
            Title = title,
            Slug = slug,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ContentPages.Add(contentPage);
        await _context.SaveChangesAsync();

        return new ContentPageDto
        {
            Id = contentPage.Id,
            Title = contentPage.Title,
            Slug = contentPage.Slug,
            Type = contentPage.Type,
            PublishedVersionId = contentPage.PublishedVersionId,
            PublishedVersion = null,
            CreatedAt = contentPage.CreatedAt,
            UpdatedAt = contentPage.UpdatedAt,
            TotalVersions = 0
        };
    }

    public async Task<ContentPageDto?> UpdateContentPageAsync(int id, string title, string slug)
    {
        var contentPage = await _context.ContentPages
            .Include(cp => cp.PublishedVersion)
                .ThenInclude(v => v!.CreatedByUser)
            .Include(cp => cp.PublishedVersion)
                .ThenInclude(v => v!.PublishedByUser)
            .Include(cp => cp.Versions)
            .FirstOrDefaultAsync(cp => cp.Id == id);

        if (contentPage == null) return null;

        // Check if another page with this slug already exists
        var existingSlug = await _context.ContentPages
            .FirstOrDefaultAsync(cp => cp.Slug == slug && cp.Id != id);

        if (existingSlug != null)
            throw new InvalidOperationException($"A content page with slug '{slug}' already exists");

        contentPage.Title = title;
        contentPage.Slug = slug;
        contentPage.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ContentPageDto
        {
            Id = contentPage.Id,
            Title = contentPage.Title,
            Slug = contentPage.Slug,
            Type = contentPage.Type,
            PublishedVersionId = contentPage.PublishedVersionId,
            PublishedVersion = contentPage.PublishedVersion != null ? new ContentPageVersionDto
            {
                Id = contentPage.PublishedVersion.Id,
                ContentPageId = contentPage.PublishedVersion.ContentPageId,
                Content = contentPage.PublishedVersion.Content,
                ChangeNotes = contentPage.PublishedVersion.ChangeNotes,
                VersionNumber = contentPage.PublishedVersion.VersionNumber,
                IsPublished = contentPage.PublishedVersion.IsPublished,
                PublishedAt = contentPage.PublishedVersion.PublishedAt,
                PublishedByUsername = contentPage.PublishedVersion.PublishedByUser?.Username,
                CreatedByUsername = contentPage.PublishedVersion.CreatedByUser.Username,
                CreatedAt = contentPage.PublishedVersion.CreatedAt
            } : null,
            CreatedAt = contentPage.CreatedAt,
            UpdatedAt = contentPage.UpdatedAt,
            TotalVersions = contentPage.Versions.Count
        };
    }

    public async Task<bool> DeleteContentPageAsync(int id)
    {
        var contentPage = await _context.ContentPages
            .Include(cp => cp.Versions)
            .FirstOrDefaultAsync(cp => cp.Id == id);

        if (contentPage == null) return false;

        // Remove all versions first
        _context.ContentPageVersions.RemoveRange(contentPage.Versions);

        // Remove the content page
        _context.ContentPages.Remove(contentPage);

        await _context.SaveChangesAsync();
        return true;
    }
}
