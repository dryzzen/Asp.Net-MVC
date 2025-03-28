using Microsoft.AspNetCore.Identity;
using LeaveTracker.Models;

namespace LeaveTracker
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await EnsureRoleAsync(roleManager, "HR");
            await EnsureRoleAsync(roleManager, "User");

            await EnsureHrAccountAsync(userManager);
        }

        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        private static async Task EnsureHrAccountAsync(UserManager<ApplicationUser> userManager)
        {
            const string hrEmail = "hr@example.com";

            if (await userManager.FindByEmailAsync(hrEmail) == null)
            {
                var hrUser = new ApplicationUser
                {
                    UserName = hrEmail,
                    Email = hrEmail,
                    EmailConfirmed = true,
                    FirstName = "HR",
                    LastName = "Admin",
                    AnnualLeaveDays = 21, 
                    BonusLeaveDays = 0,
                    Position = "HR Manager"
                };

                const string hrPassword = "Password123!"; 
                var result = await userManager.CreateAsync(hrUser, hrPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(hrUser, "HR");
                }
            }
        }
    }
}