using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;
using Yapplr.Api.Common;
using Serilog.Context;

namespace Yapplr.Api.Services;

public class GroupService : BaseService, IGroupService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INotificationService _notificationService;
    private readonly ICountCacheService _countCache;

    public GroupService(
        YapplrDbContext context,
        IHttpContextAccessor httpContextAccessor,
        INotificationService notificationService,
        ICountCacheService countCache,
        ILogger<GroupService> logger) : base(context, logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _notificationService = notificationService;
        _countCache = countCache;
    }

    public async Task<ServiceResult<GroupDto>> CreateGroupAsync(int userId, CreateGroupDto createDto)
    {
        using var activity = LogContext.PushProperty("Operation", "CreateGroup");
        
        try
        {
            // Check if group name already exists
            var existingGroup = await _context.Groups
                .FirstOrDefaultAsync(g => g.Name == createDto.Name);
                
            if (existingGroup != null)
            {
                return ServiceResult<GroupDto>.Failure("A group with this name already exists.");
            }

            var group = new Group
            {
                Name = createDto.Name,
                Description = createDto.Description,
                ImageFileName = createDto.ImageFileName,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // Automatically add the creator as a member with Admin role
            var membership = new GroupMember
            {
                GroupId = group.Id,
                UserId = userId,
                Role = GroupMemberRole.Admin,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(membership);
            await _context.SaveChangesAsync();

            // Load the group with all necessary data for DTO conversion
            var createdGroup = await GetGroupWithIncludesAsync(group.Id);
            if (createdGroup == null)
            {
                return ServiceResult<GroupDto>.Failure("Failed to retrieve created group.");
            }

            var groupDto = MapToGroupDto(createdGroup, userId);
            
            _logger.LogInformation("Group created successfully: {GroupId} by user {UserId}", group.Id, userId);
            
            return ServiceResult<GroupDto>.Success(groupDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group for user {UserId}", userId);
            return ServiceResult<GroupDto>.Failure("An error occurred while creating the group.");
        }
    }

    public async Task<ServiceResult<GroupDto>> UpdateGroupAsync(int groupId, int userId, UpdateGroupDto updateDto)
    {
        using var activity = LogContext.PushProperty("Operation", "UpdateGroup");
        
        try
        {
            var group = await _context.Groups.FindAsync(groupId);
            
            if (group == null)
            {
                return ServiceResult<GroupDto>.Failure("Group not found.");
            }

            // Check if user is the group owner
            if (group.UserId != userId)
            {
                return ServiceResult<GroupDto>.Failure("Only the group owner can update the group.");
            }

            // Check if new name conflicts with existing groups (excluding current group)
            if (group.Name != updateDto.Name)
            {
                var existingGroup = await _context.Groups
                    .FirstOrDefaultAsync(g => g.Name == updateDto.Name && g.Id != groupId);
                    
                if (existingGroup != null)
                {
                    return ServiceResult<GroupDto>.Failure("A group with this name already exists.");
                }
            }

            group.Name = updateDto.Name;
            group.Description = updateDto.Description;
            group.ImageFileName = updateDto.ImageFileName;
            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var updatedGroup = await GetGroupWithIncludesAsync(groupId);
            if (updatedGroup == null)
            {
                return ServiceResult<GroupDto>.Failure("Failed to retrieve updated group.");
            }

            var groupDto = MapToGroupDto(updatedGroup, userId);
            
            _logger.LogInformation("Group updated successfully: {GroupId} by user {UserId}", groupId, userId);
            
            return ServiceResult<GroupDto>.Success(groupDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId} for user {UserId}", groupId, userId);
            return ServiceResult<GroupDto>.Failure("An error occurred while updating the group.");
        }
    }

    public async Task<ServiceResult<bool>> DeleteGroupAsync(int groupId, int userId)
    {
        using var activity = LogContext.PushProperty("Operation", "DeleteGroup");
        
        try
        {
            var group = await _context.Groups.FindAsync(groupId);
            
            if (group == null)
            {
                return ServiceResult<bool>.Failure("Group not found.");
            }

            // Check if user is the group owner
            if (group.UserId != userId)
            {
                return ServiceResult<bool>.Failure("Only the group owner can delete the group.");
            }

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Group deleted successfully: {GroupId} by user {UserId}", groupId, userId);
            
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId} for user {UserId}", groupId, userId);
            return ServiceResult<bool>.Failure("An error occurred while deleting the group.");
        }
    }

    public async Task<GroupDto?> GetGroupByIdAsync(int groupId, int? currentUserId = null)
    {
        var group = await GetGroupWithIncludesAsync(groupId);
        
        if (group == null)
            return null;

        return MapToGroupDto(group, currentUserId);
    }

    public async Task<GroupDto?> GetGroupByNameAsync(string name, int? currentUserId = null)
    {
        var group = await _context.Groups
            .Include(g => g.User)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.Posts)
            .FirstOrDefaultAsync(g => g.Name == name);
        
        if (group == null)
            return null;

        return MapToGroupDto(group, currentUserId);
    }

    public async Task<PaginatedResult<GroupListDto>> GetGroupsAsync(int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Groups
            .Include(g => g.User)
            .Include(g => g.Members)
            .Include(g => g.Posts)
            .OrderByDescending(g => g.CreatedAt);

        var totalCount = await query.CountAsync();
        var groups = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var groupDtos = groups.Select(g => MapToGroupListDto(g, currentUserId)).ToList();

        return PaginatedResult<GroupListDto>.Create(groupDtos, page, pageSize, totalCount);
    }

    public async Task<PaginatedResult<GroupListDto>> SearchGroupsAsync(string query, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        var searchQuery = _context.Groups
            .Include(g => g.User)
            .Include(g => g.Members)
            .Include(g => g.Posts)
            .Where(g => g.Name.Contains(query) || g.Description.Contains(query))
            .OrderByDescending(g => g.CreatedAt);

        var totalCount = await searchQuery.CountAsync();
        var groups = await searchQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var groupDtos = groups.Select(g => MapToGroupListDto(g, currentUserId)).ToList();

        return PaginatedResult<GroupListDto>.Create(groupDtos, page, pageSize, totalCount);
    }

    public async Task<PaginatedResult<GroupListDto>> GetUserGroupsAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Groups
            .Include(g => g.User)
            .Include(g => g.Members)
            .Include(g => g.Posts)
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt);

        var totalCount = await query.CountAsync();
        var groups = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var groupDtos = groups.Select(g => MapToGroupListDto(g, currentUserId)).ToList();

        return PaginatedResult<GroupListDto>.Create(groupDtos, page, pageSize, totalCount);
    }

    public async Task<ServiceResult<bool>> JoinGroupAsync(int groupId, int userId)
    {
        using var activity = LogContext.PushProperty("Operation", "JoinGroup");

        try
        {
            var group = await _context.Groups.FindAsync(groupId);

            if (group == null)
            {
                return ServiceResult<bool>.Failure("Group not found.");
            }

            // Check if user is already a member
            var existingMembership = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (existingMembership != null)
            {
                return ServiceResult<bool>.Failure("User is already a member of this group.");
            }

            var membership = new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                Role = GroupMemberRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(membership);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} joined group {GroupId}", userId, groupId);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining group {GroupId} for user {UserId}", groupId, userId);
            return ServiceResult<bool>.Failure("An error occurred while joining the group.");
        }
    }

    public async Task<ServiceResult<bool>> LeaveGroupAsync(int groupId, int userId)
    {
        using var activity = LogContext.PushProperty("Operation", "LeaveGroup");

        try
        {
            var membership = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (membership == null)
            {
                return ServiceResult<bool>.Failure("User is not a member of this group.");
            }

            // Check if user is the group owner - they cannot leave their own group
            var group = await _context.Groups.FindAsync(groupId);
            if (group?.UserId == userId)
            {
                return ServiceResult<bool>.Failure("Group owners cannot leave their own group. Delete the group instead.");
            }

            _context.GroupMembers.Remove(membership);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving group {GroupId} for user {UserId}", groupId, userId);
            return ServiceResult<bool>.Failure("An error occurred while leaving the group.");
        }
    }

    private async Task<Group?> GetGroupWithIncludesAsync(int groupId)
    {
        return await _context.Groups
            .Include(g => g.User)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.Posts)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    private GroupDto MapToGroupDto(Group group, int? currentUserId)
    {
        var isCurrentUserMember = currentUserId.HasValue &&
            group.Members.Any(m => m.UserId == currentUserId.Value);

        // Calculate visible post count (same filtering as GetGroupPostsAsync)
        var visiblePostCount = group.Posts.Count(p =>
            p.PostType == PostType.Post && !p.IsHidden && !p.IsDeletedByUser);

        return new GroupDto(
            group.Id,
            group.Name,
            group.Description,
            group.ImageFileName,
            group.CreatedAt,
            group.UpdatedAt,
            group.IsOpen,
            group.User.ToDto(),
            group.Members.Count,
            visiblePostCount,
            isCurrentUserMember
        );
    }

    public Task<bool> IsUserMemberAsync(int groupId, int userId)
    {
        return _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
    }

    public async Task<PaginatedResult<GroupMemberDto>> GetGroupMembersAsync(int groupId, int page = 1, int pageSize = 20)
    {
        var query = _context.GroupMembers
            .Include(m => m.User)
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.JoinedAt);

        var totalCount = await query.CountAsync();
        var members = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var memberDtos = members.Select(m => new GroupMemberDto(
            m.Id,
            m.JoinedAt,
            m.Role,
            m.User.ToDto()
        )).ToList();

        return PaginatedResult<GroupMemberDto>.Create(memberDtos, page, pageSize, totalCount);
    }

    public async Task<PostDto?> GetGroupPostByIdAsync(int groupId, int postId, int? currentUserId = null)
    {
        // First check if user can access the group
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null) return null;

        // Check group access permissions
        if (!group.IsOpen && currentUserId.HasValue)
        {
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == currentUserId.Value);
            if (!isMember) return null;
        }
        else if (!group.IsOpen && !currentUserId.HasValue)
        {
            return null; // Private group, no user logged in
        }

        // Get the post
        var post = await _context.Posts
            .Where(p => p.Id == postId && p.GroupId == groupId && p.PostType == PostType.Post && !p.IsHidden && !p.IsDeletedByUser)
            .Include(p => p.User)
            .Include(p => p.Group)
            .Include(p => p.Likes)
            .Include(p => p.Reactions)
            .Include(p => p.Children.Where(c => c.PostType == PostType.Comment && !c.IsDeletedByUser && !c.IsHidden))
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .Include(p => p.PostMedia)
            .FirstOrDefaultAsync();

        if (post == null) return null;

        return post.MapToPostDto(currentUserId, _httpContextAccessor.HttpContext);
    }

    public async Task<PaginatedResult<PostDto>> GetGroupPostsAsync(int groupId, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Posts
            .Where(p => p.GroupId == groupId && p.PostType == PostType.Post && !p.IsHidden && !p.IsDeletedByUser)
            .Include(p => p.User)
            .Include(p => p.Group)
            .Include(p => p.Likes)
            .Include(p => p.Reactions)
            .Include(p => p.Children.Where(c => c.PostType == PostType.Comment && !c.IsDeletedByUser && !c.IsHidden))
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .Include(p => p.PostMedia)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var postDtos = posts.Select(p => p.MapToPostDto(currentUserId, _httpContextAccessor.HttpContext)).ToList();

        return PaginatedResult<PostDto>.Create(postDtos, page, pageSize, totalCount);
    }

    public async Task<Group?> GetGroupEntityByIdAsync(int groupId)
    {
        return await _context.Groups.FindAsync(groupId);
    }

    public async Task<bool> IsGroupOwnerAsync(int groupId, int userId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        return group?.UserId == userId;
    }

    private GroupListDto MapToGroupListDto(Group group, int? currentUserId)
    {
        var isCurrentUserMember = currentUserId.HasValue &&
            group.Members.Any(m => m.UserId == currentUserId.Value);

        // Calculate visible post count (same filtering as GetGroupPostsAsync)
        var visiblePostCount = group.Posts.Count(p =>
            p.PostType == PostType.Post && !p.IsHidden && !p.IsDeletedByUser);

        return new GroupListDto(
            group.Id,
            group.Name,
            group.Description,
            group.ImageFileName,
            group.CreatedAt,
            group.User.Username,
            group.Members.Count,
            visiblePostCount,
            isCurrentUserMember
        );
    }
}
