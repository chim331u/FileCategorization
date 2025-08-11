namespace FileCategorization_Api.Contracts.Utility;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for string decryption.
/// </summary>
public class DecryptStringRequest
{
    /// <summary>
    /// Gets or sets the encrypted text to be decrypted.
    /// </summary>
    public required string EncryptedText { get; set; }
}