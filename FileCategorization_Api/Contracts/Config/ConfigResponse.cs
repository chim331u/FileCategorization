namespace FileCategorization_Api.Contracts.Config;

/// <summary>
/// Represents a response Data Transfer Object (DTO) for configuration settings.
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

    /// <summary>
    /// Gets or sets the date when the configuration was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the date when the configuration was last updated.
    /// </summary>
    public DateTime LastUpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the configuration is active.
    /// </summary>
    public bool IsActive { get; set; }
}