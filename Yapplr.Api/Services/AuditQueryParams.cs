namespace Yapplr.Api.Services;

/// <summary>
/// Audit query parameters
/// </summary>
public class AuditQueryParams
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? UserId { get; set; }
    public string? EventType { get; set; }
    public string? NotificationType { get; set; }
    public bool? Success { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}