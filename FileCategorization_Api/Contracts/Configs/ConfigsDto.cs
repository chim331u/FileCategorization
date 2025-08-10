namespace FileCategorization_Api.Contracts.Configs;

/// <summary>
/// Represents a Data Transfer Object (DTO) for configuration settings.
/// </summary>
public class ConfigsDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the configuration.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the key of the configuration.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the value of the configuration.
    /// </summary>
    public string? Value { get; set; }
}