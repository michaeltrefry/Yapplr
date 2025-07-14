namespace Yapplr.Api.Endpoints;

public record TestDelayedRequest(string ToEmail, int? DelaySeconds = null);