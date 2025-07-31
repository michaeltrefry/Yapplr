namespace Yapplr.Api.Services.Notifications;

internal class CircuitBreakerState
{
    public CircuitBreakerStateEnum State { get; set; } = CircuitBreakerStateEnum.Closed;
    public int ConsecutiveFailures { get; set; }
    public DateTime LastFailureTime { get; set; }
}