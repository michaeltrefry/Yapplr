namespace Yapplr.Api.Endpoints;

public record TestNotificationRequest(int? TargetUserId = null, string? Title = null, string? Message = null);