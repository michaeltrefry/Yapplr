using Yapplr.VideoProcessor;
using Yapplr.VideoProcessor.Services;
using Yapplr.Shared.Extensions;
using MassTransit;

// Auto-detect environment based on Git branch
EnvironmentExtensions.ConfigureEnvironmentFromGitBranch();

Console.WriteLine($"Environment after detection: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

var builder = Host.CreateApplicationBuilder(args);

Console.WriteLine($"Builder environment: {builder.Environment.EnvironmentName}");

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Add services
builder.Services.AddSingleton<IVideoProcessingService, VideoProcessingService>();

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<VideoProcessingRequestConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
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

        // Configure endpoints for consumers
        cfg.ConfigureEndpoints(context);
    });
});

// Add hosted service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
