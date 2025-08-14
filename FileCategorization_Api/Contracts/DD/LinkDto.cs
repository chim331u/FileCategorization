namespace FileCategorization_Api.Contracts.DD;

/// <summary>
/// DTO for ED2K link information
/// </summary>
public class LinkDto
{
    /// <summary>
    /// Link ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Link title/filename
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The ED2K link URL
    /// </summary>
    public string Ed2kLink { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a new link
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// Whether this link has been used
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Thread ID this link belongs to
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// Link creation date
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }
}