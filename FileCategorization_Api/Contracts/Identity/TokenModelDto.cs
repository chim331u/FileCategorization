using System.ComponentModel.DataAnnotations;

namespace FileCategorization_Api.Contracts.Identity;

public class TokenModelDto
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}