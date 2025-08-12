namespace FileCategorization_Api.Domain.Enums;

/// <summary>
/// Enumeration representing different file filter types.
/// </summary>
public enum FileFilterType
{
    /// <summary>
    /// All files regardless of status.
    /// </summary>
    All = 1,

    /// <summary>
    /// Files that have been categorized.
    /// </summary>
    Categorized = 2,

    /// <summary>
    /// Files that need to be categorized.
    /// </summary>
    ToCategorize = 3,

    /// <summary>
    /// Files that are marked as new.
    /// </summary>
    New = 4
}