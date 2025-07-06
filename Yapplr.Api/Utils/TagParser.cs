using System.Text.RegularExpressions;

namespace Yapplr.Api.Utils;

public static class TagParser
{
    // Regex pattern to match #hashtag tags
    // Matches # followed by 1-50 alphanumeric characters, underscores, or hyphens
    // Uses word boundary to ensure we don't match partial tags
    // Excludes tags that start with numbers to follow common hashtag conventions
    private static readonly Regex TagRegex = new(@"#([a-zA-Z][a-zA-Z0-9_-]{0,49})\b", RegexOptions.Compiled);
    
    /// <summary>
    /// Extracts all unique hashtags from the given content
    /// </summary>
    /// <param name="content">The content to parse for hashtags</param>
    /// <returns>A list of unique hashtag names that were found (without the # symbol)</returns>
    public static List<string> ExtractTags(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<string>();
        
        var matches = TagRegex.Matches(content);
        var tagNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                var tagName = match.Groups[1].Value.ToLowerInvariant(); // Normalize to lowercase
                tagNames.Add(tagName);
            }
        }
        
        return tagNames.ToList();
    }
    
    /// <summary>
    /// Checks if the given content contains any hashtags
    /// </summary>
    /// <param name="content">The content to check</param>
    /// <returns>True if the content contains at least one hashtag</returns>
    public static bool HasTags(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;
        
        return TagRegex.IsMatch(content);
    }
    
    /// <summary>
    /// Replaces #hashtag tags in content with clickable links
    /// </summary>
    /// <param name="content">The content to process</param>
    /// <param name="linkTemplate">Template for the link (e.g., "/hashtag/{0}" where {0} will be replaced with tag name)</param>
    /// <returns>Content with hashtags replaced by links</returns>
    public static string ReplaceTagsWithLinks(string content, string linkTemplate = "/hashtag/{0}")
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;
        
        return TagRegex.Replace(content, match =>
        {
            var tagName = match.Groups[1].Value.ToLowerInvariant();
            var link = string.Format(linkTemplate, tagName);
            return $"<a href=\"{link}\" class=\"hashtag\">#{tagName}</a>";
        });
    }
    
    /// <summary>
    /// Gets all hashtag positions in the content for highlighting purposes
    /// </summary>
    /// <param name="content">The content to analyze</param>
    /// <returns>List of hashtag positions with start index, length, and tag name</returns>
    public static List<TagPosition> GetTagPositions(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<TagPosition>();
        
        var positions = new List<TagPosition>();
        var matches = TagRegex.Matches(content);
        
        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                positions.Add(new TagPosition
                {
                    StartIndex = match.Index,
                    Length = match.Length,
                    TagName = match.Groups[1].Value.ToLowerInvariant(),
                    FullMatch = match.Value
                });
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// Validates if a tag name is valid according to our rules
    /// </summary>
    /// <param name="tagName">The tag name to validate (without #)</param>
    /// <returns>True if the tag name is valid</returns>
    public static bool IsValidTagName(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return false;
        
        // Check length (1-50 characters)
        if (tagName.Length < 1 || tagName.Length > 50)
            return false;
        
        // Check if it matches our pattern (starts with letter, contains only letters, numbers, underscores, hyphens)
        return Regex.IsMatch(tagName, @"^[a-zA-Z][a-zA-Z0-9_-]*$");
    }
}

public class TagPosition
{
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string FullMatch { get; set; } = string.Empty;
}
