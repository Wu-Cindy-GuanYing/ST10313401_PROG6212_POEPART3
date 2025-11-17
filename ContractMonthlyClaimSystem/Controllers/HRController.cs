using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class HRController : Controller
{
    public IActionResult Index()
    {
        // 🎯 CHECK IF USER IS HR
        var userRole = HttpContext.Session.GetUserRole();
        if (!userRole.Equals("HR", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View();
    }

    // Other HR actions...
}