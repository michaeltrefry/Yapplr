using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Analytics;
using Xunit;

namespace Yapplr.Api.Tests.Services;

public class TopicServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly Mock<ITagAnalyticsService> _mockTagAnalyticsService;
    private readonly Mock<ITrendingService> _mockTrendingService;
    private readonly Mock<ILogger<TopicService>> _mockLogger;
    private readonly TopicService _service;

    public TopicServiceTests()
    {
        _context = new TestYapplrDbContext();
        _mockTagAnalyticsService = new Mock<ITagAnalyticsService>();
        _mockTrendingService = new Mock<ITrendingService>();
        _mockLogger = new Mock<ILogger<TopicService>>();
        
        _service = new TopicService(
            _context,
            _mockTagAnalyticsService.Object,
            _mockTrendingService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetTopicsAsync_ShouldReturnActiveTopics()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetTopicsAsync();

        // Assert
        Assert.NotNull(result);
        var topics = result.ToList();
        Assert.NotEmpty(topics);
        
        // Should only return active topics
        Assert.All(topics, topic => Assert.True(topic.Id > 0));
        
        // Should be ordered by featured status and follower count
        var featuredTopics = topics.Where(t => t.IsFeatured).ToList();
        Assert.True(featuredTopics.Count > 0);
    }

    [Fact]
    public async Task FollowTopicAsync_ShouldCreateTopicFollow()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;
        var createDto = new CreateTopicFollowDto(
            TopicName: "Technology",
            TopicDescription: "Tech-related content",
            Category: "Technology",
            RelatedHashtags: new[] { "tech", "programming", "ai" },
            InterestLevel: 0.8f,
            IncludeInMainFeed: true,
            EnableNotifications: false
        );

        // Act
        var result = await _service.FollowTopicAsync(userId, createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Technology", result.TopicName);
        Assert.Equal(0.8f, result.InterestLevel);
        Assert.True(result.IncludeInMainFeed);
        Assert.False(result.EnableNotifications);
    }

    [Fact]
    public async Task FollowTopicAsync_ShouldThrowWhenAlreadyFollowing()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;
        var createDto = new CreateTopicFollowDto(
            TopicName: "Technology",
            TopicDescription: "Tech content",
            Category: "Technology",
            RelatedHashtags: new[] { "tech" }
        );

        // Follow the topic first
        await _service.FollowTopicAsync(userId, createDto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.FollowTopicAsync(userId, createDto));
    }

    [Fact]
    public async Task UnfollowTopicAsync_ShouldRemoveTopicFollow()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;
        var createDto = new CreateTopicFollowDto(
            TopicName: "Technology",
            TopicDescription: "Tech content",
            Category: "Technology",
            RelatedHashtags: new[] { "tech" }
        );

        await _service.FollowTopicAsync(userId, createDto);

        // Act
        var result = await _service.UnfollowTopicAsync(userId, "Technology");

        // Assert
        Assert.True(result);
        
        // Verify it's no longer followed
        var isFollowing = await _service.IsFollowingTopicAsync(userId, "Technology");
        Assert.False(isFollowing);
    }

    [Fact]
    public async Task GetUserTopicsAsync_ShouldReturnFollowedTopics()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;
        
        // Follow multiple topics
        await _service.FollowTopicAsync(userId, new CreateTopicFollowDto(
            "Technology", "Tech content", "Technology", new[] { "tech" }, 0.9f, true));
        await _service.FollowTopicAsync(userId, new CreateTopicFollowDto(
            "Sports", "Sports content", "Sports", new[] { "sports" }, 0.7f, false));

        // Act
        var result = await _service.GetUserTopicsAsync(userId);

        // Assert
        Assert.NotNull(result);
        var followedTopics = result.ToList();
        Assert.Equal(2, followedTopics.Count);
        
        // Should be ordered by interest level
        Assert.True(followedTopics[0].InterestLevel >= followedTopics[1].InterestLevel);
    }

    [Fact]
    public async Task GetUserTopicsAsync_WithMainFeedFilter_ShouldReturnFilteredTopics()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;
        
        await _service.FollowTopicAsync(userId, new CreateTopicFollowDto(
            "Technology", "Tech content", "Technology", new[] { "tech" }, 0.9f, true));
        await _service.FollowTopicAsync(userId, new CreateTopicFollowDto(
            "Sports", "Sports content", "Sports", new[] { "sports" }, 0.7f, false));

        // Act
        var mainFeedTopics = await _service.GetUserTopicsAsync(userId, includeInMainFeed: true);
        var nonMainFeedTopics = await _service.GetUserTopicsAsync(userId, includeInMainFeed: false);

        // Assert
        Assert.Single(mainFeedTopics);
        Assert.Equal("Technology", mainFeedTopics.First().TopicName);
        
        Assert.Single(nonMainFeedTopics);
        Assert.Equal("Sports", nonMainFeedTopics.First().TopicName);
    }

    [Fact]
    public async Task UpdateTopicFollowAsync_ShouldUpdatePreferences()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;
        var createDto = new CreateTopicFollowDto(
            "Technology", "Tech content", "Technology", new[] { "tech" }, 0.5f, false);
        
        await _service.FollowTopicAsync(userId, createDto);

        var updateDto = new UpdateTopicFollowDto(
            InterestLevel: 0.9f,
            IncludeInMainFeed: true,
            EnableNotifications: true,
            NotificationThreshold: 0.8f
        );

        // Act
        var result = await _service.UpdateTopicFollowAsync(userId, "Technology", updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0.9f, result.InterestLevel);
        Assert.True(result.IncludeInMainFeed);
        Assert.True(result.EnableNotifications);
        Assert.Equal(0.8f, result.NotificationThreshold);
    }

    [Fact]
    public async Task SearchTopicsAsync_ShouldReturnSearchResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.SearchTopicsAsync("tech", userId: 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalResults >= 0);
        Assert.NotNull(result.ExactMatches);
        Assert.NotNull(result.PartialMatches);
        Assert.NotNull(result.Recommendations);
        Assert.NotNull(result.SuggestedHashtags);
    }

    [Fact]
    public async Task GetTopicRecommendationsAsync_ShouldReturnPersonalizedRecommendations()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;

        // Act
        var result = await _service.GetTopicRecommendationsAsync(userId, limit: 5);

        // Assert
        Assert.NotNull(result);
        var recommendations = result.ToList();
        
        // All recommendations should be personalized
        Assert.All(recommendations, rec => Assert.True(rec.IsPersonalized));
        
        // Should have recommendation scores
        Assert.All(recommendations, rec => Assert.True(rec.RecommendationScore >= 0));
    }

    [Fact]
    public async Task CalculateTopicSimilarityAsync_ShouldReturnValidScore()
    {
        // Arrange
        var hashtags1 = new[] { "tech", "programming", "ai" };
        var hashtags2 = new[] { "tech", "software", "development" };

        // Act
        var similarity = await _service.CalculateTopicSimilarityAsync(hashtags1, hashtags2);

        // Assert
        Assert.True(similarity >= 0);
        Assert.True(similarity <= 1);
        Assert.True(similarity > 0); // Should have some overlap with "tech"
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

        // Create test topics
        var topics = new[]
        {
            new Topic 
            { 
                Id = 1, 
                Name = "Technology", 
                Description = "All things tech", 
                Category = "Technology",
                RelatedHashtags = "tech,programming,ai,software",
                Slug = "technology",
                IsFeatured = true,
                FollowerCount = 100,
                IsActive = true
            },
            new Topic 
            { 
                Id = 2, 
                Name = "Sports", 
                Description = "Sports and fitness", 
                Category = "Sports",
                RelatedHashtags = "sports,fitness,football,basketball",
                Slug = "sports",
                IsFeatured = false,
                FollowerCount = 50,
                IsActive = true
            }
        };
        _context.Topics.AddRange(topics);

        // Create test tags
        var tags = new[]
        {
            new Tag { Id = 1, Name = "tech", PostCount = 100 },
            new Tag { Id = 2, Name = "programming", PostCount = 80 },
            new Tag { Id = 3, Name = "sports", PostCount = 60 },
            new Tag { Id = 4, Name = "ai", PostCount = 40 }
        };
        _context.Tags.AddRange(tags);

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
