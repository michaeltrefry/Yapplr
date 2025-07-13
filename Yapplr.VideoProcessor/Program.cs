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
builder.Services.AddSingleton<ICodecTestService, CodecTestService>();

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

            // Configure connection resilience
            h.RequestedConnectionTimeout(TimeSpan.FromSeconds(30));
            h.Heartbeat(TimeSpan.FromSeconds(60));
        });

        // Configure retry policy with exception filtering
        cfg.UseMessageRetry(r =>
        {
            r.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromMinutes(1)
            );

            // Don't retry on video processing failures - these are usually permanent
            r.Ignore<FFMpegCore.Exceptions.FFMpegException>();
            r.Ignore<InvalidOperationException>();
            r.Ignore<FileNotFoundException>();
        });

        // Configure endpoints for consumers
        cfg.ConfigureEndpoints(context);
    });
});

// Add hosted service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Run codec compatibility test on startup
using (var scope = host.Services.CreateScope())
{
    var codecTestService = scope.ServiceProvider.GetRequiredService<ICodecTestService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Running codec compatibility test on startup...");
    var testResult = await codecTestService.RunCodecTestsAsync();

    if (testResult.Success)
    {
        logger.LogInformation("✅ Codec compatibility test passed - all required codecs are available");
        logger.LogInformation("Available video codecs: {VideoCodecs}",
            string.Join(", ", testResult.VideoCodecs.Where(c => c.Value).Select(c => c.Key)));
        logger.LogInformation("Available audio codecs: {AudioCodecs}",
            string.Join(", ", testResult.AudioCodecs.Where(c => c.Value).Select(c => c.Key)));
    }
    else
    {
        logger.LogWarning("⚠️ Codec compatibility test failed: {ErrorMessage}", testResult.ErrorMessage);
        logger.LogWarning("Some video processing features may not work correctly");

        // Log missing codecs
        var missingVideoCodecs = testResult.VideoCodecs.Where(c => !c.Value).Select(c => c.Key);
        var missingAudioCodecs = testResult.AudioCodecs.Where(c => !c.Value).Select(c => c.Key);

        if (missingVideoCodecs.Any())
            logger.LogWarning("Missing video codecs: {MissingVideoCodecs}", string.Join(", ", missingVideoCodecs));
        if (missingAudioCodecs.Any())
            logger.LogWarning("Missing audio codecs: {MissingAudioCodecs}", string.Join(", ", missingAudioCodecs));
    }
}

host.Run();
