using System.Text.RegularExpressions;

namespace Yapplr.Api.Utils;

public static class MentionParser
{
    // Regex pattern to match @username mentions
    // Matches @ followed by 3-50 alphanumeric characters, underscores, or hyphens
    // Uses word boundary to ensure we don't match partial usernames
    private static readonly Regex MentionRegex = new(@"@([a-zA-Z0-9_-]{3,50})\b", RegexOptions.Compiled);
    
    /// <summary>
    /// Extracts all unique usernames mentioned in the given content
    /// </summary>
    /// <param name="content">The content to parse for mentions</param>
    /// <returns>A list of unique usernames that were mentioned (without the @ symbol)</returns>
    public static List<string> ExtractMentions(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<string>();
        
        var matches = MentionRegex.Matches(content);
        var usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                var username = match.Groups[1].Value;
                usernames.Add(username);
            }
        }
        
        return usernames.ToList();
    }
    
    /// <summary>
    /// Checks if the given content contains any mentions
    /// </summary>
    /// <param name="content">The content to check</param>
    /// <returns>True if the content contains at least one mention</returns>
    public static bool HasMentions(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;
        
        return MentionRegex.IsMatch(content);
    }
    
    /// <summary>
    /// Replaces @username mentions in content with clickable links
    /// </summary>
    /// <param name="content">The content to process</param>
    /// <param name="linkTemplate">Template for the link (e.g., "/profile/{0}" where {0} will be replaced with username)</param>
    /// <returns>Content with mentions replaced by links</returns>
    public static string ReplaceMentionsWithLinks(string content, string linkTemplate = "/profile/{0}")
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;
        
        return MentionRegex.Replace(content, match =>
        {
            var username = match.Groups[1].Value;
            var link = string.Format(linkTemplate, username);
            return $"<a href=\"{link}\" class=\"mention\">@{username}</a>";
        });
    }
    
    /// <summary>
    /// Gets all mention positions in the content for highlighting purposes
    /// </summary>
    /// <param name="content">The content to analyze</param>
    /// <returns>List of mention positions with start index, length, and username</returns>
    public static List<MentionPosition> GetMentionPositions(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<MentionPosition>();
        
        var positions = new List<MentionPosition>();
        var matches = MentionRegex.Matches(content);
        
        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                positions.Add(new MentionPosition
                {
                    StartIndex = match.Index,
                    Length = match.Length,
                    Username = match.Groups[1].Value,
                    FullMatch = match.Value
                });
            }
        }
        
        return positions;
    }
}

public class MentionPosition
{
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullMatch { get; set; } = string.Empty;
}
