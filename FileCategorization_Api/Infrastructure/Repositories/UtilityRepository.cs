using FileCategorization_Api.Common;
using FileCategorization_Api.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace FileCategorization_Api.Infrastructure.Repositories;

/// <summary>
/// Implementation of utility repository for cryptographic operations.
/// </summary>
public class UtilityRepository : IUtilityRepository
{
    private readonly ILogger<UtilityRepository> _logger;
    private readonly IConfiguration _configuration;
    private static readonly byte[] _key = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes
    private static readonly byte[] _iv = Encoding.UTF8.GetBytes("1234567890123456"); // 16 bytes

    /// <summary>
    /// Initializes a new instance of the UtilityRepository class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The configuration instance.</param>
    public UtilityRepository(ILogger<UtilityRepository> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc/>
    public async Task<Result<string>> EncryptStringAsync(string plainText, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting string encryption");

            if (string.IsNullOrEmpty(plainText))
            {
                _logger.LogWarning("Attempted to encrypt null or empty string");
                return Result<string>.Failure("Plain text cannot be null or empty");
            }

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);
            
            await swEncrypt.WriteAsync(plainText.AsMemory(), cancellationToken);
            swEncrypt.Close();

            var encrypted = msEncrypt.ToArray();
            var result = Convert.ToBase64String(encrypted);

            _logger.LogInformation("String encryption completed successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during string encryption");
            return Result<string>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<string>> DecryptStringAsync(string encryptedText, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting string decryption");

            if (string.IsNullOrEmpty(encryptedText))
            {
                _logger.LogWarning("Attempted to decrypt null or empty string");
                return Result<string>.Failure("Encrypted text cannot be null or empty");
            }

            byte[] cipherText;
            try
            {
                cipherText = Convert.FromBase64String(encryptedText);
            }
            catch (FormatException)
            {
                _logger.LogWarning("Invalid Base64 format for encrypted text");
                return Result<string>.Failure("Invalid encrypted text format");
            }

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherText);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            var result = await srDecrypt.ReadToEndAsync(cancellationToken);

            _logger.LogInformation("String decryption completed successfully");
            return Result<string>.Success(result);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during decryption");
            return Result<string>.Failure("Failed to decrypt text. Invalid encrypted data or key.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during string decryption");
            return Result<string>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<string>> HashStringAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting string hashing");

            if (string.IsNullOrEmpty(text))
            {
                _logger.LogWarning("Attempted to hash null or empty string");
                return Result<string>.Failure("Text cannot be null or empty");
            }

            using var sha256 = SHA256.Create();
            var inputBytes = Encoding.UTF8.GetBytes(text);
            var hashBytes = await Task.Run(() => sha256.ComputeHash(inputBytes), cancellationToken);
            var result = Convert.ToHexString(hashBytes).ToLowerInvariant();

            _logger.LogInformation("String hashing completed successfully");
            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during string hashing");
            return Result<string>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> VerifyHashAsync(string plainText, string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting hash verification");

            if (string.IsNullOrEmpty(plainText))
            {
                _logger.LogWarning("Attempted to verify null or empty plain text");
                return Result<bool>.Failure("Plain text cannot be null or empty");
            }

            if (string.IsNullOrEmpty(hash))
            {
                _logger.LogWarning("Attempted to verify against null or empty hash");
                return Result<bool>.Failure("Hash cannot be null or empty");
            }

            var computedHashResult = await HashStringAsync(plainText, cancellationToken);
            if (computedHashResult.IsFailure)
            {
                return Result<bool>.Failure($"Failed to compute hash for verification: {computedHashResult.ErrorMessage}");
            }

            var result = string.Equals(computedHashResult.Data, hash.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation("Hash verification completed successfully. Result: {IsMatch}", result);
            return Result<bool>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during hash verification");
            return Result<bool>.FromException(ex);
        }
    }
}