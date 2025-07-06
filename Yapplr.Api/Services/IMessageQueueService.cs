using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IMessageQueueService
{
    Task QueueVideoProcessingAsync(VideoProcessingJobRequest job);
    Task<VideoProcessingJobRequest?> DequeueVideoProcessingAsync();
    Task UpdateVideoProcessingStatusAsync(int jobId, VideoProcessingStatus status, string? errorMessage = null);
    Task<VideoProcessingJobRequest?> GetJobAsync(int jobId);
    Task<IEnumerable<VideoProcessingJobRequest>> GetJobsByUserAsync(int userId);
}

// In-memory implementation for development (replace with Redis/RabbitMQ for production)
public class InMemoryMessageQueueService : IMessageQueueService
{
    private readonly Queue<VideoProcessingJobRequest> _videoProcessingQueue = new();
    private readonly Dictionary<int, VideoProcessingJobRequest> _jobs = new();
    private readonly object _lock = new();
    private readonly ILogger<InMemoryMessageQueueService> _logger;
    private int _nextJobId = 1;

    public InMemoryMessageQueueService(ILogger<InMemoryMessageQueueService> logger)
    {
        _logger = logger;
    }

    public Task QueueVideoProcessingAsync(VideoProcessingJobRequest job)
    {
        lock (_lock)
        {
            job.Id = _nextJobId++;
            _jobs[job.Id] = job;
            _videoProcessingQueue.Enqueue(job);

            _logger.LogInformation("Video processing job queued: {JobId} - {FileName}",
                job.Id, job.FileName);
        }

        return Task.CompletedTask;
    }

    public Task<VideoProcessingJobRequest?> DequeueVideoProcessingAsync()
    {
        lock (_lock)
        {
            if (_videoProcessingQueue.Count > 0)
            {
                var job = _videoProcessingQueue.Dequeue();
                job.Status = VideoProcessingStatus.Processing;

                _logger.LogInformation("Video processing job dequeued: {JobId} - {FileName}",
                    job.Id, job.FileName);

                return Task.FromResult<VideoProcessingJobRequest?>(job);
            }
        }

        return Task.FromResult<VideoProcessingJobRequest?>(null);
    }

    public Task UpdateVideoProcessingStatusAsync(int jobId, VideoProcessingStatus status, string? errorMessage = null)
    {
        lock (_lock)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                job.Status = status;
                job.ErrorMessage = errorMessage;
                
                if (status == VideoProcessingStatus.Completed || status == VideoProcessingStatus.Failed)
                {
                    job.ProcessedAt = DateTime.UtcNow;
                }
                
                _logger.LogInformation("Video processing job status updated: {JobId} - {Status}", 
                    jobId, status);
            }
        }
        
        return Task.CompletedTask;
    }

    public Task<VideoProcessingJobRequest?> GetJobAsync(int jobId)
    {
        lock (_lock)
        {
            _jobs.TryGetValue(jobId, out var job);
            return Task.FromResult(job);
        }
    }

    public Task<IEnumerable<VideoProcessingJobRequest>> GetJobsByUserAsync(int userId)
    {
        lock (_lock)
        {
            var userJobs = _jobs.Values.Where(j => j.UserId == userId).ToList();
            return Task.FromResult<IEnumerable<VideoProcessingJobRequest>>(userJobs);
        }
    }
}

// Redis implementation for production
public class RedisMessageQueueService : IMessageQueueService
{
    // TODO: Implement Redis-based message queue
    // This would use StackExchange.Redis for production deployments
    
    public Task QueueVideoProcessingAsync(VideoProcessingJobRequest job)
    {
        throw new NotImplementedException("Redis implementation pending");
    }

    public Task<VideoProcessingJobRequest?> DequeueVideoProcessingAsync()
    {
        throw new NotImplementedException("Redis implementation pending");
    }

    public Task UpdateVideoProcessingStatusAsync(int jobId, VideoProcessingStatus status, string? errorMessage = null)
    {
        throw new NotImplementedException("Redis implementation pending");
    }

    public Task<VideoProcessingJobRequest?> GetJobAsync(int jobId)
    {
        throw new NotImplementedException("Redis implementation pending");
    }

    public Task<IEnumerable<VideoProcessingJobRequest>> GetJobsByUserAsync(int userId)
    {
        throw new NotImplementedException("Redis implementation pending");
    }
}
