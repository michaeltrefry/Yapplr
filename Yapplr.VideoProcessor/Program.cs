using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yapplr.Api.Data;
using Yapplr.VideoProcessor.Models;
using Yapplr.VideoProcessor.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<YapplrDbContext>(options =>
    options.UseNpgsql(connectionString));

// Video processing configuration
var videoProcessingOptions = new VideoProcessingOptions();
builder.Configuration.GetSection("VideoProcessing").Bind(videoProcessingOptions);
builder.Services.AddSingleton(videoProcessingOptions);

// Services
builder.Services.AddScoped<IFFmpegService, FFmpegService>();
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();

// Background worker
builder.Services.AddHostedService<VideoProcessingWorker>();

var host = builder.Build();

// Ensure database is created and up to date
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Ensuring database is up to date...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to migrate database");
        throw;
    }
}

Console.WriteLine("Yapplr Video Processor starting...");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Max concurrent jobs: {videoProcessingOptions.MaxConcurrentJobs}");
Console.WriteLine($"Poll interval: {videoProcessingOptions.PollIntervalSeconds} seconds");
Console.WriteLine($"Input path: {videoProcessingOptions.InputPath}");
Console.WriteLine($"Output path: {videoProcessingOptions.OutputPath}");
Console.WriteLine($"Thumbnail path: {videoProcessingOptions.ThumbnailPath}");
Console.WriteLine($"FFmpeg path: {videoProcessingOptions.FFmpegPath}");
Console.WriteLine($"FFprobe path: {videoProcessingOptions.FFprobePath}");
Console.WriteLine();

await host.RunAsync();
