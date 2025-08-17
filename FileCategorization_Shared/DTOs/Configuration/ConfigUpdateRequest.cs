namespace FileCategorization_Shared.DTOs.Configuration;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for updating configuration settings.
/// Used for PUT operations to update existing configuration entries.
/// All properties are nullable to support partial updates.
/// Environment (development/production) cannot be changed after creation.
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
}