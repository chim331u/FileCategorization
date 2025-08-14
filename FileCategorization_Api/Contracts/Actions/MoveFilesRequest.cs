using System.ComponentModel.DataAnnotations;
using FileCategorization_Api.Domain.Entities.FilesDetail;

namespace FileCategorization_Api.Contracts.Actions;

/// <summary>
/// Request DTO for move files operation with enhanced validation.
/// </summary>
public class MoveFilesRequest
{
    /// <summary>
    /// List of files to move with their target categories.
    /// Maximum 1000 files per request.
    /// </summary>
    [Required(ErrorMessage = "Files to move cannot be null or empty")]
    [MinLength(1, ErrorMessage = "At least one file must be specified")]
    [MaxLength(1000, ErrorMessage = "Maximum 1000 files can be moved in a single request")]
    public List<FileMoveDto> FilesToMove { get; set; } = new();

    /// <summary>
    /// Whether to continue processing remaining files if some fail.
    /// Default is true.
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Whether to validate that target categories exist before moving.
    /// Default is true.
    /// </summary>
    public bool ValidateCategories { get; set; } = true;

    /// <summary>
    /// Whether to create target directories if they don't exist.
    /// Default is true.
    /// </summary>
    public bool CreateDirectories { get; set; } = true;
}