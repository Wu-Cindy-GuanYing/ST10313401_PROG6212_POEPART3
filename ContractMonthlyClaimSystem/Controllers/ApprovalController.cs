using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static ContractMonthlyClaimSystem.Models.Claim;
using ContractMonthlyClaimSystem.Extensions;

[Authorize(Roles = "Coordinator,Manager")]

public class ApprovalController : Controller
{
    private readonly AppDbContext _db;
    public ApprovalController(AppDbContext db) { _db = db; }


    public async Task<IActionResult> Index()
    {
        var userRole = HttpContext.Session.GetUserRole();
        if (!userRole.Equals("Coordinator", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var pending = await _db.Claims
            .Include(c => c.Documents)
            .Where(c => c.Status == ClaimStatus.Pending)
            .OrderBy(c => c.SubmittedDate)
            .ToListAsync();
        return View(pending);
    }


    public async Task<IActionResult> Review()
    {
        var userRole = HttpContext.Session.GetUserRole();
        if (!userRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var claim = await _db.Claims
            .Include(c => c.Documents)
            .Where(c => c.Status == ClaimStatus.ApprovedByCoordinator)
            .OrderBy(c => c.SubmittedDate)
            .ToListAsync();
        return View(claim);
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string role)
    {
        var userRole = HttpContext.Session.GetUserRole();
        if ((role == "Coordinator" && !userRole.Equals("Coordinator")) ||
            (role == "Manager" && !userRole.Equals("Manager")))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var claim = await _db.Claims.FindAsync(id); if (claim == null) return NotFound();
        // Simple two-step flow: coordinator approval -> manager approval
        claim.Status = String.Equals(role, "Coordinator") ? ClaimStatus.ApprovedByCoordinator : ClaimStatus.ApprovedByManager;
        claim.ApprovedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Claim #{id} {(claim.Status == ClaimStatus.ApprovedByManager ? "approved by Manager" : "approved by Coordinator")}.";
        return RedirectToAction(nameof(Index));
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var claim = await _db.Claims.FindAsync(id); if (claim == null) return NotFound();
        claim.Status = ClaimStatus.Rejected; claim.ApprovedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Message"] = $"Claim #{id} rejected."; 
        return RedirectToAction(nameof(Index));
    }
}