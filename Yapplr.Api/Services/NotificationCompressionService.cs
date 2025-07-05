using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Yapplr.Api.Services;

/// <summary>
/// Compressed notification payload
/// </summary>
public class CompressedNotificationPayload
{
    public string CompressedData { get; set; } = string.Empty;
    public string CompressionMethod { get; set; } = string.Empty;
    public int OriginalSize { get; set; }
    public int CompressedSize { get; set; }
    public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize : 1.0;
}

/// <summary>
/// Notification payload optimization settings
/// </summary>
public class OptimizationSettings
{
    public bool EnableCompression { get; set; } = true;
    public int CompressionThreshold { get; set; } = 1024; // Only compress if payload > 1KB
    public bool TruncateLongMessages { get; set; } = true;
    public int MaxMessageLength { get; set; } = 200;
    public bool RemoveUnnecessaryFields { get; set; } = true;
    public bool UseShortFieldNames { get; set; } = true;
}

/// <summary>
/// Service for optimizing notification payloads to reduce bandwidth usage
/// </summary>
public interface INotificationCompressionService
{
    Task<CompressedNotificationPayload> CompressPayloadAsync(object payload, OptimizationSettings? settings = null);
    Task<T> DecompressPayloadAsync<T>(CompressedNotificationPayload compressedPayload);
    Task<object> OptimizePayloadAsync(object payload, OptimizationSettings? settings = null);
    Task<Dictionary<string, object>> GetCompressionStatsAsync();
}

public class NotificationCompressionService : INotificationCompressionService
{
    private readonly ILogger<NotificationCompressionService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Statistics tracking
    private long _totalPayloadsProcessed = 0;
    private long _totalOriginalBytes = 0;
    private long _totalCompressedBytes = 0;
    private readonly object _statsLock = new object();

    public NotificationCompressionService(ILogger<NotificationCompressionService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false, // Minimize JSON size
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<CompressedNotificationPayload> CompressPayloadAsync(object payload, OptimizationSettings? settings = null)
    {
        settings ??= new OptimizationSettings();

        try
        {
            // First optimize the payload
            var optimizedPayload = await OptimizePayloadAsync(payload, settings);
            
            // Serialize to JSON
            var jsonString = JsonSerializer.Serialize(optimizedPayload, _jsonOptions);
            var originalBytes = Encoding.UTF8.GetBytes(jsonString);
            var originalSize = originalBytes.Length;

            // Check if compression is beneficial
            if (!settings.EnableCompression || originalSize < settings.CompressionThreshold)
            {
                _logger.LogDebug("Skipping compression for payload of size {Size} bytes", originalSize);
                
                UpdateStats(originalSize, originalSize);
                
                return new CompressedNotificationPayload
                {
                    CompressedData = Convert.ToBase64String(originalBytes),
                    CompressionMethod = "none",
                    OriginalSize = originalSize,
                    CompressedSize = originalSize
                };
            }

            // Compress using Gzip
            var compressedBytes = await CompressWithGzipAsync(originalBytes);
            var compressedSize = compressedBytes.Length;

            // Only use compression if it actually reduces size significantly
            if (compressedSize >= originalSize * 0.9) // Less than 10% savings
            {
                _logger.LogDebug("Compression not beneficial for payload of size {Size} bytes", originalSize);
                
                UpdateStats(originalSize, originalSize);
                
                return new CompressedNotificationPayload
                {
                    CompressedData = Convert.ToBase64String(originalBytes),
                    CompressionMethod = "none",
                    OriginalSize = originalSize,
                    CompressedSize = originalSize
                };
            }

            UpdateStats(originalSize, compressedSize);

            _logger.LogDebug("Compressed payload from {OriginalSize} to {CompressedSize} bytes ({Ratio:P1} compression)",
                originalSize, compressedSize, (double)compressedSize / originalSize);

            return new CompressedNotificationPayload
            {
                CompressedData = Convert.ToBase64String(compressedBytes),
                CompressionMethod = "gzip",
                OriginalSize = originalSize,
                CompressedSize = compressedSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compress notification payload");
            
            // Fallback to uncompressed
            var fallbackJson = JsonSerializer.Serialize(payload, _jsonOptions);
            var fallbackBytes = Encoding.UTF8.GetBytes(fallbackJson);
            
            return new CompressedNotificationPayload
            {
                CompressedData = Convert.ToBase64String(fallbackBytes),
                CompressionMethod = "none",
                OriginalSize = fallbackBytes.Length,
                CompressedSize = fallbackBytes.Length
            };
        }
    }

    public async Task<T> DecompressPayloadAsync<T>(CompressedNotificationPayload compressedPayload)
    {
        try
        {
            var compressedBytes = Convert.FromBase64String(compressedPayload.CompressedData);
            
            byte[] originalBytes;
            if (compressedPayload.CompressionMethod == "gzip")
            {
                originalBytes = await DecompressWithGzipAsync(compressedBytes);
            }
            else
            {
                originalBytes = compressedBytes;
            }

            var jsonString = Encoding.UTF8.GetString(originalBytes);
            var result = JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);
            
            if (result == null)
            {
                throw new InvalidOperationException("Deserialization resulted in null");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decompress notification payload");
            throw;
        }
    }

    public async Task<object> OptimizePayloadAsync(object payload, OptimizationSettings? settings = null)
    {
        settings ??= new OptimizationSettings();

        try
        {
            // Convert to dictionary for manipulation
            var jsonString = JsonSerializer.Serialize(payload, _jsonOptions);
            var payloadDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, _jsonOptions);
            
            if (payloadDict == null)
                return payload;

            var optimizedDict = new Dictionary<string, object>();

            foreach (var kvp in payloadDict)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                // Use short field names to save bandwidth
                if (settings.UseShortFieldNames)
                {
                    key = GetShortFieldName(key);
                }

                // Remove unnecessary fields
                if (settings.RemoveUnnecessaryFields && IsUnnecessaryField(key, value))
                {
                    continue;
                }

                // Truncate long messages
                if (settings.TruncateLongMessages && IsMessageField(key) && value is string stringValue)
                {
                    if (stringValue.Length > settings.MaxMessageLength)
                    {
                        value = stringValue[..settings.MaxMessageLength] + "...";
                    }
                }

                optimizedDict[key] = value;
            }

            await Task.CompletedTask; // For async consistency
            return optimizedDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize notification payload");
            return payload; // Return original on error
        }
    }

    public async Task<Dictionary<string, object>> GetCompressionStatsAsync()
    {
        await Task.CompletedTask;
        
        lock (_statsLock)
        {
            var totalSavings = _totalOriginalBytes - _totalCompressedBytes;
            var compressionRatio = _totalOriginalBytes > 0 ? (double)_totalCompressedBytes / _totalOriginalBytes : 1.0;
            var bandwidthSavings = _totalOriginalBytes > 0 ? (double)totalSavings / _totalOriginalBytes : 0.0;

            return new Dictionary<string, object>
            {
                ["total_payloads_processed"] = _totalPayloadsProcessed,
                ["total_original_bytes"] = _totalOriginalBytes,
                ["total_compressed_bytes"] = _totalCompressedBytes,
                ["total_bytes_saved"] = totalSavings,
                ["average_compression_ratio"] = compressionRatio,
                ["bandwidth_savings_percentage"] = bandwidthSavings * 100,
                ["average_original_size"] = _totalPayloadsProcessed > 0 ? (double)_totalOriginalBytes / _totalPayloadsProcessed : 0,
                ["average_compressed_size"] = _totalPayloadsProcessed > 0 ? (double)_totalCompressedBytes / _totalPayloadsProcessed : 0
            };
        }
    }

    private async Task<byte[]> CompressWithGzipAsync(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            await gzip.WriteAsync(data);
        }
        return output.ToArray();
    }

    private async Task<byte[]> DecompressWithGzipAsync(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        
        await gzip.CopyToAsync(output);
        return output.ToArray();
    }

    private void UpdateStats(int originalSize, int compressedSize)
    {
        lock (_statsLock)
        {
            _totalPayloadsProcessed++;
            _totalOriginalBytes += originalSize;
            _totalCompressedBytes += compressedSize;
        }
    }

    private static string GetShortFieldName(string fieldName)
    {
        // Map common field names to shorter versions
        return fieldName.ToLower() switch
        {
            "title" => "t",
            "body" => "b",
            "message" => "m",
            "username" => "u",
            "timestamp" => "ts",
            "notification_type" => "nt",
            "conversation_id" => "cid",
            "post_id" => "pid",
            "user_id" => "uid",
            "data" => "d",
            "metadata" => "md",
            _ => fieldName
        };
    }

    private static bool IsUnnecessaryField(string fieldName, object? value)
    {
        // Remove null or empty values
        if (value == null)
            return true;

        if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            return true;

        // Remove debug/internal fields in production
        var unnecessaryFields = new[] { "debug", "internal", "trace_id", "request_id" };
        return unnecessaryFields.Contains(fieldName.ToLower());
    }

    private static bool IsMessageField(string fieldName)
    {
        var messageFields = new[] { "body", "message", "content", "text", "description" };
        return messageFields.Contains(fieldName.ToLower());
    }
}
