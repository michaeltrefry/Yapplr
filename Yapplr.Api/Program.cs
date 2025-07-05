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

var builder = WebApplication.CreateBuilder(args);

// Load notification providers configuration early
var notificationConfig = builder.Configuration
    .GetSection(NotificationProvidersConfiguration.SectionName)
    .Get<NotificationProvidersConfiguration>() ?? new NotificationProvidersConfiguration();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<YapplrDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

builder.Services.AddAuthorization();

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://192.168.254.181:3000", "https://yapplr.com", "https://www.yapplr.com", "https://app.yapplr.com") // Common frontend ports
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    // SignalR-specific CORS policy with credentials support
    options.AddPolicy("AllowSignalR", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://192.168.254.181:3000", "https://yapplr.com", "https://www.yapplr.com", "https://app.yapplr.com")
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

// Add custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IBlockService, BlockService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

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

// Register both email services
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AwsSesEmailService>();
builder.Services.AddScoped<IEmailServiceFactory, EmailServiceFactory>();

// Register the email service based on configuration
builder.Services.AddScoped<IEmailService>(provider =>
{
    var factory = provider.GetRequiredService<IEmailServiceFactory>();
    return factory.CreateEmailService();
});

var app = builder.Build();

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

// Map SignalR hub (if enabled)
if (notificationConfig.SignalR.Enabled)
{
    app.MapHub<NotificationHub>("/notificationHub");
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));



app.Run();
