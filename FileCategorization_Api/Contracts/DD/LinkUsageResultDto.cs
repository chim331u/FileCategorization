namespace FileCategorization_Api.Contracts.DD;

/// <summary>
/// Result of link usage operation
/// </summary>
public class LinkUsageResultDto
{
    /// <summary>
    /// Link ID that was used
    /// </summary>
    public int LinkId { get; set; }

    /// <summary>
    /// Title of the link
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The ED2K link content
    /// </summary>
    public string Ed2kLink { get; set; } = string.Empty;

    /// <summary>
    /// Thread ID this link belongs to
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// When the link was marked as used
    /// </summary>
    public DateTime UsedAt { get; set; }
}