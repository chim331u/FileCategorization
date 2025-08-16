namespace FileCategorization_Shared.Enums;

/// <summary>
/// Represents the result of a file move operation.
/// </summary>
public enum MoveFilesResults
{
    /// <summary>
    /// Indicates that the file was successfully moved.
    /// </summary>
    Moved = 0,

    /// <summary>
    /// Indicates that the file move operation failed.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// Indicates that the file ID was not present.
    /// </summary>
    IdNotPresent = 2,

    /// <summary>
    /// Indicates that the file move operation was completed.
    /// </summary>
    Completed = 3
}