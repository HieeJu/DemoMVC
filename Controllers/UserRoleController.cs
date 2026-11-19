using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers;

public sealed class UserRoleController(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await BuildListAsync());

    [HttpGet]
    public async Task<IActionResult> Rows() => PartialView("_Rows", await BuildListAsync());

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var assignedRoles = await userManager.GetRolesAsync(user);
        var roles = await roleManager.Roles.OrderBy(role => role.Name).ToListAsync();
        var model = new UserRoleEditViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? user.UserName ?? string.Empty,
            Roles = roles.Select(role => new RoleChoiceViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Selected = assignedRoles.Contains(role.Name!)
            }).ToList()
        };
        model.SelectedRoleIds = model.Roles.Where(role => role.Selected).Select(role => role.Id).ToList();
        return PartialView("_Edit", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserRoleEditViewModel model)
    {
        var user = await userManager.FindByIdAsync(model.UserId);
        if (user is null) return NotFound();

        var allRoles = await roleManager.Roles.ToListAsync();
        if (model.SelectedRoleIds.Except(allRoles.Select(role => role.Id)).Any())
        {
            return BadRequest(new { message = "Danh sách Role không hợp lệ." });
        }

        var currentNames = await userManager.GetRolesAsync(user);
        var selectedNames = allRoles
            .Where(role => model.SelectedRoleIds.Contains(role.Id))
            .Select(role => role.Name!)
            .ToList();

        var removeResult = await userManager.RemoveFromRolesAsync(user, currentNames.Except(selectedNames));
        if (!removeResult.Succeeded)
        {
            return BadRequest(new { message = string.Join("; ", removeResult.Errors.Select(error => error.Description)) });
        }

        var addResult = await userManager.AddToRolesAsync(user, selectedNames.Except(currentNames));
        if (!addResult.Succeeded)
        {
            return BadRequest(new { message = string.Join("; ", addResult.Errors.Select(error => error.Description)) });
        }
        return Json(new { success = true, message = "Đã cập nhật Role cho User." });
    }

    private async Task<UserRoleListViewModel> BuildListAsync()
    {
        var result = new UserRoleListViewModel();
        var users = await userManager.Users.OrderBy(user => user.Email).ToListAsync();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Users.Add(new UserRoleRowViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                Roles = string.Join(", ", roles)
            });
        }
        return result;
    }
}
