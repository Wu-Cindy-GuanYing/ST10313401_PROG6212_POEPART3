using Microsoft.AspNetCore.Identity;
using ContractMonthlyClaimSystem.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ContractMonthlyClaimSystem.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<CMCSUser>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            // Create roles
            string[] roleNames = { "Lecturer", "Coordinator", "Manager", "HR" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create default HR user (super-admin)
            var hrUser = new CMCSUser
            {
                UserName = "hr@university.ac.za",
                Email = "hr@university.ac.za",
                FullName = "HR Administrator",
                Role = "HR",
                InitialPassword = "HR@Password123"
            };

            await CreateUserWithRole(userManager, hrUser, "HR", "HR@Password123");

            // Create default Program Coordinator
            var coordinatorUser = new CMCSUser
            {
                UserName = "coordinator@university.ac.za",
                Email = "coordinator@university.ac.za",
                FullName = "Programme Coordinator",
                Role = "Coordinator",
                InitialPassword = "Coordinator@123"
            };

            await CreateUserWithRole(userManager, coordinatorUser, "Coordinator", "Coordinator@123");

            // Create default Academic Manager
            var managerUser = new CMCSUser
            {
                UserName = "manager@university.ac.za",
                Email = "manager@university.ac.za",
                FullName = "Finance Manager",
                Role = "Manager",
                InitialPassword = "Manager@123"
            };

            await CreateUserWithRole(userManager, managerUser, "Manager", "Manager@123");

            // Create sample lecturer
            var lecturerUser = new CMCSUser
            {
                UserName = "lecturer@university.ac.za",
                Email = "lecturer@university.ac.za",
                FullName = "Dr. John Smith",
                Role = "Lecturer",
                InitialPassword = "Lecturer@123"
            };

            await CreateUserWithRole(userManager, lecturerUser, "Lecturer", "Lecturer@123");

            // Create lecturer profile for the sample lecturer
            var lecturerProfile = new Lecturer
            {
                Name = "Dr. John Smith",
                Email = "lecturer@university.ac.za",
                HourlyRate = 350.00m
            };

            if (!context.Lecturers.Any(l => l.Email == lecturerProfile.Email))
            {
                context.Lecturers.Add(lecturerProfile);
                await context.SaveChangesAsync();
            }
        }

        private static async Task CreateUserWithRole(UserManager<CMCSUser> userManager, CMCSUser user, string role, string password)
        {
            var existingUser = await userManager.FindByEmailAsync(user.Email);
            if (existingUser == null)
            {
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}