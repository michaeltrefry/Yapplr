namespace Yapplr.Api.DTOs;

public class CreateContentPageVersionDto
{
    public string Content { get; set; } = string.Empty;
    public string? ChangeNotes { get; set; }
}
