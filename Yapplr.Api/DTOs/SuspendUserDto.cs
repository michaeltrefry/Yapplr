namespace Yapplr.Api.DTOs;

public class SuspendUserDto
{
    public string Reason { get; set; } = string.Empty;
    public DateTime? SuspendedUntil { get; set; } // null for permanent suspension
}
