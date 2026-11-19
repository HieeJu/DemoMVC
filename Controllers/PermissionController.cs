using DemoMVC.Data;
using DemoMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers;

public sealed class PermissionController(
    ApplicationDbContext context,
    RoleManager<ApplicationRole> roleManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var roles = await roleManager.Roles.OrderBy(role => role.Name).ToListAsync();
        var actions = await context.ControllerActions
            .Include(action => action.RolePermissions)
            .OrderBy(action => action.Controller)
            .ThenBy(action => action.Action)
            .ToListAsync();
        var model = new PermissionMatrixViewModel
        {
            Roles = roles.Select(role => new PermissionRoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty
            }).ToList(),
            Actions = actions.Select(action => new PermissionActionViewModel
            {
                Id = action.Id,
                Controller = action.Controller,
                Action = action.Action,
                DisplayName = action.DisplayName,
                IsPublic = action.IsPublic,
                GrantedRoleIds = action.RolePermissions.Select(permission => permission.RoleId).ToList()
            }).ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UpdatePermissionsViewModel model)
    {
        var roles = await roleManager.Roles.ToListAsync();
        var manageableRoleIds = roles.Where(role => role.Name != "Admin").Select(role => role.Id).ToHashSet();
        var validActionIds = await context.ControllerActions.Select(action => action.Id).ToHashSetAsync();
        var requested = new HashSet<(string RoleId, int ActionId)>();

        foreach (var grantKey in model.GrantKeys.Distinct())
        {
            var separator = grantKey.LastIndexOf('|');
            if (separator <= 0 ||
                !int.TryParse(grantKey[(separator + 1)..], out var actionId) ||
                !manageableRoleIds.Contains(grantKey[..separator]) ||
                !validActionIds.Contains(actionId))
            {
                return BadRequest(new { message = "Dữ liệu phân quyền không hợp lệ." });
            }
            requested.Add((grantKey[..separator], actionId));
        }

        var existing = await context.RoleActionPermissions
            .Where(permission => manageableRoleIds.Contains(permission.RoleId))
            .ToListAsync();
        context.RoleActionPermissions.RemoveRange(existing.Where(permission =>
            !requested.Contains((permission.RoleId, permission.ControllerActionId))));

        var existingKeys = existing.Select(permission => (permission.RoleId, permission.ControllerActionId)).ToHashSet();
        foreach (var (roleId, actionId) in requested.Where(key => !existingKeys.Contains(key)))
        {
            context.RoleActionPermissions.Add(new RoleActionPermission
            {
                RoleId = roleId,
                ControllerActionId = actionId
            });
        }

        await context.SaveChangesAsync();
        return Json(new { success = true, message = "Đã lưu ma trận phân quyền." });
    }
}
