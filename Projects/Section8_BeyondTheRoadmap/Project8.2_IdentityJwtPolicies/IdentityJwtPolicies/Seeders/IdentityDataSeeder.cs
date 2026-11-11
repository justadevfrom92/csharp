using System.Security.Claims;
using IdentityJwtPolicies.Constants;
using Microsoft.AspNetCore.Identity;

namespace IdentityJwtPolicies.Seeders;

// Creates the roles and one admin user the first time the app runs.
public static class IdentityDataSeeder
{
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "Admin123!x";

    public static async Task SeedAsync(IServiceProvider services)
    {
        RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        UserManager<IdentityUser> userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        foreach (string roleName in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        if (await userManager.FindByEmailAsync(AdminEmail) == null)
        {
            var admin = new IdentityUser { UserName = AdminEmail, Email = AdminEmail };
            await userManager.CreateAsync(admin, AdminPassword);
            await userManager.AddToRoleAsync(admin, "Admin");
            await userManager.AddClaimAsync(admin, new Claim(AppClaimTypeConstants.Department, "it"));
            // No birth_year claim on purpose: the AtLeast18 policy must refuse users it can't check.
        }
    }
}
