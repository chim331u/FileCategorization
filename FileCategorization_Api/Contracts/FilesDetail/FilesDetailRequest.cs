namespace FileCategorization_Api.Contracts.FilesDetail;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for file details.
/// </summary>
public class FilesDetailRequest
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
    /// Gets or sets the category of the file.
    /// </summary>
    public string? FileCategory { get; set; }
}