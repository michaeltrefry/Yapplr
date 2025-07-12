using Microsoft.Extensions.Options;
using Yapplr.Api.Configuration;

namespace Yapplr.Api.Endpoints;

public static class CorsConfigurationEndpoints
{
    public static void MapCorsConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cors-config")
            .WithTags("CORS Configuration")
            .RequireAuthorization("Admin");

        group.MapGet("/", GetCorsConfiguration)
            .WithName("GetCorsConfiguration")
            .WithSummary("Get current CORS configuration")
            .WithDescription("Returns the current CORS configuration for all policies (Admin only)");
    }

    private static IResult GetCorsConfiguration(
        IOptions<CorsConfiguration> corsOptions)
    {
        var config = corsOptions.Value;

        var response = new
        {
            AllowFrontend = new
            {
                AllowedOrigins = config.AllowFrontend.AllowedOrigins,
                AllowedMethods = config.AllowFrontend.AllowedMethods,
                AllowedHeaders = config.AllowFrontend.AllowedHeaders,
                AllowCredentials = config.AllowFrontend.AllowCredentials,
                AllowAnyOrigin = config.AllowFrontend.AllowAnyOrigin,
                AllowAnyMethod = config.AllowFrontend.AllowAnyMethod,
                AllowAnyHeader = config.AllowFrontend.AllowAnyHeader
            },
            AllowSignalR = new
            {
                AllowedOrigins = config.AllowSignalR.AllowedOrigins,
                AllowedMethods = config.AllowSignalR.AllowedMethods,
                AllowedHeaders = config.AllowSignalR.AllowedHeaders,
                AllowCredentials = config.AllowSignalR.AllowCredentials,
                AllowAnyOrigin = config.AllowSignalR.AllowAnyOrigin,
                AllowAnyMethod = config.AllowSignalR.AllowAnyMethod,
                AllowAnyHeader = config.AllowSignalR.AllowAnyHeader
            },
            AllowAll = new
            {
                AllowedOrigins = config.AllowAll.AllowedOrigins,
                AllowedMethods = config.AllowAll.AllowedMethods,
                AllowedHeaders = config.AllowAll.AllowedHeaders,
                AllowCredentials = config.AllowAll.AllowCredentials,
                AllowAnyOrigin = config.AllowAll.AllowAnyOrigin,
                AllowAnyMethod = config.AllowAll.AllowAnyMethod,
                AllowAnyHeader = config.AllowAll.AllowAnyHeader
            }
        };

        return Results.Ok(response);
    }
}
