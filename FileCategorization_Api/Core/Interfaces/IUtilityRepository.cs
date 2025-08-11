using FileCategorization_Api.Core.Common;

namespace FileCategorization_Api.Core.Interfaces;

/// <summary>
/// Defines the contract for utility repository operations.
/// </summary>
public interface IUtilityRepository
{
    /// <summary>
    /// Encrypts a plain text string.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the encrypted string.</returns>
    Task<Result<string>> EncryptStringAsync(string plainText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    /// <param name="encryptedText">The encrypted text to decrypt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the decrypted string.</returns>
    Task<Result<string>> DecryptStringAsync(string encryptedText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the SHA256 hash of a string.
    /// </summary>
    /// <param name="text">The text to hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the hashed string.</returns>
    Task<Result<string>> HashStringAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a plain text against a SHA256 hash.
    /// </summary>
    /// <param name="plainText">The plain text to verify.</param>
    /// <param name="hash">The hash to verify against.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the verification result.</returns>
    Task<Result<bool>> VerifyHashAsync(string plainText, string hash, CancellationToken cancellationToken = default);
}