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
            Value = "test value"
            // Note: IsDev removed - environment is handled automatically by API
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConfigRequest>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Key.Should().Be(original.Key);
        deserialized.Value.Should().Be(original.Value);
        // IsDev is no longer part of requests
    }

    [Fact]
    public void ConfigResponse_Should_Serialize_And_Deserialize_Correctly()
    {
        // Arrange
        var original = new ConfigResponse
        {
            Id = 123,
            Key = "test.key",
            Value = "test value"
            // Note: IsDev removed - responses only include current environment configs
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConfigResponse>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.Key.Should().Be(original.Key);
        deserialized.Value.Should().Be(original.Value);
        // IsDev is no longer included in responses
    }

    [Fact]
    public void ConfigUpdateRequest_Should_Serialize_And_Deserialize_Correctly()
    {
        // Arrange
        var original = new ConfigUpdateRequest
        {
            Key = "updated.key",
            Value = "updated value"
            // Note: IsDev removed - environment cannot be changed after creation
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConfigUpdateRequest>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Key.Should().Be(original.Key);
        deserialized.Value.Should().Be(original.Value);
        // IsDev is no longer part of update requests
    }

    [Fact]
    public void ConfigsDto_Should_Serialize_And_Deserialize_Correctly()
    {
        // Arrange
        var original = new ConfigsDto
        {
            Id = 456,
            Key = "legacy.key",
            Value = "legacy value"
            // Note: IsDev removed - environment handled automatically by API
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConfigsDto>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.Key.Should().Be(original.Key);
        deserialized.Value.Should().Be(original.Value);
        // IsDev assertion removed - no longer part of DTO
    }

    [Fact]
    public void ConfigResponse_To_ConfigsDto_Mapping_Should_Work()
    {
        // Arrange
        var configResponse = new ConfigResponse
        {
            Id = 789,
            Key = "mapping.test",
            Value = "mapping value"
            // Note: IsDev removed from shared DTOs - handled by environment filtering
        };

        // Act - Simulate the mapping done in ModernFileCategorizationService
        var configsDto = new ConfigsDto
        {
            Id = configResponse.Id,
            Key = configResponse.Key,
            Value = configResponse.Value
            // Note: IsDev mapping removed as it's no longer in ConfigResponse
        };

        // Assert
        configsDto.Id.Should().Be(configResponse.Id);
        configsDto.Key.Should().Be(configResponse.Key);
        configsDto.Value.Should().Be(configResponse.Value);
        // IsDev comparison removed
    }

    [Fact]
    public void ConfigsDto_To_ConfigRequest_Mapping_Should_Work()
    {
        // Arrange
        var configsDto = new ConfigsDto
        {
            Id = 999, // Should be ignored in request
            Key = "create.test",
            Value = "create value"
            // Note: IsDev removed from ConfigsDto - environment handled automatically
        };

        // Act - Simulate the mapping done in ModernFileCategorizationService
        var configRequest = new ConfigRequest
        {
            Key = configsDto.Key ?? string.Empty,
            Value = configsDto.Value ?? string.Empty
            // Note: IsDev no longer sent in requests - handled automatically by API
        };

        // Assert
        configRequest.Key.Should().Be(configsDto.Key);
        configRequest.Value.Should().Be(configsDto.Value);
        // IsDev assertion removed - no longer part of API contract
    }

    [Fact]
    public void ConfigsDto_To_ConfigUpdateRequest_Mapping_Should_Work()
    {
        // Arrange
        var configsDto = new ConfigsDto
        {
            Id = 888,
            Key = "update.test",
            Value = "update value"
            // Note: IsDev removed from ConfigsDto - environment handled automatically
        };

        // Act - Simulate the mapping done in ModernFileCategorizationService
        var updateRequest = new ConfigUpdateRequest
        {
            Key = configsDto.Key,
            Value = configsDto.Value
            // Note: IsDev no longer sent in update requests - environment cannot be changed
        };

        // Assert
        updateRequest.Key.Should().Be(configsDto.Key);
        updateRequest.Value.Should().Be(configsDto.Value);
        // IsDev assertion removed - no longer part of update API contract
    }
}