namespace FileCategorization_Shared.DTOs.Configuration;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for creating configuration settings.
/// Used for POST operations to create new configuration entries.
/// </summary>
public class ConfigRequest
{
    /// <summary>
    /// Gets or sets the key of the configuration.
    /// This is required and serves as the unique identifier for the configuration setting.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets the value of the configuration.
    /// This is required and contains the actual configuration data.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this configuration is for development environment.
    /// Defaults to false (production environment).
    /// </summary>
    public bool IsDev { get; set; } = false;
}