using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Amazon.SimpleEmail;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using Yapplr.Api.Endpoints;
using Yapplr.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

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
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Common frontend ports
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    // Allow all origins for mobile development
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

// Add custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IBlockService, BlockService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

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
// Use AllowAll CORS policy for development (includes mobile)
app.UseCors("AllowAll");
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

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
