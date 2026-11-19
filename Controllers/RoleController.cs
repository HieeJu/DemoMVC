using DemoMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers;

public sealed class RoleController(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() =>
        View(await roleManager.Roles.OrderBy(role => role.Name).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Rows() =>
        PartialView("_Rows", await roleManager.Roles.OrderBy(role => role.Name).ToListAsync());

    [HttpGet]
    public IActionResult Create() => PartialView("_Form", new RoleEditViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleEditViewModel model)
    {
        if (!ModelState.IsValid) return InvalidPartial(model);

        var result = await roleManager.CreateAsync(new ApplicationRole { Name = model.Name.Trim() });
        if (!result.Succeeded)
        {
            AddErrors(result);
            return InvalidPartial(model);
        }
        return Json(new { success = true, message = "Đã tạo Role." });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null) return NotFound();
        if (role.IsSystemRole) return Conflict(new { message = "Không thể sửa Role hệ thống." });
        return PartialView("_Form", new RoleEditViewModel { Id = role.Id, Name = role.Name ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleEditViewModel model)
    {
        var role = string.IsNullOrWhiteSpace(model.Id) ? null : await roleManager.FindByIdAsync(model.Id);
        if (role is null) return NotFound();
        if (role.IsSystemRole) return Conflict(new { message = "Không thể sửa Role hệ thống." });
        if (!ModelState.IsValid) return InvalidPartial(model);

        role.Name = model.Name.Trim();
        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return InvalidPartial(model);
        }
        return Json(new { success = true, message = "Đã cập nhật Role." });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null) return NotFound();
        if (role.IsSystemRole) return Conflict(new { message = "Không thể xóa Role hệ thống." });
        return PartialView("_Delete", role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null) return NotFound();
        if (role.IsSystemRole) return Conflict(new { message = "Không thể xóa Role hệ thống." });
        if ((await userManager.GetUsersInRoleAsync(role.Name!)).Count > 0)
        {
            return Conflict(new { message = "Không thể xóa Role đang được gán cho User." });
        }

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join("; ", result.Errors.Select(error => error.Description)) });
        }
        return Json(new { success = true, message = "Đã xóa Role." });
    }

    private IActionResult InvalidPartial(RoleEditViewModel model)
    {
        Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return PartialView("_Form", model);
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
