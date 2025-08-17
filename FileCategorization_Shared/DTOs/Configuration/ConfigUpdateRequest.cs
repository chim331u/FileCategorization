namespace FileCategorization_Shared.DTOs.Configuration;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for updating configuration settings.
/// Used for PUT operations to update existing configuration entries.
/// All properties are nullable to support partial updates.
/// </summary>
public class ConfigUpdateRequest
{
    /// <summary>
    /// Gets or sets the key of the configuration.
    /// Optional - if provided, the configuration key will be updated.
    /// Must be unique if specified.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the value of the configuration.
    /// Optional - if provided, the configuration value will be updated.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this configuration is for development environment.
    /// Optional - if provided, the environment setting will be updated.
    /// True for development, false for production.
    /// </summary>
    public bool? IsDev { get; set; }
}