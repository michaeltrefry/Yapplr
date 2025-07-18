using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Common;

namespace Yapplr.Api.Services;

public interface IGroupService
{
    // Group management
    Task<ServiceResult<GroupDto>> CreateGroupAsync(int userId, CreateGroupDto createDto);
    Task<ServiceResult<GroupDto>> UpdateGroupAsync(int groupId, int userId, UpdateGroupDto updateDto);
    Task<ServiceResult<bool>> DeleteGroupAsync(int groupId, int userId);
    
    // Group retrieval
    Task<GroupDto?> GetGroupByIdAsync(int groupId, int? currentUserId = null);
    Task<GroupDto?> GetGroupByNameAsync(string name, int? currentUserId = null);
    Task<PaginatedResult<GroupListDto>> GetGroupsAsync(int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<PaginatedResult<GroupListDto>> SearchGroupsAsync(string query, int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<PaginatedResult<GroupListDto>> GetUserGroupsAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20);
    
    // Group membership
    Task<ServiceResult<bool>> JoinGroupAsync(int groupId, int userId);
    Task<ServiceResult<bool>> LeaveGroupAsync(int groupId, int userId);
    Task<bool> IsUserMemberAsync(int groupId, int userId);
    Task<PaginatedResult<GroupMemberDto>> GetGroupMembersAsync(int groupId, int page = 1, int pageSize = 20);
    
    // Group posts
    Task<PaginatedResult<PostDto>> GetGroupPostsAsync(int groupId, int? currentUserId = null, int page = 1, int pageSize = 20);
    
    // Admin methods
    Task<Group?> GetGroupEntityByIdAsync(int groupId);
    Task<bool> IsGroupOwnerAsync(int groupId, int userId);
}
