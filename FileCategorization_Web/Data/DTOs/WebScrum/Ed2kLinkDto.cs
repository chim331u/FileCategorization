namespace FileCategorization_Web.Data.DTOs.WebScrum;

/// <summary>
/// Represents a Data Transfer Object (DTO) for ED2K links.
/// </summary>
public class Ed2kLinkDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the ED2K link.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ED2K link.
    /// </summary>
    public string Ed2kLink { get; set; }

    /// <summary>
    /// Gets or sets the title associated with the ED2K link.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the ED2K link is new.
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the ED2K link has been used.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the thread associated with the ED2K link.
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// Gets or sets the link creation date.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the last update date.
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }
}