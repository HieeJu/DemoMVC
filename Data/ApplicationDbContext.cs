using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using DemoMVC.Models;

namespace DemoMVC.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Faculty> Faculties => Set<Faculty>();
        public DbSet<ControllerAction> ControllerActions => Set<ControllerAction>();
        public DbSet<RoleActionPermission> RoleActionPermissions => Set<RoleActionPermission>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ControllerAction>()
                .HasIndex(action => new { action.Controller, action.Action })
                .IsUnique();

            builder.Entity<RoleActionPermission>()
                .HasKey(permission => new { permission.RoleId, permission.ControllerActionId });

            builder.Entity<RoleActionPermission>()
                .HasOne(permission => permission.Role)
                .WithMany()
                .HasForeignKey(permission => permission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RoleActionPermission>()
                .HasOne(permission => permission.ControllerAction)
                .WithMany(action => action.RolePermissions)
                .HasForeignKey(permission => permission.ControllerActionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}