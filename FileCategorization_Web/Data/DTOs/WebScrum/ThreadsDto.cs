namespace FileCategorization_Web.Data.DTOs.WebScrum;

/// <summary>
/// Represents a Data Transfer Object (DTO) for threads.
/// </summary>
public class ThreadsDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the thread.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the main title of the thread.
    /// </summary>
    public string? MainTitle { get; set; }

    /// <summary>
    /// Gets or sets the type of the thread.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the thread URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of links associated with the thread.
    /// </summary>
    public int LinksNumber { get; set; }

    /// <summary>
    /// Gets or sets the total number of links.
    /// </summary>
    public int LinksCount { get; set; }

    /// <summary>
    /// Gets or sets the number of new links.
    /// </summary>
    public int NewLinksCount { get; set; }

    /// <summary>
    /// Gets or sets the number of used links.
    /// </summary>
    public int UsedLinksCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the thread has new links.
    /// </summary>
    public bool NewLinks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the thread has new links.
    /// </summary>
    public bool HasNewLinks { get; set; }

    /// <summary>
    /// Gets or sets the thread creation date.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the last update date.
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }
}