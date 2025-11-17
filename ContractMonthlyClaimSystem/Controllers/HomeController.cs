using System.Diagnostics;
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimSystem.Extensions;

namespace ContractMonthlyClaimSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            //REDIRECT USERS BASED ON ROLE
            if (HttpContext.Session.IsAuthenticated())
            {
                var userRole = HttpContext.Session.GetUserRole();

                return userRole switch
                {
                    "HR" => RedirectToAction("Index", "HR"),
                    "Coordinator" => RedirectToAction("Index", "Approval"),
                    "Manager" => RedirectToAction("Review", "Approval"),
                    "Lecturer" => RedirectToAction("Index", "Claim"),
                    _ => View()
                };
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        
    }
}
