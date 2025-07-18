using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Amazon.SimpleEmail;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using Yapplr.Api.Configuration;
using SendGrid;
using Yapplr.Api.Models;
using Yapplr.Api.Authorization;
using MassTransit;
using Yapplr.Api.CQRS;
using Yapplr.Api.Common;
using Yapplr.Api.Services.Unified;

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

        // Configure forwarded headers for reverse proxy support (nginx)
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                                     Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            // Clear known networks and proxies to accept headers from any proxy
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

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

        // Configure request size limits for file uploads
        services.Configure<IISServerOptions>(options =>
        {
            options.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2GB
        });

        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2GB
        });

        services.Configure<FormOptions>(options =>
        {
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024; // 2GB
            options.MultipartHeadersLengthLimit = int.MaxValue;
        });

        // Add Redis caching
        services.AddRedisCaching(configuration);

        // Add external analytics services
        services.AddExternalAnalytics(configuration);

        // Add Prometheus metrics
        services.AddPrometheusMetrics();

        // Add custom services
        services.AddApplicationServices();
        services.AddAdminServices();
        services.AddSeedServices(environment);

        // Add logging enhancement service
        services.AddScoped<ILoggingEnhancementService, LoggingEnhancementService>();

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
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<IMultipleFileUploadService, MultipleFileUploadService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IUploadSettingsService, UploadSettingsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITagAnalyticsService, TagAnalyticsService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<ILinkPreviewService, LinkPreviewService>();
        services.AddScoped<ITrustScoreService, TrustScoreService>();
        services.AddScoped<ITrustBasedModerationService, TrustBasedModerationService>();

        // Add trust score background service
        services.AddHostedService<TrustScoreBackgroundService>();

        // Add caching services
        services.AddScoped<ICountCacheService, CountCacheService>();

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

        // Register advanced notification services
        services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();

        // Register security services
        services.AddSingleton<IApiRateLimitService, ApiRateLimitService>();

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

        // Configure Uploads
        services.Configure<UploadsConfiguration>(
            configuration.GetSection(UploadsConfiguration.SectionName));

        // Configure Trust Score Background Service
        services.Configure<TrustScoreBackgroundOptions>(
            configuration.GetSection(TrustScoreBackgroundOptions.SectionName));

        // Configure Rate Limiting
        services.Configure<RateLimitingConfiguration>(
            configuration.GetSection(RateLimitingConfiguration.SectionName));

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

        // Register the new unified notification services
        services.AddScoped<INotificationProviderManager, NotificationProviderManager>();
        services.AddScoped<INotificationQueue, NotificationQueue>();
        services.AddScoped<INotificationEnhancementService, NotificationEnhancementService>();
        services.AddScoped<IUnifiedNotificationService, UnifiedNotificationService>();



        return services;
    }

    private static IServiceCollection AddEmailServices(this IServiceCollection services)
    {
        // Register email senders
        services.AddScoped<Services.EmailSenders.AwsSesEmailSender>();
        services.AddScoped<Services.EmailSenders.SendGridEmailSender>();
        services.AddScoped<Services.EmailSenders.SmtpEmailSender>();
        services.AddScoped<IEmailSenderFactory, EmailSenderFactory>();

        // Register the email sender based on configuration
        services.AddScoped<IEmailSender>(provider =>
        {
            var factory = provider.GetRequiredService<IEmailSenderFactory>();
            return factory.CreateEmailSender();
        });

        // Register UnifiedEmailService directly with IEmailSender
        services.AddScoped<IEmailService, EmailService>();

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

                    // Add connection resilience for staging/production
                    h.RequestedConnectionTimeout(TimeSpan.FromSeconds(30));
                    h.Heartbeat(TimeSpan.FromSeconds(60));
                    h.RequestedChannelMax(100);
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

    private static IServiceCollection AddRedisCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetSection("Redis:ConnectionString").Value;

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            // Add Redis connection
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
            {
                var connectionString = redisConnectionString;
                return StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
            });

            // Register Redis-based caching service
            services.AddSingleton<ICachingService, RedisCachingService>();
        }
        else
        {
            // Fallback to memory caching if Redis is not configured
            services.AddSingleton<ICachingService, MemoryCachingService>();
        }

        return services;
    }
}
