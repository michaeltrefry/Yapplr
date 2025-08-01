using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Analytics;
using Xunit;

namespace Yapplr.Api.Tests.Services;

public class ExploreServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly Mock<ITrendingService> _mockTrendingService;
    private readonly Mock<ITagAnalyticsService> _mockTagAnalyticsService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<ExploreService>> _mockLogger;
    private readonly ExploreService _service;

    public ExploreServiceTests()
    {
        _context = new TestYapplrDbContext();
        _mockTrendingService = new Mock<ITrendingService>();
        _mockTagAnalyticsService = new Mock<ITagAnalyticsService>();
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<ExploreService>>();
        
        _service = new ExploreService(
            _context,
            _mockTrendingService.Object,
            _mockTagAnalyticsService.Object,
            _mockUserService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetExplorePageAsync_ShouldReturnComprehensiveExploreData()
    {
        // Arrange
        await SeedTestDataAsync();
        SetupMockServices();

        // Act
        var result = await _service.GetExplorePageAsync(userId: 1);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.TrendingPosts);
        Assert.NotNull(result.TrendingHashtags);
        Assert.NotNull(result.TrendingCategories);
        Assert.NotNull(result.RecommendedUsers);
        Assert.NotNull(result.PersonalizedPosts);
        Assert.NotNull(result.Metrics);
        Assert.True(result.GeneratedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetUserRecommendationsAsync_ShouldReturnSimilarUsers()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;

        // Act
        var result = await _service.GetUserRecommendationsAsync(userId, limit: 5);

        // Assert
        Assert.NotNull(result);
        var recommendations = result.ToList();
        
        // Should not recommend blocked users or users already followed
        foreach (var recommendation in recommendations)
        {
            Assert.NotEqual(userId, recommendation.User.Id);
            Assert.True(recommendation.SimilarityScore >= 0);
            Assert.True(recommendation.SimilarityScore <= 1);
            Assert.NotNull(recommendation.RecommendationReason);
        }
    }

    [Fact]
    public async Task CalculateUserSimilarityAsync_ShouldReturnValidScore()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId1 = 1;
        var userId2 = 2;

        // Act
        var similarity = await _service.CalculateUserSimilarityAsync(userId1, userId2);

        // Assert
        Assert.True(similarity >= 0);
        Assert.True(similarity <= 1);
    }

    [Fact]
    public async Task GetContentClustersAsync_ShouldReturnTopicClusters()
    {
        // Arrange
        await SeedTestDataAsync();
        SetupMockServices();

        // Act
        var result = await _service.GetContentClustersAsync(userId: 1, limit: 3);

        // Assert
        Assert.NotNull(result);
        var clusters = result.ToList();
        
        foreach (var cluster in clusters)
        {
            Assert.NotNull(cluster.Topic);
            Assert.NotNull(cluster.Description);
            Assert.NotNull(cluster.Posts);
            Assert.NotNull(cluster.RelatedHashtags);
            Assert.NotNull(cluster.TopContributors);
            Assert.True(cluster.ClusterScore >= 0);
        }
    }

    [Fact]
    public async Task GetSimilarUsersAsync_ShouldReturnUsersWithSimilarInterests()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;

        // Act
        var result = await _service.GetSimilarUsersAsync(userId, limit: 5);

        // Assert
        Assert.NotNull(result);
        var similarUsers = result.ToList();
        
        foreach (var similarUser in similarUsers)
        {
            Assert.NotEqual(userId, similarUser.User.Id);
            Assert.True(similarUser.SimilarityScore >= 0);
            Assert.NotNull(similarUser.SharedInterests);
            Assert.NotNull(similarUser.SimilarityReason);
        }
    }

    [Fact]
    public async Task GetExploreSectionsAsync_ShouldReturnModularSections()
    {
        // Arrange
        await SeedTestDataAsync();
        SetupMockServices();
        var sectionTypes = new[] { "trending_posts", "trending_hashtags" };

        // Act
        var result = await _service.GetExploreSectionsAsync(userId: 1, sectionTypes: sectionTypes);

        // Assert
        Assert.NotNull(result);
        var sections = result.ToList();
        Assert.True(sections.Count <= sectionTypes.Length);
        
        foreach (var section in sections)
        {
            Assert.NotNull(section.SectionType);
            Assert.NotNull(section.Title);
            Assert.NotNull(section.Content);
            Assert.True(section.Priority >= 0);
        }
    }

    private async Task SeedTestDataAsync()
    {
        // Create test users
        var users = new[]
        {
            new User { Id = 1, Username = "user1", Email = "user1@test.com", TrustScore = 0.8f, Status = UserStatus.Active },
            new User { Id = 2, Username = "user2", Email = "user2@test.com", TrustScore = 0.7f, Status = UserStatus.Active },
            new User { Id = 3, Username = "user3", Email = "user3@test.com", TrustScore = 0.9f, Status = UserStatus.Active },
            new User { Id = 4, Username = "user4", Email = "user4@test.com", TrustScore = 0.6f, Status = UserStatus.Active }
        };
        _context.Users.AddRange(users);

        // Create test tags
        var tags = new[]
        {
            new Tag { Id = 1, Name = "technology", PostCount = 10 },
            new Tag { Id = 2, Name = "sports", PostCount = 8 },
            new Tag { Id = 3, Name = "music", PostCount = 12 },
            new Tag { Id = 4, Name = "programming", PostCount = 15 }
        };
        _context.Tags.AddRange(tags);

        // Create test posts
        var posts = new[]
        {
            new Post { Id = 1, UserId = 1, Content = "Tech post", PostType = PostType.Post, CreatedAt = DateTime.UtcNow.AddHours(-2), Privacy = PostPrivacy.Public },
            new Post { Id = 2, UserId = 2, Content = "Sports post", PostType = PostType.Post, CreatedAt = DateTime.UtcNow.AddHours(-1), Privacy = PostPrivacy.Public },
            new Post { Id = 3, UserId = 3, Content = "Music post", PostType = PostType.Post, CreatedAt = DateTime.UtcNow.AddMinutes(-30), Privacy = PostPrivacy.Public },
            new Post { Id = 4, UserId = 4, Content = "Programming post", PostType = PostType.Post, CreatedAt = DateTime.UtcNow.AddMinutes(-15), Privacy = PostPrivacy.Public }
        };
        _context.Posts.AddRange(posts);

        // Create post-tag relationships
        var postTags = new[]
        {
            new PostTag { PostId = 1, TagId = 1, CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new PostTag { PostId = 2, TagId = 2, CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new PostTag { PostId = 3, TagId = 3, CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
            new PostTag { PostId = 4, TagId = 4, CreatedAt = DateTime.UtcNow.AddMinutes(-15) }
        };
        _context.PostTags.AddRange(postTags);

        // Create some follows
        var follows = new[]
        {
            new Follow { FollowerId = 1, FollowingId = 2, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new Follow { FollowerId = 2, FollowingId = 3, CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new Follow { FollowerId = 3, FollowingId = 4, CreatedAt = DateTime.UtcNow.AddDays(-3) }
        };
        _context.Follows.AddRange(follows);

        // Create some tag analytics for user interests
        var tagAnalytics = new[]
        {
            new TagAnalytics { Id = 1, TagId = 1, UserId = 1, Action = TagAction.Used, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new TagAnalytics { Id = 2, TagId = 4, UserId = 1, Action = TagAction.Clicked, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new TagAnalytics { Id = 3, TagId = 2, UserId = 2, Action = TagAction.Used, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };
        _context.TagAnalytics.AddRange(tagAnalytics);

        await _context.SaveChangesAsync();
    }

    private void SetupMockServices()
    {
        // Setup trending service mock
        _mockTrendingService.Setup(x => x.GetTrendingPostsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<PostDto>
            {
                CreateTestPostDto(1, "Trending post 1", 50, 10, 5),
                CreateTestPostDto(2, "Trending post 2", 30, 8, 3)
            });

        _mockTrendingService.Setup(x => x.GetPersonalizedTrendingPostsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<PostDto>
            {
                CreateTestPostDto(3, "Personalized post", 25, 5, 2)
            });

        // Setup tag analytics service mock
        _mockTagAnalyticsService.Setup(x => x.GetTrendingHashtagsWithVelocityAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<TrendingHashtagDto>
            {
                new TrendingHashtagDto("technology", 10, 5, 1.0, 0.8, 50, 8, 0.9f, "Technology", 1.0, 0.6, 0.8),
                new TrendingHashtagDto("sports", 8, 6, 0.33, 0.7, 40, 6, 0.8f, "Sports", 0.33, 0.5, 0.75)
            });

        _mockTagAnalyticsService.Setup(x => x.GetTrendingHashtagsByCategoryAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<CategoryTrendingDto>
            {
                new CategoryTrendingDto("Technology", new List<TrendingHashtagDto>(), 10, 1.0, "Tech trends"),
                new CategoryTrendingDto("Sports", new List<TrendingHashtagDto>(), 8, 0.5, "Sports trends")
            });
    }

    private static PostDto CreateTestPostDto(int id, string content, int likeCount, int commentCount, int repostCount)
    {
        var testUser = new UserDto(
            Id: 1,
            Email: "test@example.com",
            Username: "testuser",
            Bio: "",
            Birthday: null,
            Pronouns: "",
            Tagline: "",
            ProfileImageUrl: null,
            CreatedAt: DateTime.UtcNow,
            FcmToken: "",
            ExpoPushToken: "",
            EmailVerified: true,
            Role: UserRole.User,
            Status: UserStatus.Active,
            SuspendedUntil: null,
            SuspensionReason: null,
            SubscriptionTier: null
        );

        return new PostDto(
            Id: id,
            Content: content,
            ImageUrl: null,
            VideoUrl: null,
            VideoThumbnailUrl: null,
            VideoProcessingStatus: null,
            Privacy: PostPrivacy.Public,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            User: testUser,
            Group: null,
            LikeCount: likeCount,
            CommentCount: commentCount,
            RepostCount: repostCount,
            Tags: new List<TagDto>(),
            LinkPreviews: new List<LinkPreviewDto>(),
            IsLikedByCurrentUser: false,
            IsRepostedByCurrentUser: false,
            IsEdited: false,
            ModerationInfo: null,
            VideoMetadata: null,
            MediaItems: null,
            ReactionCounts: null,
            CurrentUserReaction: null,
            TotalReactionCount: likeCount,
            PostType: PostType.Post,
            RepostedPost: null
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
