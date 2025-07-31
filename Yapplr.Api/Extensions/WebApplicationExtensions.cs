using Microsoft.EntityFrameworkCore;
using Prometheus;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Payment;
using Yapplr.Api.Endpoints;
using Yapplr.Api.Middleware;
using Yapplr.Api.Configuration;
using Yapplr.Api.Common;
using Yapplr.Api.Services.Notifications;

namespace Yapplr.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureYapplrPipeline(this WebApplication app)
    {
        // Configure forwarded headers for reverse proxy support (must be first)
        app.UseForwardedHeaders();

        // Add logging context middleware (should be early in pipeline)
        app.UseLoggingContext();

        // Add global error handling middleware (should be early in pipeline)
        app.UseErrorHandling();

        // Configure Swagger/OpenAPI - only in development and staging
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Yapplr API v1");
                c.RoutePrefix = "swagger";
            });
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        
        // Use appropriate CORS policy based on environment
        if (app.Environment.IsDevelopment())
        {
            app.UseCors("AllowAll");
        }
        else
        {
            app.UseCors("AllowFrontend");
        }
        // Add Prometheus metrics middleware
        app.UseHttpMetrics();

        app.UseAuthentication();
        app.UseMiddleware<UserActivityMiddleware>();
        app.UseMiddleware<ApiRateLimitMiddleware>();
        app.UseMiddleware<SubscriptionSystemMiddleware>();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapYapplrEndpoints(this WebApplication app)
    {
        // Map API endpoints
        app.MapAuthEndpoints();
        app.MapUserEndpoints();
        app.MapPostEndpoints();
        app.MapGroupEndpoints();
        app.MapBlockEndpoints();
        app.MapImageEndpoints();
        app.MapVideoEndpoints();
        app.MapMultipleFileUploadEndpoints();
        app.MapMessageEndpoints();
        app.MapUserPreferencesEndpoints();
        app.MapNotificationEndpoints();
        app.MapNotificationPreferencesEndpoints();
        app.MapSecurityEndpoints();
        app.MapMetricsEndpoints();
        app.MapNotificationConfigurationEndpoints();
        app.MapTagEndpoints();
        app.MapTrendingEndpoints();
        app.MapUserReportEndpoints();
        app.MapContentEndpoints();
        app.MapGifEndpoints();
        app.MapAdminEndpoints();
        app.MapUploadSettingsEndpoints();
        app.MapCorsConfigurationEndpoints();
        app.MapSubscriptionEndpoints();
        app.MapPaymentEndpoints();
        app.MapPaymentAdminEndpoints();

        // Map traditional controllers
        app.MapControllers();

        // Only register test endpoints in development environment
        if (app.Environment.IsDevelopment())
        {
            app.MapCqrsTestEndpoints();
        }

        // Map Prometheus metrics endpoint
        app.MapMetrics();

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
            try
            {
                logger.LogInformation("🗄️ Running database migrations at startup...");

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

                // Seed subscription tiers
                logger.LogInformation("💳 Seeding subscription tiers...");
                var subscriptionSeedService = scope.ServiceProvider.GetRequiredService<SubscriptionSeedService>();
                await subscriptionSeedService.SeedSubscriptionDataAsync();

                // Initialize system configurations
                logger.LogInformation("⚙️ Initializing system configurations...");
                var systemConfigService = scope.ServiceProvider.GetRequiredService<ISystemConfigurationService>();
                await systemConfigService.InitializeDefaultConfigurationsAsync();

                // Initialize payment configurations
                logger.LogInformation("💳 Initializing payment configurations...");
                var paymentSeedService = scope.ServiceProvider.GetRequiredService<PaymentConfigurationSeedingService>();
                await paymentSeedService.SeedDefaultConfigurationAsync();

                // Seed test data for non-production environments
                var stagingSeedService = scope.ServiceProvider.GetService<StagingSeedService>();
                if (stagingSeedService != null)
                {
                    logger.LogInformation("🌱 Seeding test data for non-production environment...");
                    await stagingSeedService.SeedStagingDataAsync();
                }
                    
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
