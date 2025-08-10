namespace FileCategorization_Api.Interfaces;

/// <summary>
/// Defines utility services.
/// </summary>
public interface IUtilityServices
{
    /// <summary>
    /// Calculates the time difference between two dates.
    /// </summary>
    /// <param name="start">The start date and time.</param>
    /// <param name="end">The end date and time.</param>
    /// <returns>A string representing the time difference.</returns>
    Task<string> TimeDiff(DateTime start, DateTime end);

    /// <summary>
    /// Encrypts a plain text string.
    /// </summary>
    /// <param name="plainText">The plain text string to encrypt.</param>
    /// <returns>The encrypted string.</returns>
    Task<string> EncryptString(string plainText);

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    /// <param name="cipherText">The encrypted string to decrypt.</param>
    /// <returns>The decrypted string.</returns>
    Task<string> DecryptString(string cipherText);

    /// <summary>
    /// Hashes a string using the SHA256 algorithm.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>The hashed string.</returns>
    Task<string> HashString_SHA256(string input);

    /// <summary>
    /// Verifies a hash against an input string using the SHA256 algorithm.
    /// </summary>
    /// <param name="input">The input string to verify.</param>
    /// <param name="hash">The hash to compare against.</param>
    /// <returns>A boolean indicating whether the hash matches the input.</returns>
    Task<bool> VerifyHash_SHA256(string input, string hash);

    string GetLogFileText(string logFilePath);
}