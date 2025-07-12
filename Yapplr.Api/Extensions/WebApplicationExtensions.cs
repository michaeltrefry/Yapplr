using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using Yapplr.Api.Endpoints;
using Yapplr.Api.Middleware;
using Yapplr.Api.Hubs;
using Yapplr.Api.Configuration;
using Yapplr.Api.Models;

namespace Yapplr.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureYapplrPipeline(this WebApplication app)
    {
        // Configure the HTTP request pipeline
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Yapplr API v1");
            c.RoutePrefix = "swagger";
        });

        app.MapOpenApi();
        app.UseHttpsRedirection();
        
        // Use AllowSignalR CORS policy for development (supports credentials for SignalR)
        app.UseCors("AllowSignalR");
        app.UseAuthentication();
        app.UseMiddleware<UserActivityMiddleware>();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapYapplrEndpoints(this WebApplication app)
    {
        // Map API endpoints
        app.MapAuthEndpoints();
        app.MapUserEndpoints();
        app.MapPostEndpoints();
        app.MapBlockEndpoints();
        app.MapImageEndpoints();
        app.MapMessageEndpoints();
        app.MapUserPreferencesEndpoints();
        app.MapNotificationEndpoints();
        app.MapNotificationPreferencesEndpoints();
        app.MapUXEnhancementEndpoints();
        app.MapSecurityEndpoints();
        app.MapMetricsEndpoints();
        app.MapNotificationConfigurationEndpoints();
        app.MapTagEndpoints();
        app.MapUserReportEndpoints();
        app.MapContentEndpoints();
        app.MapAdminEndpoints();
        app.MapCorsConfigurationEndpoints();
        app.MapCqrsTestEndpoints();

        return app;
    }

    public static WebApplication MapYapplrHubs(this WebApplication app, IConfiguration configuration)
    {
        var notificationConfig = configuration
            .GetSection(NotificationProvidersConfiguration.SectionName)
            .Get<NotificationProvidersConfiguration>() ?? new NotificationProvidersConfiguration();

        // Map SignalR hub (if enabled)
        if (notificationConfig.SignalR.Enabled)
        {
            app.MapHub<NotificationHub>("/notificationHub");
        }

        return app;
    }

    public static WebApplication MapHealthCheck(this WebApplication app)
    {
        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

        return app;
    }

    public static async Task<WebApplication> RunDatabaseMigrationsAsync(this WebApplication app)
    {
        // Run database migrations at startup with retry logic
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var retryService = scope.ServiceProvider.GetRequiredService<ISmartRetryService>();

            try
            {
                logger.LogInformation("🗄️ Running database migrations at startup...");
                
                await retryService.ExecuteWithRetryAsync(
                    async () => {
                        // Test database connection first
                        await context.Database.CanConnectAsync();

                        // Run migrations
                        await context.Database.MigrateAsync();

                        // Seed essential users
                        logger.LogInformation("👤 Seeding essential users...");
                        var essentialUserSeedService = scope.ServiceProvider.GetRequiredService<EssentialUserSeedService>();
                        await essentialUserSeedService.SeedEssentialUsersAsync();

                        // Seed system tags
                        logger.LogInformation("🏷️ Seeding system tags...");
                        var systemTagSeedService = scope.ServiceProvider.GetRequiredService<SystemTagSeedService>();
                        await systemTagSeedService.SeedDefaultSystemTagsAsync();

                        // Seed content pages
                        logger.LogInformation("📄 Seeding content pages...");
                        var contentSeedService = scope.ServiceProvider.GetRequiredService<ContentSeedService>();
                        await contentSeedService.SeedContentPagesAsync();

                        // Seed test data for non-production environments
                        var stagingSeedService = scope.ServiceProvider.GetService<StagingSeedService>();
                        if (stagingSeedService != null)
                        {
                            logger.LogInformation("🌱 Seeding test data for non-production environment...");
                            await stagingSeedService.SeedStagingDataAsync();
                        }

                        return true;
                    },
                    "DatabaseMigrationAndSeeding");
                    
                logger.LogInformation("✅ Database migrations and all seeding operations completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to run database migrations or seeding at startup after multiple attempts");
                throw; // This will prevent the application from starting if migrations fail
            }
        }

        return app;
    }
}
