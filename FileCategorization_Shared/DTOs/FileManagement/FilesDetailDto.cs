using System.ComponentModel.DataAnnotations;

namespace FileCategorization_Shared.DTOs.FileManagement;

/// <summary>
/// Represents a Data Transfer Object (DTO) for file details.
/// </summary>
public class FilesDetailDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the file.
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public double FileSize { get; set; }
    
    /// <summary>
    /// Gets or sets the category of the file.
    /// </summary>
    public string FileCategory { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether the file needs to be categorized.
    /// </summary>
    public bool IsToCategorize { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the file is new.
    /// </summary>
    public bool IsNew { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the file should not be moved.
    /// </summary>
    public bool IsNotToMove { get; set; }
}