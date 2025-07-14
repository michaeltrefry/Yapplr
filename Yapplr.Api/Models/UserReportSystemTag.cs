namespace Yapplr.Api.Models;

public class UserReportSystemTag
{
    public int Id { get; set; }
    
    public int UserReportId { get; set; }
    public UserReport UserReport { get; set; } = null!;
    
    public int SystemTagId { get; set; }
    public SystemTag SystemTag { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
