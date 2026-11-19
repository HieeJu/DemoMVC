using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models;

public class ControllerAction
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Controller { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsPublic { get; set; }

    public ICollection<RoleActionPermission> RolePermissions { get; set; } = [];
}
