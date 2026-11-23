using Microsoft.AspNetCore.Identity;

namespace Recrutement.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = true;
}