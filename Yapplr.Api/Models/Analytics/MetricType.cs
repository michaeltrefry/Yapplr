namespace Yapplr.Api.Models.Analytics;

public enum MetricType
{
    ResponseTime = 0,
    DatabaseQuery = 1,
    ApiCall = 2,
    CacheHit = 3,
    CacheMiss = 4,
    ErrorRate = 5,
    ThroughputRpm = 6,
    MemoryUsage = 7,
    CpuUsage = 8,
    DiskUsage = 9,
    NetworkLatency = 10,
    QueueDepth = 11,
    ProcessingTime = 12,
    UserSessionDuration = 13,
    PageLoadTime = 14,
    VideoProcessingTime = 15,
    ImageProcessingTime = 16,
    NotificationDeliveryTime = 17,
    SearchResponseTime = 18,
    FeedGenerationTime = 19
}