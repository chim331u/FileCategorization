namespace FileCategorization_Web.Data.DTOs.Configuration;

/// <summary>
/// Represents a request Data Transfer Object (DTO) for updating configuration settings.
/// </summary>
public class ConfigUpdateRequest
{
    /// <summary>
    /// Gets or sets the key of the configuration.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the value of the configuration.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this configuration is for development environment.
    /// </summary>
    public bool? IsDev { get; set; }
}