namespace DemoMVC.Models;

public class RoleActionPermission
{
    public string RoleId { get; set; } = string.Empty;
    public ApplicationRole Role { get; set; } = null!;

    public int ControllerActionId { get; set; }
    public ControllerAction ControllerAction { get; set; } = null!;
}
