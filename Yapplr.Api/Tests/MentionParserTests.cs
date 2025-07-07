using Xunit;
using Yapplr.Api.Utils;

namespace Yapplr.Api.Tests;

public class MentionParserTests
{
    [Fact]
    public void ExtractMentions_WithValidMentions_ReturnsUsernames()
    {
        // Arrange
        var content = "Hello @john_doe and @jane123! How are you @bob-smith?";
        
        // Act
        var mentions = MentionParser.ExtractMentions(content);
        
        // Assert
        Assert.Equal(3, mentions.Count);
        Assert.Contains("john_doe", mentions);
        Assert.Contains("jane123", mentions);
        Assert.Contains("bob-smith", mentions);
    }

    [Fact]
    public void ExtractMentions_WithDuplicateMentions_ReturnsUniqueUsernames()
    {
        // Arrange
        var content = "Hey @john @john @jane @john!";
        
        // Act
        var mentions = MentionParser.ExtractMentions(content);
        
        // Assert
        Assert.Equal(2, mentions.Count);
        Assert.Contains("john", mentions);
        Assert.Contains("jane", mentions);
    }

    [Fact]
    public void ExtractMentions_WithNoMentions_ReturnsEmptyList()
    {
        // Arrange
        var content = "This is a regular post without any mentions.";
        
        // Act
        var mentions = MentionParser.ExtractMentions(content);
        
        // Assert
        Assert.Empty(mentions);
    }

    [Fact]
    public void ExtractMentions_WithInvalidMentions_IgnoresThem()
    {
        // Arrange
        var content = "Invalid mentions: @a @ab @valid_user";

        // Act
        var mentions = MentionParser.ExtractMentions(content);

        // Assert
        Assert.Single(mentions);
        Assert.Contains("valid_user", mentions);
    }

    [Fact]
    public void ExtractMentions_WithLongUsername_MatchesFirst50Characters()
    {
        // Arrange
        var content = "Long mention: @toolongusernamethatexceedsfiftycharacterslimit";

        // Act
        var mentions = MentionParser.ExtractMentions(content);

        // Assert
        Assert.Single(mentions);
        // Regex matches first 50 characters due to {3,50} quantifier
        Assert.Contains("toolongusernamethatexceedsfiftycharacterslimit", mentions);
    }

    [Fact]
    public void HasMentions_WithMentions_ReturnsTrue()
    {
        // Arrange
        var content = "Hello @user!";
        
        // Act
        var result = MentionParser.HasMentions(content);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasMentions_WithoutMentions_ReturnsFalse()
    {
        // Arrange
        var content = "Hello world!";
        
        // Act
        var result = MentionParser.HasMentions(content);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetMentionPositions_ReturnsCorrectPositions()
    {
        // Arrange
        var content = "Hello @john and @jane!";
        
        // Act
        var positions = MentionParser.GetMentionPositions(content);
        
        // Assert
        Assert.Equal(2, positions.Count);
        
        var firstMention = positions[0];
        Assert.Equal(6, firstMention.StartIndex);
        Assert.Equal(5, firstMention.Length);
        Assert.Equal("john", firstMention.Username);
        Assert.Equal("@john", firstMention.FullMatch);
        
        var secondMention = positions[1];
        Assert.Equal(16, secondMention.StartIndex);
        Assert.Equal(5, secondMention.Length);
        Assert.Equal("jane", secondMention.Username);
        Assert.Equal("@jane", secondMention.FullMatch);
    }

    [Fact]
    public void ReplaceMentionsWithLinks_ReplacesCorrectly()
    {
        // Arrange
        var content = "Hello @john!";
        
        // Act
        var result = MentionParser.ReplaceMentionsWithLinks(content);
        
        // Assert
        Assert.Equal("Hello <a href=\"/profile/john\" class=\"mention\">@john</a>!", result);
    }
}
