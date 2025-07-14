namespace Yapplr.Api.Services;

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