using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Amazon.SimpleEmail;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using Yapplr.Api.Endpoints;
using Yapplr.Api.Middleware;
using Yapplr.Api.Hubs;
using Yapplr.Api.Configuration;
using SendGrid;
using Yapplr.Api.Models;
using Yapplr.Api.Authorization;


// Auto-detect environment based on Git branch ONLY in development scenarios
// This prevents auto-detection from running in staging/production deployments
if (IsLocalDevelopment())
{
    try
    {
        var gitBranch = GetCurrentGitBranch();
        var environment = (gitBranch == "main" || gitBranch == "master") ? "Test" : "Development";
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
        Console.WriteLine($"🔄 Auto-detected Git branch '{gitBranch}' -> Using {environment} environment");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Could not auto-detect Git branch: {ex.Message}. Using default environment.");
    }
}

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

// Load notification providers configuration early
var notificationConfig = builder.Configuration
    .GetSection(NotificationProvidersConfiguration.SectionName)
    .Get<NotificationProvidersConfiguration>() ?? new NotificationProvidersConfiguration();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<YapplrDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

    // Configure warnings
    options.ConfigureWarnings(warnings =>
    {
        // Suppress the multiple collection include warning since we're using split queries
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
    });
});

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is required");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

// Add authorization with policies
builder.Services.AddAuthorization(options =>
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
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", "http://localhost:5173", "http://192.168.254.181:3000", // Development
                "https://yapplr.com", "https://www.yapplr.com", "https://app.yapplr.com", // Production
                "https://stg.yapplr.com", "https://stg-api.yapplr.com", // Staging HTTPS
                "http://stg.yapplr.com", "http://stg-api.yapplr.com" // Staging HTTP
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    // SignalR-specific CORS policy with credentials support
    options.AddPolicy("AllowSignalR", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", "http://localhost:5173", "http://192.168.254.181:3000", // Development
                "https://yapplr.com", "https://www.yapplr.com", "https://app.yapplr.com", // Production
                "https://stg.yapplr.com", "https://stg-api.yapplr.com", // Staging HTTPS
                "http://stg.yapplr.com", "http://stg-api.yapplr.com" // Staging HTTP
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    // Allow all origins for mobile development (no credentials for mobile)
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure AWS SES
var awsSesSettings = builder.Configuration.GetSection("AwsSesSettings");
var awsRegion = awsSesSettings["Region"] ?? "us-east-1";
var awsAccessKey = awsSesSettings["AccessKey"];
var awsSecretKey = awsSesSettings["SecretKey"];

if (!string.IsNullOrEmpty(awsAccessKey) && !string.IsNullOrEmpty(awsSecretKey))
{
    // Use explicit credentials
    builder.Services.AddSingleton<IAmazonSimpleEmailService>(provider =>
        new AmazonSimpleEmailServiceClient(awsAccessKey, awsSecretKey, Amazon.RegionEndpoint.GetBySystemName(awsRegion)));
}
else
{
    // Use default credential chain (IAM roles, environment variables, etc.)
    builder.Services.AddSingleton<IAmazonSimpleEmailService>(provider =>
        new AmazonSimpleEmailServiceClient(Amazon.RegionEndpoint.GetBySystemName(awsRegion)));
}

// Add SendGrid
var sendGridSettings = builder.Configuration.GetSection("SendGridSettings");
var sendGridApiKey = sendGridSettings["ApiKey"];
if (!string.IsNullOrEmpty(sendGridApiKey))
{
    builder.Services.AddSingleton<ISendGridClient>(provider =>
        new SendGridClient(sendGridApiKey));
}

// Add SignalR with production configuration (if enabled)
if (notificationConfig.SignalR.Enabled)
{
    builder.Services.AddSignalR(options =>
    {
        // Connection timeouts
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);

        // Message size and buffer limits
        options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
        options.StreamBufferCapacity = 10;

        // Enable detailed errors based on configuration
        options.EnableDetailedErrors = notificationConfig.SignalR.EnableDetailedErrors || builder.Environment.IsDevelopment();

        // Maximum parallel invocations per connection
        options.MaximumParallelInvocationsPerClient = 1;
    });
}

// Add memory cache for caching services
builder.Services.AddMemoryCache();

// Add custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IUserCacheService, UserCacheService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IBlockService, BlockService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<ITagAnalyticsService, TagAnalyticsService>();
builder.Services.AddScoped<ILinkPreviewService, LinkPreviewService>();

// Add admin services
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserReportService, UserReportService>();
builder.Services.AddScoped<IModerationMessageService, ModerationMessageService>();
builder.Services.AddScoped<IContentManagementService, ContentManagementService>();
builder.Services.AddScoped<SystemTagSeedService>();
builder.Services.AddScoped<EssentialUserSeedService>();
builder.Services.AddScoped<ContentSeedService>();

// Add test data seed service (for all environments except production)
Console.WriteLine($"🔍 Environment detected: {builder.Environment.EnvironmentName} (IsProduction: {builder.Environment.IsProduction()})");
if (!builder.Environment.IsProduction())
{
    Console.WriteLine("✅ Registering StagingSeedService for non-production environment");
    builder.Services.AddScoped<StagingSeedService>();
}
else
{
    Console.WriteLine("⚠️ Skipping StagingSeedService registration - Production environment detected");
}

// Add HttpClient for LinkPreviewService
builder.Services.AddHttpClient<LinkPreviewService>();

// Add HttpClient for ContentModerationService
builder.Services.AddHttpClient<ContentModerationService>();
builder.Services.AddScoped<IContentModerationService, ContentModerationService>();

// Register performance and monitoring services
builder.Services.AddSingleton<ISignalRConnectionPool, SignalRConnectionPool>();
builder.Services.AddHostedService<SignalRCleanupService>();
builder.Services.AddSingleton<INotificationMetricsService, NotificationMetricsService>();
builder.Services.AddScoped<INotificationQueueService, NotificationQueueService>();

// Register advanced notification services
builder.Services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();
builder.Services.AddScoped<INotificationDeliveryService, NotificationDeliveryService>();

// Register UX enhancement services
builder.Services.AddSingleton<ISmartRetryService, SmartRetryService>();
builder.Services.AddSingleton<INotificationCompressionService, NotificationCompressionService>();
builder.Services.AddScoped<IOfflineNotificationService, OfflineNotificationService>();

// Register security services
builder.Services.AddSingleton<INotificationRateLimitService, NotificationRateLimitService>();
builder.Services.AddSingleton<INotificationContentFilterService, NotificationContentFilterService>();
builder.Services.AddScoped<INotificationAuditService, NotificationAuditService>();

// Register background services
builder.Services.AddHostedService<NotificationBackgroundService>();

// Configure notification providers
builder.Services.Configure<NotificationProvidersConfiguration>(
    builder.Configuration.GetSection(NotificationProvidersConfiguration.SectionName));

if (notificationConfig.Firebase.Enabled)
{
    builder.Services.AddScoped<IFirebaseService, FirebaseService>();
}

if (notificationConfig.SignalR.Enabled)
{
    builder.Services.AddScoped<SignalRNotificationService>();
}

// Register Expo notification service
var expoEnabled = builder.Configuration.GetValue<bool>("NotificationProviders:Expo:Enabled", false);
if (expoEnabled)
{
    builder.Services.AddHttpClient<ExpoNotificationService>();
    builder.Services.AddScoped<ExpoNotificationService>();
}

// Register provider collection for dependency injection
builder.Services.AddScoped<IEnumerable<IRealtimeNotificationProvider>>(provider =>
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
builder.Services.AddScoped<ICompositeNotificationService>(provider =>
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

// Register all email services
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AwsSesEmailService>();
builder.Services.AddScoped<SendGridEmailService>();
builder.Services.AddScoped<IEmailServiceFactory, EmailServiceFactory>();

// Register the email service based on configuration
builder.Services.AddScoped<IEmailService>(provider =>
{
    var factory = provider.GetRequiredService<IEmailServiceFactory>();
    return factory.CreateEmailService();
});

var app = builder.Build();

// Handle command line arguments for admin operations
if (args.Length > 0 && args[0] == "create-admin")
{
    await CreateAdminUser(app.Services, args);
    return;
}

if (args.Length > 0 && args[0] == "promote-user")
{
    await PromoteUser(app.Services, args);
    return;
}

// Run database migrations at startup with retry logic
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 5;
    const int delayMs = 2000;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("🗄️ Running database migrations at startup... (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

            // Test database connection first
            await context.Database.CanConnectAsync();

            // Run migrations
            await context.Database.MigrateAsync();
            logger.LogInformation("✅ Database migrations completed successfully");

            // Seed essential users (system user, etc.)
            logger.LogInformation("👤 Seeding essential users...");
            var essentialUserSeedService = scope.ServiceProvider.GetRequiredService<EssentialUserSeedService>();
            await essentialUserSeedService.SeedEssentialUsersAsync();
            logger.LogInformation("✅ Essential users seeding completed successfully");

            // Seed default system tags
            logger.LogInformation("🏷️ Seeding default system tags...");
            var systemTagSeedService = scope.ServiceProvider.GetRequiredService<SystemTagSeedService>();
            await systemTagSeedService.SeedDefaultSystemTagsAsync();
            logger.LogInformation("✅ System tags seeding completed successfully");

            // Seed content pages
            logger.LogInformation("📄 Seeding content pages...");
            var contentSeedService = scope.ServiceProvider.GetRequiredService<ContentSeedService>();
            await contentSeedService.SeedContentPagesAsync();
            logger.LogInformation("✅ Content pages seeding completed successfully");

            // Seed test data (all environments except production)
            logger.LogInformation("🔍 Environment check for seeding: {Environment} (IsProduction: {IsProduction})",
                app.Environment.EnvironmentName, app.Environment.IsProduction());

            if (!app.Environment.IsProduction())
            {
                logger.LogInformation("🌱 Seeding test data for {Environment} environment...", app.Environment.EnvironmentName);
                var stagingSeedService = scope.ServiceProvider.GetRequiredService<StagingSeedService>();
                await stagingSeedService.SeedStagingDataAsync();
                logger.LogInformation("✅ Test data seeding completed successfully for {Environment} environment", app.Environment.EnvironmentName);
            }
            else
            {
                logger.LogInformation("⚠️ Skipping test data seeding - Production environment detected");
            }

            break; // Success, exit retry loop
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "❌ Failed to run database migrations or seeding at startup (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}ms...",
                attempt, maxRetries, delayMs);
            await Task.Delay(delayMs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to run database migrations or seeding at startup after {MaxRetries} attempts", maxRetries);
            throw; // This will prevent the application from starting if migrations fail
        }
    }
}

// Configure the HTTP request pipeline.
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

// Map SignalR hub (if enabled)
if (notificationConfig.SignalR.Enabled)
{
    app.MapHub<NotificationHub>("/notificationHub");
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));



app.Run();

// Helper methods for command line operations
static async Task CreateAdminUser(IServiceProvider services, string[] args)
{
    if (args.Length < 4)
    {
        Console.WriteLine("Usage: dotnet run create-admin <username> <email> <password>");
        Console.WriteLine("Example: dotnet run create-admin admin admin@yapplr.com SecurePassword123!");
        return;
    }

    var username = args[1];
    var email = args[2];
    var password = args[3];

    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

    try
    {
        // Check if user already exists
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == username || u.Email == email);
        if (existingUser != null)
        {
            Console.WriteLine($"❌ User with username '{username}' or email '{email}' already exists.");
            return;
        }

        // Create admin user
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = authService.HashPassword(password),
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Log the admin creation
        await auditService.LogActionAsync(AuditAction.UserRoleChanged, user.Id, $"Admin user created via command line");

        Console.WriteLine($"✅ Admin user created successfully!");
        Console.WriteLine($"   Username: {username}");
        Console.WriteLine($"   Email: {email}");
        Console.WriteLine($"   Role: Admin");
        Console.WriteLine($"   Status: Active");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error creating admin user: {ex.Message}");
    }
}

static async Task PromoteUser(IServiceProvider services, string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: dotnet run promote-user <username-or-email> <role>");
        Console.WriteLine("Roles: User, Moderator, Admin");
        Console.WriteLine("Example: dotnet run promote-user john@example.com Admin");
        return;
    }

    var usernameOrEmail = args[1];
    var roleString = args[2];

    if (!Enum.TryParse<UserRole>(roleString, true, out var role))
    {
        Console.WriteLine("❌ Invalid role. Valid roles are: User, Moderator, Admin");
        return;
    }

    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
    var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

    try
    {
        // Find user
        var user = await context.Users.FirstOrDefaultAsync(u =>
            u.Username == usernameOrEmail || u.Email == usernameOrEmail);

        if (user == null)
        {
            Console.WriteLine($"❌ User not found: {usernameOrEmail}");
            return;
        }

        var oldRole = user.Role;
        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // Log the role change
        await auditService.LogActionAsync(AuditAction.UserRoleChanged, user.Id,
            $"Role changed from {oldRole} to {role} via command line");

        Console.WriteLine($"✅ User role updated successfully!");
        Console.WriteLine($"   Username: {user.Username}");
        Console.WriteLine($"   Email: {user.Email}");
        Console.WriteLine($"   Old Role: {oldRole}");
        Console.WriteLine($"   New Role: {role}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error promoting user: {ex.Message}");
    }
}

static bool IsLocalDevelopment()
{
    // Check if we're running in a local development environment
    // This prevents auto-detection from running in staging/production
    try
    {
        // Check for common development indicators
        var isDevelopment =
            // Running from source directory (has .git folder)
            Directory.Exists(".git") ||
            Directory.Exists("../.git") ||
            // Running with dotnet run (not published)
            Environment.CommandLine.Contains("dotnet run") ||
            // Local development URLs
            Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Contains("localhost") == true ||
            // Development machine indicators
            Environment.MachineName.ToLower().Contains("dev") ||
            Environment.UserName.ToLower().Contains("dev") ||
            // Not running in container
            !File.Exists("/.dockerenv");

        return isDevelopment;
    }
    catch
    {
        // If we can't determine, err on the side of caution
        return false;
    }
}

static string GetCurrentGitBranch()
{
    try
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "branch --show-current",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
        {
            return output;
        }

        throw new Exception("Git command failed or returned empty result");
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed to execute git command: {ex.Message}");
    }
}
