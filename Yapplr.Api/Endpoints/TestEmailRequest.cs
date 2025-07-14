namespace Yapplr.Api.Endpoints;

public record TestEmailRequest(string ToEmail, string? Subject = null, string? Message = null);