using Xunit;
using FluentAssertions;
using Yapplr.Api.Utils;

namespace Yapplr.Api.Tests;

public class TagParserTests
{
    [Fact]
    public void ExtractTags_WithValidTags_ReturnsTagNames()
    {
        // Arrange
        var content = "Check out #technology and #programming! Also #webdev";
        
        // Act
        var tags = TagParser.ExtractTags(content);
        
        // Assert
        tags.Should().HaveCount(3);
        tags.Should().Contain("technology");
        tags.Should().Contain("programming");
        tags.Should().Contain("webdev");
    }

    [Fact]
    public void ExtractTags_WithDuplicateTags_ReturnsUniqueTagNames()
    {
        // Arrange
        var content = "Love #coding and #coding! Also #CODING and #Coding";
        
        // Act
        var tags = TagParser.ExtractTags(content);
        
        // Assert
        tags.Should().HaveCount(1);
        tags.Should().Contain("coding");
    }

    [Fact]
    public void ExtractTags_WithNoTags_ReturnsEmptyList()
    {
        // Arrange
        var content = "This is a regular post without any hashtags.";
        
        // Act
        var tags = TagParser.ExtractTags(content);
        
        // Assert
        tags.Should().BeEmpty();
    }

    [Fact]
    public void ExtractTags_WithNullOrEmptyContent_ReturnsEmptyList()
    {
        // Act & Assert
        TagParser.ExtractTags(null).Should().BeEmpty();
        TagParser.ExtractTags("").Should().BeEmpty();
        TagParser.ExtractTags("   ").Should().BeEmpty();
    }

    [Fact]
    public void ExtractTags_WithInvalidTags_IgnoresThem()
    {
        // Arrange
        var content = "Invalid tags: #1invalid #_invalid #-invalid #valid_tag";
        
        // Act
        var tags = TagParser.ExtractTags(content);
        
        // Assert
        tags.Should().HaveCount(1);
        tags.Should().Contain("valid_tag");
    }

    [Fact]
    public void ExtractTags_WithTagsContainingUnderscoresAndHyphens_ReturnsValidTags()
    {
        // Arrange
        var content = "Tags with special chars: #web_development #front-end #back_end-dev";
        
        // Act
        var tags = TagParser.ExtractTags(content);
        
        // Assert
        tags.Should().HaveCount(3);
        tags.Should().Contain("web_development");
        tags.Should().Contain("front-end");
        tags.Should().Contain("back_end-dev");
    }

    [Fact]
    public void ExtractTags_WithLongTags_IgnoresTagsOver50Characters()
    {
        // Arrange
        var longTag = "verylongtagthatexceedsfiftycharacterslimitandshouldbetrun"; // 58 chars
        var content = $"Long tag: #{longTag} and #validtag";

        // Act
        var tags = TagParser.ExtractTags(content);

        // Assert
        tags.Should().HaveCount(1);
        tags.Should().Contain("validtag");
        tags.Should().NotContain(longTag);
    }

    [Fact]
    public void ExtractTags_WithTagsAtWordBoundaries_ExtractsCorrectly()
    {
        // Arrange
        var content = "Start #tag1 middle #tag2, end #tag3.";
        
        // Act
        var tags = TagParser.ExtractTags(content);
        
        // Assert
        tags.Should().HaveCount(3);
        tags.Should().Contain("tag1");
        tags.Should().Contain("tag2");
        tags.Should().Contain("tag3");
    }

    [Fact]
    public void HasTags_WithTags_ReturnsTrue()
    {
        // Arrange
        var content = "This has a #hashtag";
        
        // Act
        var result = TagParser.HasTags(content);
        
        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasTags_WithoutTags_ReturnsFalse()
    {
        // Arrange
        var content = "This has no hashtags";
        
        // Act
        var result = TagParser.HasTags(content);
        
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasTags_WithNullOrEmptyContent_ReturnsFalse()
    {
        // Act & Assert
        TagParser.HasTags(null).Should().BeFalse();
        TagParser.HasTags("").Should().BeFalse();
        TagParser.HasTags("   ").Should().BeFalse();
    }

    [Fact]
    public void ReplaceTagsWithLinks_WithDefaultTemplate_ReplacesCorrectly()
    {
        // Arrange
        var content = "Check out #technology!";
        
        // Act
        var result = TagParser.ReplaceTagsWithLinks(content);
        
        // Assert
        result.Should().Be("Check out <a href=\"/hashtag/technology\" class=\"hashtag\">#technology</a>!");
    }

    [Fact]
    public void ReplaceTagsWithLinks_WithCustomTemplate_ReplacesCorrectly()
    {
        // Arrange
        var content = "Check out #technology!";
        var template = "/tags/{0}";
        
        // Act
        var result = TagParser.ReplaceTagsWithLinks(content, template);
        
        // Assert
        result.Should().Be("Check out <a href=\"/tags/technology\" class=\"hashtag\">#technology</a>!");
    }

    [Fact]
    public void ReplaceTagsWithLinks_WithMultipleTags_ReplacesAll()
    {
        // Arrange
        var content = "Love #coding and #programming!";
        
        // Act
        var result = TagParser.ReplaceTagsWithLinks(content);
        
        // Assert
        result.Should().Contain("<a href=\"/hashtag/coding\" class=\"hashtag\">#coding</a>");
        result.Should().Contain("<a href=\"/hashtag/programming\" class=\"hashtag\">#programming</a>");
    }

    [Fact]
    public void ReplaceTagsWithLinks_WithNullOrEmptyContent_ReturnsOriginal()
    {
        // Act & Assert
        TagParser.ReplaceTagsWithLinks(null).Should().BeNull();
        TagParser.ReplaceTagsWithLinks("").Should().Be("");
        TagParser.ReplaceTagsWithLinks("   ").Should().Be("   ");
    }

    [Fact]
    public void GetTagPositions_WithTags_ReturnsCorrectPositions()
    {
        // Arrange
        var content = "Start #tag1 and #tag2 end";
        
        // Act
        var positions = TagParser.GetTagPositions(content);
        
        // Assert
        positions.Should().HaveCount(2);
        
        var firstTag = positions[0];
        firstTag.StartIndex.Should().Be(6);
        firstTag.Length.Should().Be(5);
        firstTag.TagName.Should().Be("tag1");
        firstTag.FullMatch.Should().Be("#tag1");
        
        var secondTag = positions[1];
        secondTag.StartIndex.Should().Be(16);
        secondTag.Length.Should().Be(5);
        secondTag.TagName.Should().Be("tag2");
        secondTag.FullMatch.Should().Be("#tag2");
    }

    [Fact]
    public void GetTagPositions_WithNoTags_ReturnsEmptyList()
    {
        // Arrange
        var content = "No hashtags here";
        
        // Act
        var positions = TagParser.GetTagPositions(content);
        
        // Assert
        positions.Should().BeEmpty();
    }

    [Fact]
    public void GetTagPositions_WithNullOrEmptyContent_ReturnsEmptyList()
    {
        // Act & Assert
        TagParser.GetTagPositions(null).Should().BeEmpty();
        TagParser.GetTagPositions("").Should().BeEmpty();
        TagParser.GetTagPositions("   ").Should().BeEmpty();
    }

    [Fact]
    public void IsValidTagName_WithValidNames_ReturnsTrue()
    {
        // Act & Assert
        TagParser.IsValidTagName("technology").Should().BeTrue();
        TagParser.IsValidTagName("web_development").Should().BeTrue();
        TagParser.IsValidTagName("front-end").Should().BeTrue();
        TagParser.IsValidTagName("a").Should().BeTrue();
        TagParser.IsValidTagName("A1B2C3").Should().BeTrue();
    }

    [Fact]
    public void IsValidTagName_WithInvalidNames_ReturnsFalse()
    {
        // Act & Assert
        TagParser.IsValidTagName("").Should().BeFalse();
        TagParser.IsValidTagName(null).Should().BeFalse();
        TagParser.IsValidTagName("   ").Should().BeFalse();
        TagParser.IsValidTagName("1invalid").Should().BeFalse();
        TagParser.IsValidTagName("_invalid").Should().BeFalse();
        TagParser.IsValidTagName("-invalid").Should().BeFalse();
        TagParser.IsValidTagName("invalid@").Should().BeFalse();
        TagParser.IsValidTagName("invalid space").Should().BeFalse();
    }

    [Fact]
    public void IsValidTagName_WithTooLongName_ReturnsFalse()
    {
        // Arrange
        var tooLongName = new string('a', 51);
        
        // Act
        var result = TagParser.IsValidTagName(tooLongName);
        
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidTagName_WithMaxLengthName_ReturnsTrue()
    {
        // Arrange
        var maxLengthName = new string('a', 50);
        
        // Act
        var result = TagParser.IsValidTagName(maxLengthName);
        
        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("#tech", "tech")]
    [InlineData("#TECH", "tech")]
    [InlineData("#Tech", "tech")]
    [InlineData("#tEcH", "tech")]
    public void ExtractTags_NormalizesToLowercase(string input, string expected)
    {
        // Act
        var tags = TagParser.ExtractTags(input);
        
        // Assert
        tags.Should().HaveCount(1);
        tags.First().Should().Be(expected);
    }

    [Fact]
    public void ExtractTags_WithSpecialCharactersAroundTags_ExtractsCorrectly()
    {
        // Arrange
        var content = "Punctuation: #tag1, #tag2; #tag3. #tag4! #tag5? (#tag6) [#tag7] {#tag8}";
        
        // Act
        var tags = TagParser.ExtractTags(content);
        
        // Assert
        tags.Should().HaveCount(8);
        tags.Should().Contain("tag1");
        tags.Should().Contain("tag2");
        tags.Should().Contain("tag3");
        tags.Should().Contain("tag4");
        tags.Should().Contain("tag5");
        tags.Should().Contain("tag6");
        tags.Should().Contain("tag7");
        tags.Should().Contain("tag8");
    }
}
