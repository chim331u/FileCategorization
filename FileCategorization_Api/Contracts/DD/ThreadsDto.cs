namespace FileCategorization_Api.Contracts.DD;

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
    /// Gets or sets the number of links associated with the thread.
    /// </summary>
    public int LinksNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the thread has new links.
    /// </summary>
    public bool NewLinks { get; set; }
}