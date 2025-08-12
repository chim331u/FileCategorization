using Microsoft.AspNetCore.Identity;

namespace FileCategorization_Api.Domain.Entities.Identity;

public class ApplicationUser : IdentityUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}