namespace FileCategorization_Api.Contracts.Utility;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for string hashing.
/// </summary>
public class HashStringRequest
{
    /// <summary>
    /// Gets or sets the text to be hashed.
    /// </summary>
    public required string Text { get; set; }
}