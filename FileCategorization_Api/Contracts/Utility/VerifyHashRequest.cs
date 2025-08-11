namespace FileCategorization_Api.Contracts.Utility;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for hash verification.
/// </summary>
public class VerifyHashRequest
{
    /// <summary>
    /// Gets or sets the plain text to verify.
    /// </summary>
    public required string PlainText { get; set; }

    /// <summary>
    /// Gets or sets the hash to verify against.
    /// </summary>
    public required string Hash { get; set; }
}