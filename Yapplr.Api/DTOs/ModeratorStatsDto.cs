using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class ModeratorStatsDto
{
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int TotalActions { get; set; }
    public int UserActions { get; set; }
    public int ContentActions { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastActive { get; set; }
}
