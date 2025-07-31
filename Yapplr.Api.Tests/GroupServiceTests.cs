using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Unified;
using Yapplr.Api.DTOs;
using Yapplr.Api.Common;

namespace Yapplr.Api.Tests;

/// <summary>
/// Comprehensive tests for GroupService covering all functionality including:
/// - Group CRUD operations
/// - Group membership management
/// - Search and pagination
/// - Authorization and validation
/// - Error handling and edge cases
/// </summary>
public class GroupServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly GroupService _groupService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IUnifiedNotificationService> _mockNotificationService;
    private readonly Mock<ICountCacheService> _mockCountCacheService;
    private readonly Mock<ILogger<GroupService>> _mockLogger;

    public GroupServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestYapplrDbContext(options);
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockNotificationService = new Mock<IUnifiedNotificationService>();
        _mockCountCacheService = new Mock<ICountCacheService>();
        _mockLogger = new Mock<ILogger<GroupService>>();

        _groupService = new GroupService(
            _context,
            _mockHttpContextAccessor.Object,
            _mockNotificationService.Object,
            _mockCountCacheService.Object,
            _mockLogger.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        // Create test users
        var users = new[]
        {
            new User
            {
                Id = 1,
                Username = "testuser1",
                Email = "test1@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                Status = UserStatus.Active
            },
            new User
            {
                Id = 2,
                Username = "testuser2",
                Email = "test2@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                Status = UserStatus.Active
            },
            new User
            {
                Id = 3,
                Username = "testuser3",
                Email = "test3@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Status = UserStatus.Active
            }
        };

        _context.Users.AddRange(users);

        // Create test groups
        var groups = new[]
        {
            new Group
            {
                Id = 1,
                Name = "Test Group 1",
                Description = "First test group",
                UserId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Group
            {
                Id = 2,
                Name = "Test Group 2",
                Description = "Second test group",
                UserId = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        _context.Groups.AddRange(groups);

        // Create group memberships
        var memberships = new[]
        {
            new GroupMember
            {
                Id = 1,
                GroupId = 1,
                UserId = 1,
                Role = GroupMemberRole.Admin,
                JoinedAt = DateTime.UtcNow.AddDays(-15)
            },
            new GroupMember
            {
                Id = 2,
                GroupId = 1,
                UserId = 2,
                Role = GroupMemberRole.Member,
                JoinedAt = DateTime.UtcNow.AddDays(-12)
            },
            new GroupMember
            {
                Id = 3,
                GroupId = 2,
                UserId = 2,
                Role = GroupMemberRole.Admin,
                JoinedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        _context.GroupMembers.AddRange(memberships);

        // Create test posts in groups
        var posts = new[]
        {
            new Post
            {
                Id = 1,
                Content = "Test post in group 1",
                UserId = 1,
                GroupId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Privacy = PostPrivacy.Public
            },
            new Post
            {
                Id = 2,
                Content = "Another test post in group 1",
                UserId = 2,
                GroupId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Privacy = PostPrivacy.Public
            }
        };

        _context.Posts.AddRange(posts);
        _context.SaveChanges();
    }

    #region CreateGroupAsync Tests

    [Fact]
    public async Task CreateGroupAsync_WithValidData_ShouldCreateGroupSuccessfully()
    {
        // Arrange
        var createDto = new CreateGroupDto("New Test Group", "A new group for testing", "http://test.com/api/images/test-image.jpg");
        var userId = 1;

        // Act
        var result = await _groupService.CreateGroupAsync(userId, createDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("New Test Group");
        result.Data.Description.Should().Be("A new group for testing");
        result.Data.ImageUrl.Should().Be("http://test.com/api/images/test-image.jpg");
        result.Data.User.Id.Should().Be(userId);
        result.Data.IsCurrentUserMember.Should().BeTrue();
        result.Data.MemberCount.Should().Be(1);

        // Verify group was created in database
        var groupInDb = await _context.Groups.FindAsync(result.Data.Id);
        groupInDb.Should().NotBeNull();
        groupInDb!.Name.Should().Be("New Test Group");

        // Verify creator was added as admin member
        var membership = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == result.Data.Id && m.UserId == userId);
        membership.Should().NotBeNull();
        membership!.Role.Should().Be(GroupMemberRole.Admin);
    }

    [Fact]
    public async Task CreateGroupAsync_WithDuplicateName_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateGroupDto("Test Group 1", "Duplicate name group"); // Name already exists
        var userId = 1;

        // Act
        var result = await _groupService.CreateGroupAsync(userId, createDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("A group with this name already exists.");
    }

    [Fact]
    public async Task CreateGroupAsync_WithMinimalData_ShouldCreateGroupSuccessfully()
    {
        // Arrange
        var createDto = new CreateGroupDto("Min Group"); // Only required name
        var userId = 1;

        // Act
        var result = await _groupService.CreateGroupAsync(userId, createDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Min Group");
        result.Data.Description.Should().Be("");
        result.Data.ImageUrl.Should().BeNull();
    }

    #endregion

    #region UpdateGroupAsync Tests

    [Fact]
    public async Task UpdateGroupAsync_WithValidDataByOwner_ShouldUpdateSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateGroupDto("Updated Group Name", "Updated description", "http://test.com/api/images/new-image.jpg");
        var groupId = 1;
        var userId = 1; // Owner of group 1

        // Act
        var result = await _groupService.UpdateGroupAsync(groupId, userId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Updated Group Name");
        result.Data.Description.Should().Be("Updated description");
        result.Data.ImageUrl.Should().Be("http://test.com/api/images/new-image.jpg");

        // Verify in database
        var groupInDb = await _context.Groups.FindAsync(groupId);
        groupInDb!.Name.Should().Be("Updated Group Name");
        groupInDb.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateGroupAsync_WithNonExistentGroup_ShouldReturnFailure()
    {
        // Arrange
        var updateDto = new UpdateGroupDto("Updated Name", "Updated description");
        var groupId = 999; // Non-existent group
        var userId = 1;

        // Act
        var result = await _groupService.UpdateGroupAsync(groupId, userId, updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Group not found.");
    }

    [Fact]
    public async Task UpdateGroupAsync_ByNonOwner_ShouldReturnFailure()
    {
        // Arrange
        var updateDto = new UpdateGroupDto("Updated Name", "Updated description");
        var groupId = 1;
        var userId = 2; // Not the owner of group 1

        // Act
        var result = await _groupService.UpdateGroupAsync(groupId, userId, updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Only the group owner can update the group.");
    }

    [Fact]
    public async Task UpdateGroupAsync_WithDuplicateName_ShouldReturnFailure()
    {
        // Arrange
        var updateDto = new UpdateGroupDto("Test Group 2", "Updated description"); // Name of group 2
        var groupId = 1;
        var userId = 1; // Owner of group 1

        // Act
        var result = await _groupService.UpdateGroupAsync(groupId, userId, updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("A group with this name already exists.");
    }

    [Fact]
    public async Task UpdateGroupAsync_WithSameName_ShouldUpdateSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateGroupDto("Test Group 1", "Updated description"); // Same name as current
        var groupId = 1;
        var userId = 1; // Owner of group 1

        // Act
        var result = await _groupService.UpdateGroupAsync(groupId, userId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Description.Should().Be("Updated description");
    }

    #endregion

    #region DeleteGroupAsync Tests

    [Fact]
    public async Task DeleteGroupAsync_ByOwner_ShouldDeleteSuccessfully()
    {
        // Arrange
        var groupId = 1;
        var userId = 1; // Owner of group 1

        // Act
        var result = await _groupService.DeleteGroupAsync(groupId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Verify group is deleted from database
        var groupInDb = await _context.Groups.FindAsync(groupId);
        groupInDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteGroupAsync_WithNonExistentGroup_ShouldReturnFailure()
    {
        // Arrange
        var groupId = 999; // Non-existent group
        var userId = 1;

        // Act
        var result = await _groupService.DeleteGroupAsync(groupId, userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Group not found.");
    }

    [Fact]
    public async Task DeleteGroupAsync_ByNonOwner_ShouldReturnFailure()
    {
        // Arrange
        var groupId = 1;
        var userId = 2; // Not the owner of group 1

        // Act
        var result = await _groupService.DeleteGroupAsync(groupId, userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Only the group owner can delete the group.");
    }

    #endregion

    #region GetGroupByIdAsync Tests

    [Fact]
    public async Task GetGroupByIdAsync_WithExistingGroup_ShouldReturnGroup()
    {
        // Arrange
        var groupId = 1;
        var currentUserId = 2; // Member of group 1

        // Act
        var result = await _groupService.GetGroupByIdAsync(groupId, currentUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(groupId);
        result.Name.Should().Be("Test Group 1");
        result.IsCurrentUserMember.Should().BeTrue();
        result.MemberCount.Should().Be(2);
        result.PostCount.Should().Be(2);
    }

    [Fact]
    public async Task GetGroupByIdAsync_WithNonExistentGroup_ShouldReturnNull()
    {
        // Arrange
        var groupId = 999; // Non-existent group

        // Act
        var result = await _groupService.GetGroupByIdAsync(groupId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGroupByIdAsync_WithoutCurrentUser_ShouldReturnGroupWithoutMembershipInfo()
    {
        // Arrange
        var groupId = 1;

        // Act
        var result = await _groupService.GetGroupByIdAsync(groupId);

        // Assert
        result.Should().NotBeNull();
        result!.IsCurrentUserMember.Should().BeFalse();
    }

    #endregion

    #region GetGroupByNameAsync Tests

    [Fact]
    public async Task GetGroupByNameAsync_WithExistingGroup_ShouldReturnGroup()
    {
        // Arrange
        var groupName = "Test Group 1";
        var currentUserId = 1;

        // Act
        var result = await _groupService.GetGroupByNameAsync(groupName, currentUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(groupName);
        result.IsCurrentUserMember.Should().BeTrue();
    }

    [Fact]
    public async Task GetGroupByNameAsync_WithNonExistentGroup_ShouldReturnNull()
    {
        // Arrange
        var groupName = "Non-existent Group";

        // Act
        var result = await _groupService.GetGroupByNameAsync(groupName);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetGroupsAsync Tests

    [Fact]
    public async Task GetGroupsAsync_ShouldReturnPaginatedGroups()
    {
        // Arrange
        var currentUserId = 1;
        var page = 1;
        var pageSize = 10;

        // Act
        var result = await _groupService.GetGroupsAsync(currentUserId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
        result.HasNextPage.Should().BeFalse();

        // Verify ordering (newest first)
        result.Items.First().Name.Should().Be("Test Group 2");
        result.Items.Last().Name.Should().Be("Test Group 1");
    }

    [Fact]
    public async Task GetGroupsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var page = 2;
        var pageSize = 1;

        // Act
        var result = await _groupService.GetGroupsAsync(null, page, pageSize);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Test Group 1"); // Second newest
        result.HasNextPage.Should().BeFalse();
    }

    #endregion

    #region SearchGroupsAsync Tests

    [Fact]
    public async Task SearchGroupsAsync_WithNameMatch_ShouldReturnMatchingGroups()
    {
        // Arrange
        var query = "Test Group 1";
        var currentUserId = 1;

        // Act
        var result = await _groupService.SearchGroupsAsync(query, currentUserId);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Test Group 1");
    }

    [Fact]
    public async Task SearchGroupsAsync_WithDescriptionMatch_ShouldReturnMatchingGroups()
    {
        // Arrange
        var query = "First test";
        var currentUserId = 1;

        // Act
        var result = await _groupService.SearchGroupsAsync(query, currentUserId);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Test Group 1");
    }

    [Fact]
    public async Task SearchGroupsAsync_WithPartialMatch_ShouldReturnMatchingGroups()
    {
        // Arrange
        var query = "Test";

        // Act
        var result = await _groupService.SearchGroupsAsync(query);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchGroupsAsync_WithNoMatch_ShouldReturnEmptyResult()
    {
        // Arrange
        var query = "NonExistentGroup";

        // Act
        var result = await _groupService.SearchGroupsAsync(query);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region GetUserGroupsAsync Tests

    [Fact]
    public async Task GetUserGroupsAsync_ShouldReturnUserGroups()
    {
        // Arrange
        var userId = 2; // Member of both groups
        var currentUserId = 1;

        // Act
        var result = await _groupService.GetUserGroupsAsync(userId, currentUserId);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(g => g.Name == "Test Group 1");
        result.Items.Should().Contain(g => g.Name == "Test Group 2");
    }

    [Fact]
    public async Task GetUserGroupsAsync_WithUserNotInAnyGroup_ShouldReturnEmptyResult()
    {
        // Arrange
        var userId = 3; // Not a member of any group

        // Act
        var result = await _groupService.GetUserGroupsAsync(userId);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region JoinGroupAsync Tests

    [Fact]
    public async Task JoinGroupAsync_WithValidGroup_ShouldJoinSuccessfully()
    {
        // Arrange
        var groupId = 2;
        var userId = 3; // Not currently a member

        // Act
        var result = await _groupService.JoinGroupAsync(groupId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Verify membership was created
        var membership = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        membership.Should().NotBeNull();
        membership!.Role.Should().Be(GroupMemberRole.Member);
    }

    [Fact]
    public async Task JoinGroupAsync_WithNonExistentGroup_ShouldReturnFailure()
    {
        // Arrange
        var groupId = 999; // Non-existent group
        var userId = 3;

        // Act
        var result = await _groupService.JoinGroupAsync(groupId, userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Group not found.");
    }

    [Fact]
    public async Task JoinGroupAsync_WhenAlreadyMember_ShouldReturnFailure()
    {
        // Arrange
        var groupId = 1;
        var userId = 2; // Already a member

        // Act
        var result = await _groupService.JoinGroupAsync(groupId, userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User is already a member of this group.");
    }

    #endregion

    #region LeaveGroupAsync Tests

    [Fact]
    public async Task LeaveGroupAsync_WithValidMembership_ShouldLeaveSuccessfully()
    {
        // Arrange
        var groupId = 1;
        var userId = 2; // Member but not owner

        // Act
        var result = await _groupService.LeaveGroupAsync(groupId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Verify membership was removed
        var membership = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        membership.Should().BeNull();
    }

    [Fact]
    public async Task LeaveGroupAsync_WhenNotMember_ShouldReturnFailure()
    {
        // Arrange
        var groupId = 1;
        var userId = 3; // Not a member

        // Act
        var result = await _groupService.LeaveGroupAsync(groupId, userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User is not a member of this group.");
    }

    [Fact]
    public async Task LeaveGroupAsync_WhenOwner_ShouldReturnFailure()
    {
        // Arrange
        var groupId = 1;
        var userId = 1; // Owner of group 1

        // Act
        var result = await _groupService.LeaveGroupAsync(groupId, userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Group owners cannot leave their own group. Delete the group instead.");
    }

    #endregion

    #region IsUserMemberAsync Tests

    [Fact]
    public async Task IsUserMemberAsync_WhenUserIsMember_ShouldReturnTrue()
    {
        // Arrange
        var groupId = 1;
        var userId = 2; // Member of group 1

        // Act
        var result = await _groupService.IsUserMemberAsync(groupId, userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserMemberAsync_WhenUserIsNotMember_ShouldReturnFalse()
    {
        // Arrange
        var groupId = 1;
        var userId = 3; // Not a member of group 1

        // Act
        var result = await _groupService.IsUserMemberAsync(groupId, userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetGroupMembersAsync Tests

    [Fact]
    public async Task GetGroupMembersAsync_ShouldReturnPaginatedMembers()
    {
        // Arrange
        var groupId = 1;

        // Act
        var result = await _groupService.GetGroupMembersAsync(groupId);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        // Verify ordering (newest first)
        result.Items.First().User.Id.Should().Be(2); // Joined later
        result.Items.Last().User.Id.Should().Be(1); // Joined first (owner)

        // Verify roles
        result.Items.Should().Contain(m => m.User.Id == 1 && m.Role == GroupMemberRole.Admin);
        result.Items.Should().Contain(m => m.User.Id == 2 && m.Role == GroupMemberRole.Member);
    }

    [Fact]
    public async Task GetGroupMembersAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var groupId = 1;
        var page = 2;
        var pageSize = 1;

        // Act
        var result = await _groupService.GetGroupMembersAsync(groupId, page, pageSize);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().User.Id.Should().Be(1); // Second newest member
    }

    #endregion

    #region GetGroupPostsAsync Tests

    [Fact]
    public async Task GetGroupPostsAsync_ShouldReturnPaginatedPosts()
    {
        // Arrange
        var groupId = 1;
        var currentUserId = 1;

        // Act
        var result = await _groupService.GetGroupPostsAsync(groupId, currentUserId);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        // Verify ordering (newest first)
        result.Items.First().Content.Should().Be("Another test post in group 1");
        result.Items.Last().Content.Should().Be("Test post in group 1");
    }

    [Fact]
    public async Task GetGroupPostsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var groupId = 1;
        var page = 2;
        var pageSize = 1;

        // Act
        var result = await _groupService.GetGroupPostsAsync(groupId, null, page, pageSize);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Content.Should().Be("Test post in group 1"); // Older post
    }

    #endregion

    #region IsGroupOwnerAsync Tests

    [Fact]
    public async Task IsGroupOwnerAsync_WhenUserIsOwner_ShouldReturnTrue()
    {
        // Arrange
        var groupId = 1;
        var userId = 1; // Owner of group 1

        // Act
        var result = await _groupService.IsGroupOwnerAsync(groupId, userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGroupOwnerAsync_WhenUserIsNotOwner_ShouldReturnFalse()
    {
        // Arrange
        var groupId = 1;
        var userId = 2; // Not owner of group 1

        // Act
        var result = await _groupService.IsGroupOwnerAsync(groupId, userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGroupOwnerAsync_WithNonExistentGroup_ShouldReturnFalse()
    {
        // Arrange
        var groupId = 999; // Non-existent group
        var userId = 1;

        // Act
        var result = await _groupService.IsGroupOwnerAsync(groupId, userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetGroupEntityByIdAsync Tests

    [Fact]
    public async Task GetGroupEntityByIdAsync_WithExistingGroup_ShouldReturnEntity()
    {
        // Arrange
        var groupId = 1;

        // Act
        var result = await _groupService.GetGroupEntityByIdAsync(groupId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(groupId);
        result.Name.Should().Be("Test Group 1");
    }

    [Fact]
    public async Task GetGroupEntityByIdAsync_WithNonExistentGroup_ShouldReturnNull()
    {
        // Arrange
        var groupId = 999; // Non-existent group

        // Act
        var result = await _groupService.GetGroupEntityByIdAsync(groupId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task CreateGroupAsync_WithVeryLongDescription_ShouldSucceed()
    {
        // Arrange - Test with maximum allowed description length
        var longDescription = new string('A', 500); // Max length is 500
        var createDto = new CreateGroupDto("Long Desc Group", longDescription);
        var userId = 1;

        // Act
        var result = await _groupService.CreateGroupAsync(userId, createDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Description.Should().Be(longDescription);
    }

    [Fact]
    public async Task JoinGroupAsync_MultipleTimesRapidly_ShouldHandleGracefully()
    {
        // Arrange
        var groupId = 2;
        var userId = 3; // Not currently a member

        // Act - First join should succeed
        var result1 = await _groupService.JoinGroupAsync(groupId, userId);

        // Second join should fail gracefully
        var result2 = await _groupService.JoinGroupAsync(groupId, userId);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeFalse();
        result2.ErrorMessage.Should().Be("User is already a member of this group.");
    }

    #endregion
}
