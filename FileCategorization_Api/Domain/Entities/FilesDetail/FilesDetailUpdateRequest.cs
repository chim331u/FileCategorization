using FileCategorization_Api.Domain.Entities.FilesDetail;
namespace FileCategorization_Api.Domain.Entities.FilesDetail;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for updating file details.
/// </summary>
public class FilesDetailUpdateRequest
{
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the path of the file.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public double FileSize { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp of the file.
    /// </summary>
    public DateTime LastUpdateFile { get; set; }

    /// <summary>
    /// Gets or sets the category of the file.
    /// </summary>
    public string? FileCategory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the file needs to be categorized.
    /// </summary>
    public bool IsToCategorize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the file is new.
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the file is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the file should not be moved.
    /// </summary>
    public bool IsNotToMove { get; set; }
}