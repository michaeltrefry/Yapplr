using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class UpdateSystemTagDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public SystemTagCategory? Category { get; set; }
    public bool? IsVisibleToUsers { get; set; }
    public bool? IsActive { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int? SortOrder { get; set; }
}
