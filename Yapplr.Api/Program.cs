using Yapplr.Api.Extensions;
using Yapplr.Shared.Extensions;
using Serilog;
using Serilog.Enrichers;
using Serilog.Events;


// Auto-detect environment based on Git branch
EnvironmentExtensions.ConfigureEnvironmentFromGitBranch();

// Enable Serilog self-logging to see any issues
Serilog.Debugging.SelfLog.Enable(Console.WriteLine);

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel server options for large file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2GB
});

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    var seqUrl = context.Configuration.GetValue<string>("Logging:Seq:Url") ?? "http://seq:80";
    var environment = context.HostingEnvironment.EnvironmentName;
    var applicationName = context.HostingEnvironment.ApplicationName;

    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", applicationName)
        .Enrich.WithProperty("Environment", environment)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File(
            path: "/app/logs/yapplr-api-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.Seq(seqUrl);
});

Console.WriteLine(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

// Add services to the container
builder.Services.AddYapplrServices(builder.Configuration, builder.Environment);

// Configure form options for large file uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024; // 2GB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

var app = builder.Build();

// Handle command line arguments for admin operations
if (await app.HandleCommandLineArgumentsAsync(args))
{
    return;
}

// Run database migrations at startup
await app.RunDatabaseMigrationsAsync();

// Configure the HTTP request pipeline
app.ConfigureYapplrPipeline();

// Map endpoints
app.MapYapplrEndpoints();
app.MapYapplrHubs(builder.Configuration);
app.MapHealthCheck();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
