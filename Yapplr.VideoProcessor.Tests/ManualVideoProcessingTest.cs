using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.VideoProcessor.Services;
using Yapplr.Shared.Models;

namespace Yapplr.VideoProcessor.Tests;

/// <summary>
/// Manual test for video processing with real files
/// This is not an automated test - it's for manual verification
/// </summary>
public class ManualVideoProcessingTest
{
    /// <summary>
    /// Test the SimpleVideoProcessingService with a real video file
    /// This method can be called manually for testing
    /// </summary>
    public static async Task TestSimpleVideoProcessing()
    {
        // Setup
        var mockLogger = new Mock<ILogger<SimpleVideoProcessingService>>();
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FFmpeg:BinaryPath"] = "ffmpeg",
                ["FFmpeg:ProbePath"] = "ffprobe"
            });
        var configuration = configBuilder.Build();
        
        var service = new SimpleVideoProcessingService(mockLogger.Object, configuration);
        
        // Create a test video first
        var tempDir = Path.Combine(Path.GetTempPath(), "yapplr_manual_test");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var inputPath = await CreateTestVideo(tempDir, "test_input.mp4", 1920, 1080);
            var outputPath = Path.Combine(tempDir, "test_output.mp4");
            var thumbnailPath = Path.Combine(tempDir, "test_thumbnail.jpg");
            
            var config = new VideoProcessingConfig
            {
                MaxWidth = 1280,
                MaxHeight = 720,
                TargetBitrate = 1000,
                VideoCodec = "libx264",
                AudioCodec = "aac",
                ThumbnailWidth = 320,
                ThumbnailHeight = 240,
                ThumbnailTimeSeconds = 1.0
            };
            
            Console.WriteLine("Testing SimpleVideoProcessingService...");
            Console.WriteLine($"Input: {inputPath}");
            Console.WriteLine($"Output: {outputPath}");
            Console.WriteLine($"Thumbnail: {thumbnailPath}");
            
            // Test metadata reading
            var metadata = await service.GetVideoMetadataAsync(inputPath);
            if (metadata != null)
            {
                Console.WriteLine($"Original dimensions: {metadata.OriginalWidth}x{metadata.OriginalHeight}");
                Console.WriteLine($"Display dimensions: {metadata.DisplayWidth}x{metadata.DisplayHeight}");
                Console.WriteLine($"Rotation: {metadata.OriginalRotation}°");
                Console.WriteLine($"Duration: {metadata.OriginalDuration}");
            }
            else
            {
                Console.WriteLine("Failed to read metadata");
                return;
            }
            
            // Test video processing
            var result = await service.ProcessVideoAsync(inputPath, outputPath, thumbnailPath, config);
            
            if (result.Success)
            {
                Console.WriteLine("✅ Video processing succeeded!");
                Console.WriteLine($"Processed video: {result.ProcessedVideoFileName}");
                Console.WriteLine($"Thumbnail: {result.ThumbnailFileName}");
                Console.WriteLine($"Processing time: {result.ProcessingDuration}");
                
                if (result.Metadata != null)
                {
                    Console.WriteLine($"Processed dimensions: {result.Metadata.ProcessedWidth}x{result.Metadata.ProcessedHeight}");
                    Console.WriteLine($"Compression ratio: {result.Metadata.CompressionRatio:F2}");
                }
                
                // Verify files exist
                if (File.Exists(outputPath))
                {
                    Console.WriteLine($"✅ Output video exists ({new FileInfo(outputPath).Length} bytes)");
                }
                else
                {
                    Console.WriteLine("❌ Output video file not found");
                }
                
                if (File.Exists(thumbnailPath))
                {
                    Console.WriteLine($"✅ Thumbnail exists ({new FileInfo(thumbnailPath).Length} bytes)");
                }
                else
                {
                    Console.WriteLine("❌ Thumbnail file not found");
                }
            }
            else
            {
                Console.WriteLine($"❌ Video processing failed: {result.ErrorMessage}");
            }
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                Console.WriteLine($"Note: Could not clean up temp directory: {tempDir}");
            }
        }
    }
    
    private static async Task<string> CreateTestVideo(string directory, string filename, int width, int height)
    {
        var outputPath = Path.Combine(directory, filename);
        
        // Create a simple test video
        var arguments = $"-f lavfi -i \"testsrc2=size={width}x{height}:duration=5:rate=30\" " +
                       $"-c:v libx264 -pix_fmt yuv420p " +
                       $"-y \"{outputPath}\"";

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new System.Diagnostics.Process { StartInfo = processInfo };
        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to create test video: {error}");
        }

        return outputPath;
    }
}

/// <summary>
/// Simple console program to run the manual test
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "test-video")
        {
            await ManualVideoProcessingTest.TestSimpleVideoProcessing();
        }
        else
        {
            Console.WriteLine("Use 'dotnet run test-video' to run the manual video processing test");
        }
    }
}
