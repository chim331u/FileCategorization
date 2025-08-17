namespace FileCategorization_Shared.DTOs.Configuration;

/// <summary>
/// Represents a response Data Transfer Object (DTO) for configuration settings.
/// Used for GET operations and as response from POST/PUT operations.
/// </summary>
public class ConfigResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the configuration.
    /// This is assigned by the system when the configuration is created.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the key of the configuration.
    /// This serves as the unique identifier for the configuration setting.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets the value of the configuration.
    /// This contains the actual configuration data.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this configuration is for development environment.
    /// True for development configurations, false for production configurations.
    /// </summary>
    public bool IsDev { get; set; }
}