namespace FileCategorization_Api.Contracts.DD;

/// <summary>
/// Result of thread processing operation
/// </summary>
public class ThreadProcessingResultDto
{
    /// <summary>
    /// Thread ID
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// Thread title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Thread URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Whether this was a new thread creation
    /// </summary>
    public bool IsNewThread { get; set; }

    /// <summary>
    /// Number of new links found
    /// </summary>
    public int NewLinksCount { get; set; }

    /// <summary>
    /// Number of existing links updated
    /// </summary>
    public int UpdatedLinksCount { get; set; }

    /// <summary>
    /// Total links for this thread
    /// </summary>
    public int TotalLinksCount { get; set; }

    /// <summary>
    /// Processing timestamp
    /// </summary>
    public DateTime ProcessedAt { get; set; }
}