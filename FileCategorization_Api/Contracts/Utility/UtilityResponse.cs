namespace FileCategorization_Api.Contracts.Utility;

/// <summary>
/// Represents a generic response for utility operations.
/// </summary>
/// <typeparam name="T">The type of the result data.</typeparam>
public class UtilityResponse<T>
{
    /// <summary>
    /// Gets or sets the result data.
    /// </summary>
    public required T Result { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional message describing the operation result.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Represents a string-specific utility response.
/// </summary>
public class StringUtilityResponse : UtilityResponse<string>
{
}

/// <summary>
/// Represents a boolean-specific utility response.
/// </summary>
public class BooleanUtilityResponse : UtilityResponse<bool>
{
}