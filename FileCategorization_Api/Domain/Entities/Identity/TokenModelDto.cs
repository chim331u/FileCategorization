namespace FileCategorization_Api.Domain.Entities.Identity;

/// <summary>
/// Data transfer object for token operations.
/// </summary>
public class TokenModelDto
{
    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }
}