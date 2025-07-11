using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Amazon.SimpleEmail;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using Yapplr.Api.Hubs;
using Yapplr.Api.Configuration;
using SendGrid;
using Yapplr.Api.Models;
using Yapplr.Api.Authorization;
using MassTransit;
using Yapplr.Api.CQRS;

namespace Yapplr.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYapplrServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Add OpenAPI and Swagger
        services.AddOpenApi();
        services.AddSwaggerGen();

        // Add Entity Framework
        services.AddEntityFramework(configuration);

        // Add Authentication and Authorization
        services.AddYapplrAuthentication(configuration);
        services.AddYapplrAuthorization();

        // Add HTTP context accessor
        services.AddHttpContextAccessor();

        // Add CORS
        services.AddYapplrCors(configuration);

        // Add AWS SES
        services.AddAwsSes(configuration);

        // Add SendGrid
        services.AddSendGrid(configuration);

        // Add SignalR
        services.AddYapplrSignalR(configuration, environment);

        // Add memory cache
        services.AddMemoryCache();

        // Add custom services
        services.AddApplicationServices();
        services.AddAdminServices();
        services.AddSeedServices(environment);

        // Add HTTP clients
        services.AddHttpClients();

        // Add notification services
        services.AddNotificationServices(configuration);

        // Add email services
        services.AddEmailServices();

        // Add CQRS and RabbitMQ services
        services.AddCqrsServices(configuration);

        return services;
    }

    private static IServiceCollection AddEntityFramework(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<YapplrDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

            // Configure warnings
            options.ConfigureWarnings(warnings =>
            {
                // Suppress the multiple collection include warning since we're using split queries
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
            });
        });

        return services;
    }

    private static IServiceCollection AddYapplrAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is required");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });

        return services;
    }

    private static IServiceCollection AddYapplrAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("User", policy =>
                policy.Requirements.Add(new RoleRequirement(UserRole.User)));

            options.AddPolicy("ActiveUser", policy =>
            {
                policy.Requirements.Add(new RoleRequirement(UserRole.User, UserStatus.Active));
            });

            options.AddPolicy("Moderator", policy =>
            {
                policy.Requirements.Add(new RoleRequirement(UserRole.Moderator, UserStatus.Active));
            });

            options.AddPolicy("Admin", policy =>
            {
                policy.Requirements.Add(new RoleRequirement(UserRole.Admin, UserStatus.Active));
            });

            options.AddPolicy("System", policy =>
                policy.Requirements.Add(new RoleRequirement(UserRole.System)));
        });

        // Register authorization handler
        services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();

        return services;
    }

    private static IServiceCollection AddYapplrCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsConfig = configuration
            .GetSection(CorsConfiguration.SectionName)
            .Get<CorsConfiguration>() ?? new CorsConfiguration();

        services.AddCors(options =>
        {
            // Configure AllowFrontend policy
            options.AddPolicy("AllowFrontend", policy =>
            {
                var frontendConfig = corsConfig.AllowFrontend;
                ConfigureCorsPolicy(policy, frontendConfig);
            });

            // Configure AllowSignalR policy
            options.AddPolicy("AllowSignalR", policy =>
            {
                var signalRConfig = corsConfig.AllowSignalR;
                ConfigureCorsPolicy(policy, signalRConfig);
            });

            // Configure AllowAll policy
            options.AddPolicy("AllowAll", policy =>
            {
                var allowAllConfig = corsConfig.AllowAll;
                ConfigureCorsPolicy(policy, allowAllConfig);
            });
        });

        return services;
    }

    private static void ConfigureCorsPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy, CorsPolicyConfiguration corsPolicy)
    {
        if (corsPolicy.AllowAnyOrigin)
        {
            policy.AllowAnyOrigin();
        }
        else if (corsPolicy.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsPolicy.AllowedOrigins);
        }

        if (corsPolicy.AllowAnyHeader)
        {
            policy.AllowAnyHeader();
        }
        else if (corsPolicy.AllowedHeaders.Length > 0)
        {
            policy.WithHeaders(corsPolicy.AllowedHeaders);
        }

        if (corsPolicy.AllowAnyMethod)
        {
            policy.AllowAnyMethod();
        }
        else if (corsPolicy.AllowedMethods.Length > 0)
        {
            policy.WithMethods(corsPolicy.AllowedMethods);
        }

        if (corsPolicy.AllowCredentials && !corsPolicy.AllowAnyOrigin)
        {
            policy.AllowCredentials();
        }
    }

    private static IServiceCollection AddAwsSes(this IServiceCollection services, IConfiguration configuration)
    {
        var awsSesSettings = configuration.GetSection("AwsSesSettings");
        var awsRegion = awsSesSettings["Region"] ?? "us-east-1";
        var awsAccessKey = awsSesSettings["AccessKey"];
        var awsSecretKey = awsSesSettings["SecretKey"];

        if (!string.IsNullOrEmpty(awsAccessKey) && !string.IsNullOrEmpty(awsSecretKey))
        {
            // Use explicit credentials
            services.AddSingleton<IAmazonSimpleEmailService>(provider =>
                new AmazonSimpleEmailServiceClient(awsAccessKey, awsSecretKey, Amazon.RegionEndpoint.GetBySystemName(awsRegion)));
        }
        else
        {
            // Use default credential chain (IAM roles, environment variables, etc.)
            services.AddSingleton<IAmazonSimpleEmailService>(provider =>
                new AmazonSimpleEmailServiceClient(Amazon.RegionEndpoint.GetBySystemName(awsRegion)));
        }

        return services;
    }

    private static IServiceCollection AddSendGrid(this IServiceCollection services, IConfiguration configuration)
    {
        var sendGridSettings = configuration.GetSection("SendGridSettings");
        var sendGridApiKey = sendGridSettings["ApiKey"];
        if (!string.IsNullOrEmpty(sendGridApiKey))
        {
            services.AddSingleton<ISendGridClient>(provider =>
                new SendGridClient(sendGridApiKey));
        }

        return services;
    }

    private static IServiceCollection AddYapplrSignalR(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var notificationConfig = configuration
            .GetSection(NotificationProvidersConfiguration.SectionName)
            .Get<NotificationProvidersConfiguration>() ?? new NotificationProvidersConfiguration();

        if (notificationConfig.SignalR.Enabled)
        {
            services.AddSignalR(options =>
            {
                // Connection timeouts
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);

                // Message size and buffer limits
                options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
                options.StreamBufferCapacity = 10;

                // Enable detailed errors based on configuration
                options.EnableDetailedErrors = notificationConfig.SignalR.EnableDetailedErrors || environment.IsDevelopment();

                // Maximum parallel invocations per connection
                options.MaximumParallelInvocationsPerClient = 1;
            });
        }

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IUserCacheService, UserCacheService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITagAnalyticsService, TagAnalyticsService>();
        services.AddScoped<ILinkPreviewService, LinkPreviewService>();

        return services;
    }

    private static IServiceCollection AddAdminServices(this IServiceCollection services)
    {
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserReportService, UserReportService>();
        services.AddScoped<IModerationMessageService, ModerationMessageService>();
        services.AddScoped<IContentManagementService, ContentManagementService>();

        return services;
    }

    private static IServiceCollection AddSeedServices(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddScoped<SystemTagSeedService>();
        services.AddScoped<EssentialUserSeedService>();
        services.AddScoped<ContentSeedService>();

        // Add test data seed service (for all environments except production)
        Console.WriteLine($"🔍 Environment detected: {environment.EnvironmentName} (IsProduction: {environment.IsProduction()})");
        if (!environment.IsProduction())
        {
            Console.WriteLine("✅ Registering StagingSeedService for non-production environment");
            services.AddScoped<StagingSeedService>();
        }
        else
        {
            Console.WriteLine("⚠️ Skipping StagingSeedService registration - Production environment detected");
        }

        return services;
    }

    private static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<LinkPreviewService>();
        services.AddHttpClient<ContentModerationService>();
        services.AddScoped<IContentModerationService, ContentModerationService>();

        return services;
    }

    private static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var notificationConfig = configuration
            .GetSection(NotificationProvidersConfiguration.SectionName)
            .Get<NotificationProvidersConfiguration>() ?? new NotificationProvidersConfiguration();

        // Register performance and monitoring services
        services.AddSingleton<ISignalRConnectionPool, SignalRConnectionPool>();
        services.AddHostedService<SignalRCleanupService>();
        services.AddSingleton<INotificationMetricsService, NotificationMetricsService>();
        services.AddScoped<INotificationQueueService, NotificationQueueService>();

        // Register advanced notification services
        services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();
        services.AddScoped<INotificationDeliveryService, NotificationDeliveryService>();

        // Register UX enhancement services
        services.AddSingleton<ISmartRetryService, SmartRetryService>();
        services.AddSingleton<INotificationCompressionService, NotificationCompressionService>();
        services.AddScoped<IOfflineNotificationService, OfflineNotificationService>();

        // Register security services
        services.AddSingleton<INotificationRateLimitService, NotificationRateLimitService>();
        services.AddSingleton<INotificationContentFilterService, NotificationContentFilterService>();
        services.AddScoped<INotificationAuditService, NotificationAuditService>();

        // Register background services
        services.AddHostedService<NotificationBackgroundService>();

        // Configure notification providers
        services.Configure<NotificationProvidersConfiguration>(
            configuration.GetSection(NotificationProvidersConfiguration.SectionName));

        // Configure CORS
        services.Configure<CorsConfiguration>(
            configuration.GetSection(CorsConfiguration.SectionName));

        // Configure Frontend URLs
        services.Configure<FrontendUrlsConfiguration>(
            configuration.GetSection(FrontendUrlsConfiguration.SectionName));

        // Add provider-specific services
        if (notificationConfig.Firebase.Enabled)
        {
            services.AddScoped<IFirebaseService, FirebaseService>();
        }

        if (notificationConfig.SignalR.Enabled)
        {
            services.AddScoped<SignalRNotificationService>();
        }

        // Register Expo notification service
        var expoEnabled = configuration.GetValue<bool>("NotificationProviders:Expo:Enabled", false);
        if (expoEnabled)
        {
            services.AddHttpClient<ExpoNotificationService>();
            services.AddScoped<ExpoNotificationService>();
        }

        // Register provider collection for dependency injection
        services.AddScoped<IEnumerable<IRealtimeNotificationProvider>>(provider =>
        {
            var providers = new List<IRealtimeNotificationProvider>();

            if (notificationConfig.Firebase.Enabled)
            {
                providers.Add(provider.GetRequiredService<IFirebaseService>());
            }

            if (notificationConfig.SignalR.Enabled)
            {
                providers.Add(provider.GetRequiredService<SignalRNotificationService>());
            }

            if (expoEnabled)
            {
                providers.Add(provider.GetRequiredService<ExpoNotificationService>());
            }

            return providers;
        });

        // Register the composite notification service with explicit provider collection
        services.AddScoped<ICompositeNotificationService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<CompositeNotificationService>>();
            var providers = provider.GetRequiredService<IEnumerable<IRealtimeNotificationProvider>>();
            var preferencesService = provider.GetRequiredService<INotificationPreferencesService>();
            var deliveryService = provider.GetRequiredService<INotificationDeliveryService>();
            var retryService = provider.GetRequiredService<ISmartRetryService>();
            var compressionService = provider.GetRequiredService<INotificationCompressionService>();
            var offlineService = provider.GetRequiredService<IOfflineNotificationService>();
            var rateLimitService = provider.GetRequiredService<INotificationRateLimitService>();
            var contentFilterService = provider.GetRequiredService<INotificationContentFilterService>();
            var auditService = provider.GetRequiredService<INotificationAuditService>();

            return new CompositeNotificationService(
                logger,
                providers,
                preferencesService,
                deliveryService,
                retryService,
                compressionService,
                offlineService,
                rateLimitService,
                contentFilterService,
                auditService);
        });

        return services;
    }

    private static IServiceCollection AddEmailServices(this IServiceCollection services)
    {
        // Register email senders
        services.AddScoped<Yapplr.Api.Services.EmailSenders.AwsSesEmailSender>();
        services.AddScoped<Yapplr.Api.Services.EmailSenders.SendGridEmailSender>();
        services.AddScoped<Yapplr.Api.Services.EmailSenders.SmtpEmailSender>();
        services.AddScoped<IEmailSenderFactory, EmailSenderFactory>();

        // Register email service factory
        services.AddScoped<IEmailServiceFactory, EmailServiceFactory>();

        // Register the email service based on configuration
        services.AddScoped<IEmailService>(provider =>
        {
            var factory = provider.GetRequiredService<IEmailServiceFactory>();
            return factory.CreateEmailService();
        });

        return services;
    }

    private static IServiceCollection AddCqrsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add MassTransit with RabbitMQ
        services.AddMassTransit(x =>
        {
            // Register all command handlers
            x.AddConsumers(typeof(Program).Assembly);

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqConfig = configuration.GetSection("RabbitMQ");
                var host = rabbitMqConfig["Host"] ?? "localhost";
                var port = rabbitMqConfig.GetValue<int>("Port", 5672);
                var username = rabbitMqConfig["Username"] ?? "guest";
                var password = rabbitMqConfig["Password"] ?? "guest";
                var virtualHost = rabbitMqConfig["VirtualHost"] ?? "/";

                cfg.Host(host, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                // Configure retry policy
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromMinutes(1)
                ));

                // Configure endpoints for command handlers
                cfg.ConfigureEndpoints(context);
            });
        });

        // Register CQRS services
        services.AddScoped<ICommandPublisher, CommandPublisher>();

        return services;
    }
}
