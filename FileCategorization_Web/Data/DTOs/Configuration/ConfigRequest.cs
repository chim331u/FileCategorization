namespace FileCategorization_Web.Data.DTOs.Configuration;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for creating configuration settings.
/// </summary>
public class ConfigRequest
{
    /// <summary>
    /// Gets or sets the key of the configuration.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets the value of the configuration.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this configuration is for development environment.
    /// </summary>
    public bool IsDev { get; set; } = false;
}