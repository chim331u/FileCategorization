namespace FileCategorization_Api.Contracts.Utility;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for string encryption.
/// </summary>
public class EncryptStringRequest
{
    /// <summary>
    /// Gets or sets the plain text to be encrypted.
    /// </summary>
    public required string PlainText { get; set; }
}