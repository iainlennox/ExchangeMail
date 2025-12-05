using System.Collections.Concurrent;

namespace ExchangeMail.Core.Services;

public class ImportStatusService
{
    private readonly ConcurrentDictionary<string, ImportJobStatus> _jobs = new();

    public void StartJob(string jobId, int totalItems)
    {
        _jobs[jobId] = new ImportJobStatus
        {
            JobId = jobId,
            TotalItems = totalItems,
            ProcessedItems = 0,
            Status = "Running",
            StartTime = DateTime.UtcNow
        };
    }

    public void UpdateProgress(string jobId, int processedItems)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.ProcessedItems = processedItems;
            job.LastUpdateTime = DateTime.UtcNow;
        }
    }

    public void CompleteJob(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = "Completed";
            job.EndTime = DateTime.UtcNow;
            job.ProcessedItems = job.TotalItems; // Ensure 100%
        }
    }

    public void FailJob(string jobId, string error)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = "Failed";
            job.Error = error;
            job.EndTime = DateTime.UtcNow;
        }
    }

    public ImportJobStatus? GetStatus(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }
}

public class ImportJobStatus
{
    public string JobId { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed
    public string? Error { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? LastUpdateTime { get; set; }

    public int PercentComplete => TotalItems == 0 ? 0 : (int)((double)ProcessedItems / TotalItems * 100);
}
