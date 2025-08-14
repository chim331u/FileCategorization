using System.ComponentModel.DataAnnotations;

namespace FileCategorization_Api.Contracts.Actions;

/// <summary>
/// Request DTO for refresh files operation.
/// </summary>
public class RefreshFilesRequest
{
    /// <summary>
    /// Optional filter to process only files matching specific patterns.
    /// If null or empty, all files in the origin directory will be processed.
    /// </summary>
    public List<string>? FileExtensionFilters { get; set; }

    /// <summary>
    /// Maximum number of files to process in a single batch.
    /// Default is 100. Range: 10-1000.
    /// </summary>
    [Range(10, 1000, ErrorMessage = "Batch size must be between 10 and 1000")]
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Whether to force re-categorization of existing files.
    /// Default is false.
    /// </summary>
    public bool ForceRecategorization { get; set; } = false;
}