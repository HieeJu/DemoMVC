using DemoMVC.Data;
using DemoMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Authorization;

public sealed class ActionPermissionFilter(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext filterContext)
    {
        if (filterContext.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        var principal = filterContext.HttpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            filterContext.Result = new ChallengeResult();
            return;
        }

        if (filterContext.RouteData.Values["controller"]?.ToString() == "Account" &&
            filterContext.RouteData.Values["action"]?.ToString() == "Logout")
        {
            return;
        }

        if (principal.IsInRole("Admin"))
        {
            return;
        }

        var controller = filterContext.RouteData.Values["controller"]?.ToString();
        var action = filterContext.RouteData.Values["action"]?.ToString();
        if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
        {
            filterContext.Result = new ForbidResult();
            return;
        }

        var roleNames = await userManager.GetRolesAsync(
            await userManager.GetUserAsync(principal) ?? throw new InvalidOperationException("Authenticated user was not found."));
        var permitted = await context.RoleActionPermissions
            .AnyAsync(permission =>
                permission.ControllerAction.Controller == controller &&
                permission.ControllerAction.Action == action &&
                roleNames.Contains(permission.Role.Name!));

        if (!permitted)
        {
            filterContext.Result = new ForbidResult();
        }
    }
}
