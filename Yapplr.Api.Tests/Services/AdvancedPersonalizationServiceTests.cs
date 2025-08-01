using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Common;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Personalization;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Analytics;
using Xunit;

namespace Yapplr.Api.Tests.Services;

public class AdvancedPersonalizationServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly Mock<ITagAnalyticsService> _mockTagAnalyticsService;
    private readonly Mock<ITrendingService> _mockTrendingService;
    private readonly Mock<ITopicService> _mockTopicService;
    private readonly Mock<ICachingService> _mockCachingService;
    private readonly Mock<ILogger<AdvancedPersonalizationService>> _mockLogger;
    private readonly AdvancedPersonalizationService _service;

    public AdvancedPersonalizationServiceTests()
    {
        _context = new TestYapplrDbContext();
        _mockTagAnalyticsService = new Mock<ITagAnalyticsService>();
        _mockTrendingService = new Mock<ITrendingService>();
        _mockTopicService = new Mock<ITopicService>();
        _mockCachingService = new Mock<ICachingService>();
        _mockLogger = new Mock<ILogger<AdvancedPersonalizationService>>();

        _service = new AdvancedPersonalizationService(
            _context,
            _mockTagAnalyticsService.Object,
            _mockTrendingService.Object,
            _mockTopicService.Object,
            _mockCachingService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetUserProfileAsync_WithNewUser_ShouldCreateInitialProfile()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.True(result.PersonalizationConfidence >= 0);
        Assert.NotNull(result.InterestScores);
        Assert.NotNull(result.ContentTypePreferences);
        Assert.NotNull(result.EngagementPatterns);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ShouldUpdateExistingProfile()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;

        // Create initial profile
        await _service.GetUserProfileAsync(userId);

        // Add some interaction data
        await SeedInteractionDataAsync(userId);

        // Act
        var result = await _service.UpdateUserProfileAsync(userId, forceRebuild: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.True(result.DataPointCount > 0);
        Assert.True(result.PersonalizationConfidence > 0);
    }

    [Fact]
    public async Task TrackInteractionAsync_ShouldStoreInteraction()
    {
        // Arrange
        await SeedTestDataAsync();
        var interactionEvent = new UserInteractionEventDto(
            UserId: 1,
            InteractionType: "like",
            TargetEntityType: "post",
            TargetEntityId: 1,
            InteractionStrength: 1.0f,
            DurationMs: null,
            Context: "test_context",
            DeviceInfo: "web",
            SessionId: "session123",
            IsImplicit: false,
            Sentiment: 0.8f
        );

        // Act
        var result = await _service.TrackInteractionAsync(interactionEvent);

        // Assert
        Assert.True(result);

        var storedInteraction = await _context.UserInteractionEvents
            .FirstOrDefaultAsync(e => e.UserId == 1 && e.InteractionType == "like");

        Assert.NotNull(storedInteraction);
        Assert.Equal("post", storedInteraction.TargetEntityType);
        Assert.Equal(1, storedInteraction.TargetEntityId);
        Assert.Equal(1.0f, storedInteraction.InteractionStrength);
    }

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_ShouldReturnRecommendations()
    {
        // Arrange
        await SeedTestDataAsync();
        await SeedInteractionDataAsync(1);

        // Setup mock trending service
        var mockUser = new UserDto(1, "user1", "User 1", "Test bio", DateTime.UtcNow, "user1", "User 1", null, DateTime.UtcNow, null, null, false, UserRole.User, UserStatus.Active, null, null, null);
        var mockPosts = new List<PostDto>
        {
            new PostDto(
                Id: 1,
                Content: "Test post",
                ImageUrl: null,
                VideoUrl: null,
                VideoThumbnailUrl: null,
                VideoProcessingStatus: null,
                Privacy: PostPrivacy.Public,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow,
                User: mockUser,
                Group: null,
                LikeCount: 0,
                CommentCount: 0,
                RepostCount: 0,
                Tags: Enumerable.Empty<TagDto>(),
                LinkPreviews: Enumerable.Empty<LinkPreviewDto>(),
                IsLikedByCurrentUser: false,
                IsRepostedByCurrentUser: false,
                IsEdited: false,
                ModerationInfo: null,
                VideoMetadata: null,
                MediaItems: null,
                ReactionCounts: null,
                CurrentUserReaction: null,
                TotalReactionCount: 0,
                PostType: PostType.Post,
                RepostedPost: null
            )
        };

        _mockTrendingService.Setup(s => s.GetPersonalizedTrendingPostsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(mockPosts);

        // Act
        var result = await _service.GetPersonalizedRecommendationsAsync(1, "posts", 10);

        // Assert
        Assert.NotNull(result);
        var recommendations = result.ToList();
        Assert.NotEmpty(recommendations);
        Assert.All(recommendations, r => Assert.True(r.RecommendationScore >= 0));
        Assert.All(recommendations, r => Assert.Equal("post", r.ContentType));
    }

    [Fact]
    public async Task CalculateUserSimilarityAsync_ShouldReturnValidScore()
    {
        // Arrange
        await SeedTestDataAsync();
        await SeedInteractionDataAsync(1);
        await SeedInteractionDataAsync(2);

        // Create profiles for both users
        await _service.UpdateUserProfileAsync(1, forceRebuild: true);
        await _service.UpdateUserProfileAsync(2, forceRebuild: true);

        // Act
        var similarity = await _service.CalculateUserSimilarityAsync(1, 2);

        // Assert
        Assert.True(similarity >= 0.0f);
        Assert.True(similarity <= 1.0f);
    }

    [Fact]
    public async Task FindSimilarUsersAsync_ShouldReturnSimilarUsers()
    {
        // Arrange
        await SeedTestDataAsync();
        await SeedInteractionDataAsync(1);
        await SeedInteractionDataAsync(2);

        // Create profiles
        await _service.UpdateUserProfileAsync(1, forceRebuild: true);
        await _service.UpdateUserProfileAsync(2, forceRebuild: true);

        // Act
        var result = await _service.FindSimilarUsersAsync(1, limit: 5);

        // Assert
        Assert.NotNull(result);
        var similarUsers = result.ToList();
        Assert.All(similarUsers, u => Assert.True(u.SimilarityScore >= 0));
        Assert.All(similarUsers, u => Assert.NotNull(u.SimilarUser));
        Assert.All(similarUsers, u => Assert.NotNull(u.SimilarityReason));
    }

    [Fact]
    public async Task GetPersonalizationInsightsAsync_ShouldReturnInsights()
    {
        // Arrange
        await SeedTestDataAsync();
        await SeedInteractionDataAsync(1);
        await _service.UpdateUserProfileAsync(1, forceRebuild: true);

        // Act
        var result = await _service.GetPersonalizationInsightsAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.NotNull(result.TopInterests);
        Assert.NotNull(result.ContentPreferences);
        Assert.NotNull(result.EngagementPatterns);
        Assert.NotNull(result.SimilarUsers);
        Assert.NotNull(result.Stats);
        Assert.NotNull(result.RecommendationTips);
        Assert.True(result.GeneratedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetPersonalizedFeedAsync_ShouldReturnPersonalizedContent()
    {
        // Arrange
        await SeedTestDataAsync();
        await SeedInteractionDataAsync(1);

        // Setup mocks
        _mockTrendingService.Setup(s => s.GetPersonalizedTrendingPostsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<PostDto>());

        _mockTopicService.Setup(s => s.GetTopicRecommendationsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<TopicRecommendationDto>());

        // Act
        var result = await _service.GetPersonalizedFeedAsync(1);

        // Assert
        Assert.NotNull(result);
        var feedItems = result.ToList();
        Assert.All(feedItems, item => Assert.True(item.RecommendationScore >= 0));
        Assert.All(feedItems, item => Assert.NotNull(item.Content));
        Assert.All(feedItems, item => Assert.NotNull(item.ContentType));
    }

    [Fact]
    public async Task GetPersonalizedSearchAsync_ShouldReturnSearchResults()
    {
        // Arrange
        await SeedTestDataAsync();
        await SeedInteractionDataAsync(1);
        var query = "technology";

        // Act
        var result = await _service.GetPersonalizedSearchAsync(1, query, new[] { "posts", "users" }, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(query, result.Query);
        Assert.NotNull(result.Results);
        Assert.NotNull(result.QueryExpansion);
        Assert.True(result.PersonalizationStrength >= 0);
        Assert.True(result.TotalResults >= 0);
        Assert.True(result.SearchedAt <= DateTime.UtcNow);
    }

    private async Task SeedTestDataAsync()
    {
        // Create test users
        var users = new[]
        {
            new User { Id = 1, Username = "user1", Email = "user1@test.com", TrustScore = 0.8f, Status = UserStatus.Active },
            new User { Id = 2, Username = "user2", Email = "user2@test.com", TrustScore = 0.7f, Status = UserStatus.Active }
        };
        _context.Users.AddRange(users);

        // Create test tags
        var tags = new[]
        {
            new Tag { Id = 1, Name = "technology", PostCount = 100 },
            new Tag { Id = 2, Name = "programming", PostCount = 80 },
            new Tag { Id = 3, Name = "ai", PostCount = 60 }
        };
        _context.Tags.AddRange(tags);

        // Create test posts
        var posts = new[]
        {
            new Post { Id = 1, UserId = 1, Content = "Test post about technology", PostType = PostType.Post, Privacy = PostPrivacy.Public },
            new Post { Id = 2, UserId = 2, Content = "Another tech post", PostType = PostType.Post, Privacy = PostPrivacy.Public }
        };
        _context.Posts.AddRange(posts);

        // Create post tags
        var postTags = new[]
        {
            new PostTag { PostId = 1, TagId = 1 },
            new PostTag { PostId = 1, TagId = 2 },
            new PostTag { PostId = 2, TagId = 1 }
        };
        _context.PostTags.AddRange(postTags);

        await _context.SaveChangesAsync();
    }

    private async Task SeedInteractionDataAsync(int userId)
    {
        var interactions = new[]
        {
            new UserInteractionEvent
            {
                UserId = userId,
                InteractionType = "like",
                TargetEntityType = "post",
                TargetEntityId = 1,
                InteractionStrength = 1.0f,
                IsImplicit = false,
                Sentiment = 0.8f,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new UserInteractionEvent
            {
                UserId = userId,
                InteractionType = "view",
                TargetEntityType = "post",
                TargetEntityId = 2,
                InteractionStrength = 0.3f,
                DurationMs = 5000,
                IsImplicit = true,
                Sentiment = 0.5f,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            }
        };

        _context.UserInteractionEvents.AddRange(interactions);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}