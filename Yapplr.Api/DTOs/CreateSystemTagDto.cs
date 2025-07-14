using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class CreateSystemTagDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SystemTagCategory Category { get; set; }
    public bool IsVisibleToUsers { get; set; } = false;
    public string Color { get; set; } = "#6B7280";
    public string? Icon { get; set; }
    public int SortOrder { get; set; } = 0;
}
