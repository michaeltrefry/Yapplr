using Yapplr.Api.Extensions;
using Yapplr.Shared.Extensions;
using Serilog;
using Serilog.Enrichers;
using Serilog.Sinks.Grafana.Loki;

// Auto-detect environment based on Git branch
EnvironmentExtensions.ConfigureEnvironmentFromGitBranch();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    var lokiUrl = context.Configuration.GetValue<string>("Logging:Loki:Url") ?? "http://loki:3100";
    var environment = context.HostingEnvironment.EnvironmentName;
    var applicationName = context.HostingEnvironment.ApplicationName;

    configuration
        .ReadFrom.Configuration(context.Configuration)
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
        .WriteTo.GrafanaLoki(
            uri: lokiUrl,
            labels: new[]
            {
                new LokiLabel { Key = "app", Value = applicationName },
                new LokiLabel { Key = "environment", Value = environment },
                new LokiLabel { Key = "service", Value = "yapplr-api" }
            },
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
});

Console.WriteLine(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

// Add services to the container
builder.Services.AddYapplrServices(builder.Configuration, builder.Environment);

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
