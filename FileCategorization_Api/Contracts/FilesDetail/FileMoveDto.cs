namespace FileCategorization_Api.Contracts.FilesDetail;

/// <summary>
/// Represents a Data Transfer Object (DTO) for moving files.
/// </summary>
public class FileMoveDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the file.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the category of the file.
    /// </summary>
    public string FileCategory { get; set; }
}