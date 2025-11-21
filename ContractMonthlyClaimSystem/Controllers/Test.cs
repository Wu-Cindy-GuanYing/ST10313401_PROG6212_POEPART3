using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class TestController : Controller
    {
        private readonly UserManager<CMCSUser> _userManager;

        public TestController(UserManager<CMCSUser> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult HashTest()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> HashTest(string password, string email)
        {
            if (string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter a password";
                return View();
            }

            try
            {
                // Create a temporary user
                var user = new CMCSUser
                {
                    UserName = email ?? "test@example.com",
                    Email = email ?? "test@example.com"
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Get the hashed password
                    var hashedPassword = user.PasswordHash;

                    ViewBag.OriginalPassword = password;
                    ViewBag.HashedPassword = hashedPassword;
                    ViewBag.Email = user.Email;
                    ViewBag.UserId = user.Id;

                    // Clean up - delete the test user
                    await _userManager.DeleteAsync(user);
                }
                else
                {
                    ViewBag.Error = "Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
            }

            return View();
        }
    }
}