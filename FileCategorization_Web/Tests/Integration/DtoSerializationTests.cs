using System.Text.Json;
using FileCategorization_Shared.DTOs.Configuration;
using Xunit;
using FluentAssertions;

namespace FileCategorization_Web.Tests.Integration;

/// <summary>
/// Integration tests for DTO serialization/deserialization.
/// Verifies that all DTOs can be properly serialized to/from JSON.
/// </summary>
public class DtoSerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public DtoSerializationTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public void ConfigRequest_Should_Serialize_And_Deserialize_Correctly()
    {
        // Arrange
        var original = new ConfigRequest
        {
            Key = "test.key",
            Value = "test value",
            IsDev = true
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConfigRequest>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Key.Should().Be(original.Key);
        deserialized.Value.Should().Be(original.Value);
        deserialized.IsDev.Should().Be(original.IsDev);
    }

    [Fact]
    public void ConfigResponse_Should_Serialize_And_Deserialize_Correctly()
    {
        // Arrange
        var original = new ConfigResponse
        {
            Id = 123,
            Key = "test.key",
            Value = "test value",
            IsDev = false
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConfigResponse>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.Key.Should().Be(original.Key);
        deserialized.Value.Should().Be(original.Value);
        deserialized.IsDev.Should().Be(original.IsDev);
    }

    [Fact]
    public void ConfigUpdateRequest_Should_Serialize_And_Deserialize_Correctly()
    {
        // Arrange
        var original = new ConfigUpdateRequest
        {
            Key = "updated.key",
            Value = "updated value",
            IsDev = null // Test nullable property
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConfigUpdateRequest>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Key.Should().Be(original.Key);
        deserialized.Value.Should().Be(original.Value);
        deserialized.IsDev.Should().BeNull();
    }

    [Fact]
    public void ConfigsDto_Should_Serialize_And_Deserialize_Correctly()
    {
        // Arrange
        var original = new ConfigsDto
        {
            Id = 456,
            Key = "legacy.key",
            Value = "legacy value",
            IsDev = true
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConfigsDto>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.Key.Should().Be(original.Key);
        deserialized.Value.Should().Be(original.Value);
        deserialized.IsDev.Should().Be(original.IsDev);
    }

    [Fact]
    public void ConfigResponse_To_ConfigsDto_Mapping_Should_Work()
    {
        // Arrange
        var configResponse = new ConfigResponse
        {
            Id = 789,
            Key = "mapping.test",
            Value = "mapping value",
            IsDev = false
        };

        // Act - Simulate the mapping done in ModernFileCategorizationService
        var configsDto = new ConfigsDto
        {
            Id = configResponse.Id,
            Key = configResponse.Key,
            Value = configResponse.Value,
            IsDev = configResponse.IsDev
        };

        // Assert
        configsDto.Id.Should().Be(configResponse.Id);
        configsDto.Key.Should().Be(configResponse.Key);
        configsDto.Value.Should().Be(configResponse.Value);
        configsDto.IsDev.Should().Be(configResponse.IsDev);
    }

    [Fact]
    public void ConfigsDto_To_ConfigRequest_Mapping_Should_Work()
    {
        // Arrange
        var configsDto = new ConfigsDto
        {
            Id = 999, // Should be ignored in request
            Key = "create.test",
            Value = "create value",
            IsDev = true
        };

        // Act - Simulate the mapping done in ModernFileCategorizationService
        var configRequest = new ConfigRequest
        {
            Key = configsDto.Key ?? string.Empty,
            Value = configsDto.Value ?? string.Empty,
            IsDev = configsDto.IsDev
        };

        // Assert
        configRequest.Key.Should().Be(configsDto.Key);
        configRequest.Value.Should().Be(configsDto.Value);
        configRequest.IsDev.Should().Be(configsDto.IsDev);
    }

    [Fact]
    public void ConfigsDto_To_ConfigUpdateRequest_Mapping_Should_Work()
    {
        // Arrange
        var configsDto = new ConfigsDto
        {
            Id = 888,
            Key = "update.test",
            Value = "update value",
            IsDev = false
        };

        // Act - Simulate the mapping done in ModernFileCategorizationService
        var updateRequest = new ConfigUpdateRequest
        {
            Key = configsDto.Key,
            Value = configsDto.Value,
            IsDev = configsDto.IsDev
        };

        // Assert
        updateRequest.Key.Should().Be(configsDto.Key);
        updateRequest.Value.Should().Be(configsDto.Value);
        updateRequest.IsDev.Should().Be(configsDto.IsDev);
    }
}