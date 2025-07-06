using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public interface ITagService
{
    Task<IEnumerable<TagDto>> SearchTagsAsync(string query, int limit = 20);
    Task<IEnumerable<TagDto>> GetTrendingTagsAsync(int limit = 10);
    Task<IEnumerable<PostDto>> GetPostsByTagAsync(string tagName, int? currentUserId = null, int page = 1, int pageSize = 25);
    Task<TagDto?> GetTagByNameAsync(string tagName);
}
