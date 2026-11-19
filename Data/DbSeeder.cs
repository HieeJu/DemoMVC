using DemoMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DemoMVC.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IActionDescriptorCollectionProvider actionProvider,
            IConfiguration configuration,
            ILogger logger)
        {
            var faculties = new[]
            {
                new Faculty { FacultyId = "CNTT", FacultyName = "Công nghệ Thông tin", MajorCode = "01" },
                new Faculty { FacultyId = "DDK", FacultyName = "Dầu khí & Năng lượng", MajorCode = "02" },
                new Faculty { FacultyId = "M", FacultyName = "Mỏ", MajorCode = "03" },
                new Faculty { FacultyId = "TD", FacultyName = "Trắc địa - Bản đồ & Quản lý đất đai", MajorCode = "04" },
                new Faculty { FacultyId = "KT", FacultyName = "Kinh tế & Quản trị kinh doanh", MajorCode = "05" },
                new Faculty { FacultyId = "MT", FacultyName = "Môi trường", MajorCode = "06" },
                new Faculty { FacultyId = "XD", FacultyName = "Xây dựng", MajorCode = "07" }
            };

            foreach (var faculty in faculties)
            {
                if (!await context.Faculties.AnyAsync(existing => existing.FacultyId == faculty.FacultyId))
                {
                    context.Faculties.Add(faculty);
                }
            }

            await context.SaveChangesAsync();

            await EnsureRoleAsync(roleManager, "Admin", isSystem: true);
            await EnsureRoleAsync(roleManager, "User", isSystem: true);
            await SeedAdministratorAsync(userManager, configuration, logger);

            var knownActions = actionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Select(descriptor => new ControllerAction
                {
                    Controller = descriptor.ControllerName,
                    Action = descriptor.ActionName,
                    DisplayName = $"{descriptor.ControllerName}.{descriptor.ActionName}",
                    IsPublic = descriptor.EndpointMetadata.OfType<Microsoft.AspNetCore.Authorization.IAllowAnonymous>().Any() ||
                               (descriptor.ControllerName == "Account" && descriptor.ActionName == "Logout")
                })
                .GroupBy(item => new { item.Controller, item.Action })
                .Select(group => group.First())
                .ToList();
            var knownActionKeys = knownActions
                .Select(action => $"{action.Controller}\0{action.Action}")
                .ToHashSet(StringComparer.Ordinal);
            var staleActions = (await context.ControllerActions.ToListAsync())
                .Where(action => !knownActionKeys.Contains($"{action.Controller}\0{action.Action}"));
            context.ControllerActions.RemoveRange(staleActions);

            foreach (var knownAction in knownActions)
            {
                var existing = await context.ControllerActions.FirstOrDefaultAsync(item =>
                    item.Controller == knownAction.Controller && item.Action == knownAction.Action);
                if (existing is null)
                {
                    context.ControllerActions.Add(knownAction);
                }
                else
                {
                    existing.DisplayName = knownAction.DisplayName;
                    existing.IsPublic = knownAction.IsPublic;
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task EnsureRoleAsync(
            RoleManager<ApplicationRole> roleManager,
            string roleName,
            bool isSystem)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                var result = await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName,
                    IsSystemRole = isSystem
                });
                EnsureSucceeded(result, $"creating role {roleName}");
            }
            else if (isSystem && !role.IsSystemRole)
            {
                role.IsSystemRole = true;
                EnsureSucceeded(await roleManager.UpdateAsync(role), $"protecting role {roleName}");
            }
        }

        private static async Task SeedAdministratorAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            var email = configuration["Identity:AdminEmail"];
            var password = configuration["Identity:AdminPassword"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogError("Admin seed was skipped. Configure Identity:AdminEmail and Identity:AdminPassword using a secret store.");
                return;
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(user, password);
                EnsureSucceeded(createResult, "creating the configured administrator");
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                EnsureSucceeded(await userManager.UpdateAsync(user), "confirming the configured administrator");
            }

            if (!await userManager.IsInRoleAsync(user, "Admin"))
            {
                EnsureSucceeded(await userManager.AddToRoleAsync(user, "Admin"), "assigning the Admin role");
            }
        }

        private static void EnsureSucceeded(IdentityResult result, string operation)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Identity failed while {operation}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
            }
        }
    }
}