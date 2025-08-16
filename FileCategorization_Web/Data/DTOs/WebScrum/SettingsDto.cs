namespace FileCategorization_Web.Value.DTOs.WebScrum;

/// <summary>
/// Represents a Data Transfer Object (DTO) for settings.
/// </summary>
public class SettingDto  
{
    /// <summary>
    /// Gets or sets the unique identifier for the setting.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the username for the setting.
    /// </summary>
    public string Dd_User { get; set; }

    /// <summary>
    /// Gets or sets the password for the setting.
    /// </summary>
    public string Dd_Password { get; set; }

    /// <summary>
    /// Get or sets the NasAlias for the setting.
    /// </summary>
    public string? Alias { get; set; }
}