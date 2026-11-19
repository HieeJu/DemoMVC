using Microsoft.AspNetCore.Identity;

namespace DemoMVC.Models;

public class ApplicationRole : IdentityRole
{
    public bool IsSystemRole { get; set; }
}
