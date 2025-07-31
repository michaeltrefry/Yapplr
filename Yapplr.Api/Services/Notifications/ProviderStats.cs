namespace Yapplr.Api.Services.Notifications;

internal class ProviderStats
{
    public long TotalAttempts;
    public long SuccessfulAttempts;
    public long FailedAttempts;
    public long TotalLatencyMs;
    public int ConsecutiveFailures;
    public DateTime? LastSuccessfulDelivery;
    public DateTime? LastFailedDelivery;
    public string? LastError;
}