namespace Yapplr.Api.Services.Unified;

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