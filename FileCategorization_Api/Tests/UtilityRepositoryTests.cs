using FileCategorization_Api.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FileCategorization_Api.Tests;

/// <summary>
/// Unit tests for UtilityRepository.
/// </summary>
public class UtilityRepositoryTests
{
    private readonly Mock<ILogger<UtilityRepository>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly UtilityRepository _repository;

    /// <summary>
    /// Initializes a new instance of the UtilityRepositoryTests class.
    /// </summary>
    public UtilityRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<UtilityRepository>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _repository = new UtilityRepository(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task EncryptStringAsync_WithValidText_ReturnsEncryptedString()
    {
        // Arrange
        var plainText = "Hello World";

        // Act
        var result = await _repository.EncryptStringAsync(plainText);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data);
        Assert.NotEqual(plainText, result.Data);
    }

    [Fact]
    public async Task EncryptStringAsync_WithEmptyText_ReturnsFailure()
    {
        // Arrange
        var plainText = "";

        // Act
        var result = await _repository.EncryptStringAsync(plainText);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Plain text cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task DecryptStringAsync_WithValidEncryptedText_ReturnsOriginalText()
    {
        // Arrange
        var originalText = "Hello World";
        var encryptResult = await _repository.EncryptStringAsync(originalText);
        Assert.True(encryptResult.IsSuccess);

        // Act
        var decryptResult = await _repository.DecryptStringAsync(encryptResult.Data!);

        // Assert
        Assert.True(decryptResult.IsSuccess);
        Assert.Equal(originalText, decryptResult.Data);
    }

    [Fact]
    public async Task DecryptStringAsync_WithInvalidText_ReturnsFailure()
    {
        // Arrange
        var invalidEncryptedText = "invalid-base64!";

        // Act
        var result = await _repository.DecryptStringAsync(invalidEncryptedText);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid encrypted text format", result.ErrorMessage);
    }

    [Fact]
    public async Task HashStringAsync_WithValidText_ReturnsValidHash()
    {
        // Arrange
        var text = "Hello World";

        // Act
        var result = await _repository.HashStringAsync(text);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(64, result.Data.Length); // SHA256 hash is 64 characters
        Assert.Matches("^[a-f0-9]{64}$", result.Data); // Valid hex format
    }

    [Fact]
    public async Task HashStringAsync_WithEmptyText_ReturnsFailure()
    {
        // Arrange
        var text = "";

        // Act
        var result = await _repository.HashStringAsync(text);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Text cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task VerifyHashAsync_WithMatchingHash_ReturnsTrue()
    {
        // Arrange
        var text = "Hello World";
        var hashResult = await _repository.HashStringAsync(text);
        Assert.True(hashResult.IsSuccess);

        // Act
        var verifyResult = await _repository.VerifyHashAsync(text, hashResult.Data!);

        // Assert
        Assert.True(verifyResult.IsSuccess);
        Assert.True(verifyResult.Data);
    }

    [Fact]
    public async Task VerifyHashAsync_WithNonMatchingHash_ReturnsFalse()
    {
        // Arrange
        var text = "Hello World";
        var differentText = "Different Text";
        var hashResult = await _repository.HashStringAsync(differentText);
        Assert.True(hashResult.IsSuccess);

        // Act
        var verifyResult = await _repository.VerifyHashAsync(text, hashResult.Data!);

        // Assert
        Assert.True(verifyResult.IsSuccess);
        Assert.False(verifyResult.Data);
    }

    [Fact]
    public async Task VerifyHashAsync_WithEmptyPlainText_ReturnsFailure()
    {
        // Arrange
        var plainText = "";
        var hash = "somehash";

        // Act
        var result = await _repository.VerifyHashAsync(plainText, hash);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Plain text cannot be null or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task VerifyHashAsync_WithEmptyHash_ReturnsFailure()
    {
        // Arrange
        var plainText = "Hello World";
        var hash = "";

        // Act
        var result = await _repository.VerifyHashAsync(plainText, hash);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Hash cannot be null or empty", result.ErrorMessage);
    }
}