using Microsoft.AspNetCore.Identity;

namespace FileCategorization_Api.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}