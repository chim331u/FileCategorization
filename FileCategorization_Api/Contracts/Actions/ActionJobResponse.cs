namespace FileCategorization_Api.Contracts.Actions;

/// <summary>
/// Response DTO for action job operations.
/// Provides comprehensive status and progress information.
/// </summary>
public class ActionJobResponse
{
    /// <summary>
    /// Unique identifier for the background job.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable job status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Job start timestamp.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Job completion timestamp (null if still running).
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total number of items to process.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Number of items successfully processed.
    /// </summary>
    public int ProcessedItems { get; set; }

    /// <summary>
    /// Number of items that failed processing.
    /// </summary>
    public int FailedItems { get; set; }

    /// <summary>
    /// Percentage completion (0-100).
    /// </summary>
    public decimal ProgressPercentage => TotalItems > 0 ? (decimal)(ProcessedItems + FailedItems) * 100 / TotalItems : 0;

    /// <summary>
    /// Estimated time remaining (null if cannot be calculated).
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// List of error messages for failed items.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Additional metadata about the job execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}