namespace FileCategorization_Web.Data.DTOs.Configuration;

/// <summary>
/// Represents a response Data Transfer Object (DTO) for configuration settings from API v2.
/// </summary>
public class ConfigResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the configuration.
    /// </summary>
    public int Id { get; set; }

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
    public bool IsDev { get; set; }
}