namespace FileCategorization_Api.Domain.Entities.Identity;

/// <summary>
/// Data transfer object for user registration.
/// </summary>
public class SignupModelDto
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the user's name.
    /// </summary>
    public string? Name { get; set; }
}