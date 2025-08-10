using System.ComponentModel.DataAnnotations;

namespace FileCategorization_Api.Contracts.Identity;

public class LoginModelDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}