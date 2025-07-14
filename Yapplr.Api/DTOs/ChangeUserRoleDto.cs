using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class ChangeUserRoleDto
{
    public UserRole Role { get; set; }
    public string Reason { get; set; } = string.Empty;
}
