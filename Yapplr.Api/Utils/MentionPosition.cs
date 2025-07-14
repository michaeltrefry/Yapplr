namespace Yapplr.Api.Utils;

public class MentionPosition
{
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullMatch { get; set; } = string.Empty;
}
