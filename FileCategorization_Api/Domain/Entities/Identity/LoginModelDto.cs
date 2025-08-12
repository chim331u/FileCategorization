namespace FileCategorization_Api.Domain.Entities.Identity;

/// <summary>
/// Data transfer object for user login.
/// </summary>
public class LoginModelDto
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string? Password { get; set; }
}