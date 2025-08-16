namespace FileCategorization_Shared.DTOs.DD;

/// <summary>
/// Summary information for a thread
/// </summary>
public class ThreadSummaryDto
{
    /// <summary>
    /// Thread ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Thread main title
    /// </summary>
    public string MainTitle { get; set; } = string.Empty;

    /// <summary>
    /// Thread type/category
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Thread URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Total number of links
    /// </summary>
    public int LinksCount { get; set; }

    /// <summary>
    /// Number of new (unprocessed) links
    /// </summary>
    public int NewLinksCount { get; set; }

    /// <summary>
    /// Number of used links
    /// </summary>
    public int UsedLinksCount { get; set; }

    /// <summary>
    /// Whether thread has new links
    /// </summary>
    public bool HasNewLinks { get; set; }

    /// <summary>
    /// Thread creation date
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }
}