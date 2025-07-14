namespace Yapplr.Api.DTOs;

public class SystemHealthDto
{
    public double UptimePercentage { get; set; }
    public int ActiveUsers24h { get; set; }
    public int ErrorCount24h { get; set; }
    public double AverageResponseTime { get; set; }
    public int DatabaseConnections { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public List<SystemAlertDto> Alerts { get; set; } = new();
}
