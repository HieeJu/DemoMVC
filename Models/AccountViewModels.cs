using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 12)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 12)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class RoleEditViewModel
{
    public string? Id { get; set; }

    [Required, StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public sealed class UserRoleEditViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> SelectedRoleIds { get; set; } = [];
    public List<RoleChoiceViewModel> Roles { get; set; } = [];
}

public sealed class UserRoleListViewModel
{
    public List<UserRoleRowViewModel> Users { get; set; } = [];
}

public sealed class UserRoleRowViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
}

public sealed class RoleChoiceViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

public sealed class PermissionMatrixViewModel
{
    public List<PermissionRoleViewModel> Roles { get; set; } = [];
    public List<PermissionActionViewModel> Actions { get; set; } = [];
}

public sealed class PermissionRoleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class PermissionActionViewModel
{
    public int Id { get; set; }
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public List<string> GrantedRoleIds { get; set; } = [];
}

public sealed class UpdatePermissionsViewModel
{
    public List<string> GrantKeys { get; set; } = [];
}
