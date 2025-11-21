using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using ContractMonthlyClaimSystem.Extensions;
using ContractMonthlyClaimSystem.ViewModels;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<CMCSUser> _signInManager;
        private readonly UserManager<CMCSUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SignInManager<CMCSUser> signInManager, UserManager<CMCSUser> userManager, ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // First, find the user by email
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }

                // Check if email is confirmed (if you require email confirmation)
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "You must have a confirmed email to log in.");
                    return View(model);
                }

                // Use the USERNAME for sign in, not email
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName, // ← Use UserName here, not Email
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // 🎯 SESSION MANAGEMENT
                    HttpContext.Session.SetString("UserRole", user.Role);
                    HttpContext.Session.SetString("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.FullName);

                    _logger.LogInformation($"User {user.FullName} with role {user.Role} logged in.");
                    return RedirectToLocal(returnUrl);
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]  
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Logout POST method called.");

            // Clear session data more thoroughly
            HttpContext.Session.Clear();
            await HttpContext.Session.CommitAsync(); // Ensure session is committed

            _logger.LogInformation("Session cleared.");

            await _signInManager.SignOutAsync();
            _logger.LogInformation("SignOutAsync completed.");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }

    
}