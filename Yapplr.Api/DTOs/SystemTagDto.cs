using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class SystemTagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SystemTagCategory Category { get; set; }
    public bool IsVisibleToUsers { get; set; }
    public bool IsActive { get; set; }
    public string Color { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
