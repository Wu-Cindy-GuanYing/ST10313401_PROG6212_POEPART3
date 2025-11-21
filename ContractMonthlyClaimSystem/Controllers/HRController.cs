using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Extensions;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Report;
using ContractMonthlyClaimSystem.ViewModels;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ContractMonthlyClaimSystem.Models.Claim;

[Authorize]
public class HRController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<CMCSUser> _userManager;

    public HRController(AppDbContext context, UserManager<CMCSUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        //CHECK IF USER IS HR
        var userRole = HttpContext.Session.GetUserRole();
        if (!userRole.Equals("HR", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        try
        {
            // Get total lecturers count
            ViewBag.LecturerCount = await _context.Lecturers.CountAsync();

            // Get active lecturers count
            ViewBag.ActiveLecturers = await _context.Lecturers
                .Where(l => l.IsActive)
                .CountAsync();

            // Get pending claims count
            ViewBag.PendingClaims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Pending)
                .CountAsync();

            // Get total paid amount - use 0m for decimal zero
            ViewBag.TotalPaid = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Paid)
                .SumAsync(c => c.TotalAmount);
        }
        catch (Exception ex)
        {
            // Log the error and set default values
            // _logger.LogError(ex, "Error loading dashboard data");

            ViewBag.LecturerCount = 0;
            ViewBag.ActiveLecturers = 0;
            ViewBag.PendingClaims = 0;
            ViewBag.TotalPaid = 0m; // Use decimal zero here too

            TempData["ErrorMessage"] = "Error loading dashboard data. Please try again.";
        }

        return View();
    }


    // HR adds all lecturers
    [HttpGet]
    public IActionResult CreateLecturer()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLecturer(LecturerCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if email already exists using LINQ
            bool emailExists = _context.Lecturers
                .Any(l => l.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "A lecturer with this email already exists.");
                return View(model);
            }

            var lecturer = new Lecturer
            {
                Name = model.Name,
                Email = model.Email,
                HourlyRate = model.HourlyRate,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            _context.Lecturers.Add(lecturer);
            await _context.SaveChangesAsync();

            // Create login account for the lecturer
            var user = new CMCSUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.TemporaryPassword);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Lecturer");
                TempData["SuccessMessage"] = $"Lecturer {model.Name} created successfully!";
                return RedirectToAction(nameof(LecturerList));
            }
            else
            {
                _context.Lecturers.Remove(lecturer);
                await _context.SaveChangesAsync();

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        return View(model);
    }

    // HR can create Program Coordinators
    [HttpGet]
    public IActionResult CreateProgramCoordinator()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProgramCoordinator(StaffCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if email already exists in any staff role
            bool emailExists = await _userManager.FindByEmailAsync(model.Email) != null;

            if (emailExists)
            {
                ModelState.AddModelError("Email", "A user with this email already exists.");
                return View(model);
            }

            // Create login account for the program coordinator
            var user = new CMCSUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.Name,
                EmailConfirmed = true,
                Role = "Coordinator"
            };

            var result = await _userManager.CreateAsync(user, model.TemporaryPassword);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Coordinator");
                TempData["SuccessMessage"] = $"Program Coordinator {model.Name} created successfully!";
                return RedirectToAction(nameof(StaffList));
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        return View(model);
    }

    // HR can create Academic Managers
    [HttpGet]
    public IActionResult CreateAcademicManager()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAcademicManager(StaffCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if email already exists in any staff role
            bool emailExists = await _userManager.FindByEmailAsync(model.Email) != null;

            if (emailExists)
            {
                ModelState.AddModelError("Email", "A user with this email already exists.");
                return View(model);
            }

            // Create login account for the academic manager
            var user = new CMCSUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.Name,
                EmailConfirmed = true,
                Role = "Manager"
            };

            var result = await _userManager.CreateAsync(user, model.TemporaryPassword);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Manager");
                TempData["SuccessMessage"] = $"Academic Manager {model.Name} created successfully!";
                return RedirectToAction(nameof(StaffList));
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        return View(model);
    }

    // HR can update all lecturer information
    [HttpGet]
    public async Task<IActionResult> EditLecturer(int id)
    {
        var lecturer = await _context.Lecturers.FindAsync(id);
        if (lecturer == null) return NotFound();

        var model = new LecturerEditViewModel
        {
            Id = lecturer.Id,
            Name = lecturer.Name,
            Email = lecturer.Email,
            HourlyRate = lecturer.HourlyRate,
            IsActive = lecturer.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLecturer(LecturerEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var lecturer = await _context.Lecturers.FindAsync(model.Id);
            if (lecturer == null) return NotFound();

            // Check email conflict using LINQ
            bool emailConflict = _context.Lecturers
                .Any(l => l.Email == model.Email && l.Id != model.Id);

            if (emailConflict)
            {
                ModelState.AddModelError("Email", "A lecturer with this email already exists.");
                return View(model);
            }

            lecturer.Name = model.Name;
            lecturer.Email = model.Email;
            lecturer.HourlyRate = model.HourlyRate;
            lecturer.IsActive = model.IsActive;

            // Update login account if email changed
            if (lecturer.Email != model.Email)
            {
                var user = await _userManager.FindByEmailAsync(lecturer.Email);
                if (user != null)
                {
                    user.UserName = model.Email;
                    user.Email = model.Email;
                    await _userManager.UpdateAsync(user);
                }
            }

            _context.Lecturers.Update(lecturer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Lecturer {model.Name} updated successfully!";
            return RedirectToAction(nameof(LecturerList));
        }

        return View(model);
    }

    // Edit Program Coordinator or Academic Manager
    [HttpGet]
    public async Task<IActionResult> EditStaff(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        var isProgramCoordinator = userRoles.Contains("Coordinator");
        var isAcademicManager = userRoles.Contains("Manager");

        if (!isProgramCoordinator && !isAcademicManager)
        {
            TempData["ErrorMessage"] = "User is not a Program Coordinator or Academic Manager.";
            return RedirectToAction(nameof(StaffList));
        }

        var model = new StaffEditViewModel
        {
            Id = user.Id,
            Name = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive,
            IsProgramCoordinator = isProgramCoordinator,
            IsAcademicManager = isAcademicManager
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStaff(StaffEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Check email conflict
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null && existingUser.Id != model.Id)
            {
                ModelState.AddModelError("Email", "A user with this email already exists.");
                return View(model);
            }

            user.FullName = model.Name;
            user.UserName = model.Email;
            user.Email = model.Email;
            user.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Update roles if changed
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (model.IsProgramCoordinator)
                {
                    await _userManager.AddToRoleAsync(user, "Coordinator");
                }
                if (model.IsAcademicManager)
                {
                    await _userManager.AddToRoleAsync(user, "Manager");
                }

                TempData["SuccessMessage"] = $"Staff member {model.Name} updated successfully!";
                return RedirectToAction(nameof(StaffList));
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        return View(model);
    }

    // Lecturer list for HR to manage
    [HttpGet]
    public async Task<IActionResult> LecturerList()
    {
        var lecturers = _context.Lecturers
            .OrderBy(l => l.Name)
            .Select(l => new LecturerListViewModel
            {
                Id = l.Id,
                Name = l.Name,
                Email = l.Email,
                HourlyRate = l.HourlyRate,
                IsActive = l.IsActive,
                CreatedDate = l.CreatedDate
            })
            .ToList();

        return View(lecturers);
    }

    // Staff list for HR to manage Program Coordinators and Academic Managers
    [HttpGet]
    public async Task<IActionResult> StaffList()
    {
        var programCoordinators = await _userManager.GetUsersInRoleAsync("Coordinator");
        var academicManagers = await _userManager.GetUsersInRoleAsync("Manager");

        var staffList = programCoordinators.Select(u => new StaffListViewModel
        {
            Id = u.Id,
            Name = u.FullName,
            Email = u.Email,
            Role = "Program Coordinator",
            IsActive = u.IsActive,
            CreatedDate = u.CreatedDate
        }).Concat(academicManagers.Select(u => new StaffListViewModel
        {
            Id = u.Id,
            Name = u.FullName,
            Email = u.Email,
            Role = "Academic Manager",
            IsActive = u.IsActive,
            CreatedDate = u.CreatedDate
        })).OrderBy(s => s.Name).ToList();

        return View(staffList);
    }

    // HR can deactivate/activate lecturers
    [HttpPost]
    public async Task<IActionResult> ToggleLecturerStatus(int id)
    {
        var lecturer = await _context.Lecturers.FindAsync(id);
        if (lecturer == null) return NotFound();

        lecturer.IsActive = !lecturer.IsActive;
        _context.Lecturers.Update(lecturer);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Lecturer {lecturer.Name} has been {(lecturer.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(LecturerList));
    }

    // HR can deactivate/activate staff
    [HttpPost]
    public async Task<IActionResult> ToggleStaffStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Staff member {user.FullName} has been {(user.IsActive ? "activated" : "deactivated")}.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Failed to update staff member status.";
        }

        return RedirectToAction(nameof(StaffList));
    }

    // Reset password for staff members
    [HttpPost]
    public async Task<IActionResult> ResetStaffPassword(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Password for {user.FullName} has been reset successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Failed to reset password.";
        }

        return RedirectToAction(nameof(StaffList));
    }

    // HR can generate reports or invoices
    [HttpGet]
    public async Task<IActionResult> GenerateReport()
    {
        try
        {
            // Set user info for the view
            var userName = HttpContext.Session.GetUserName();
            var userRole = HttpContext.Session.GetUserRole();
            var userEmail = User.Identity?.Name;

            ViewBag.UserName = userName;
            ViewBag.UserRole = userRole;
            ViewBag.UserEmail = userEmail;

            // Get active lecturers for the dropdown
            var lecturers = await _context.Lecturers
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new { l.Id, l.Name })
                .ToListAsync();

            ViewBag.Lecturers = lecturers;

            return View();
        }
        catch (Exception ex)
        {
            // Remove the logger line or replace with Console.WriteLine for debugging
            Console.WriteLine($"Error loading GenerateReport page: {ex.Message}");
            TempData["ErrorMessage"] = "Error loading report generation page. Please try again.";
            return View();
        }
    }

    [HttpPost]
    [HttpPost]
    public async Task<IActionResult> GenerateMonthlyInvoice(ReportParameters model)
    {
        if (!ModelState.IsValid)
        {
            return View("GenerateReport", model);
        }

        // Get lecturer info
        var lecturer = await _context.Lecturers
            .FirstOrDefaultAsync(l => l.Id == model.LecturerId);

        if (lecturer == null)
        {
            TempData["ErrorMessage"] = "Lecturer not found.";
            return RedirectToAction(nameof(GenerateReport));
        }

        // Get ALL paid claims for lecturer and month and SUM them
        var monthlyClaims = await _context.Claims
            .Where(c => c.LecturerId == model.LecturerId &&
                       c.Month.Month == model.Month &&
                       c.Month.Year == model.Year &&
                       c.Status == Claim.ClaimStatus.ApprovedByManager)
            .ToListAsync();

        if (!monthlyClaims.Any())
        {
            TempData["ErrorMessage"] = "No paid claims found for the specified period.";
            return RedirectToAction(nameof(GenerateReport));
        }

        // Calculate totals from all claims
        var totalHours = monthlyClaims.Sum(c => c.TotalHours);
        var totalAmount = monthlyClaims.Sum(c => c.TotalAmount);

        // Create a summary object for the invoice
        var invoiceSummary = new
        {
            Lecturer = lecturer,
            TotalHours = totalHours,
            TotalAmount = totalAmount,
            Month = new DateTime(model.Year, model.Month, 1),
            ClaimCount = monthlyClaims.Count
        };

        var pdfBytes = GenerateInvoicePdf(invoiceSummary);
        return File(pdfBytes, "application/pdf",
            $"Invoice_{lecturer.Name.Replace(" ", "_")}_{model.Month:00}_{model.Year}.pdf");
    }

    [HttpPost]
    public async Task<IActionResult> GeneratePayrollReport(ReportParameters model)
    {
        // Generate payroll report using LINQ
        var payrollData = _context.Claims
            .Where(c => c.Month.Month == model.Month &&
                       c.Month.Year == model.Year &&
                       c.Status == Claim.ClaimStatus.ApprovedByManager)
            .Join(_context.Lecturers.Where(l => l.IsActive),
                  claim => claim.LecturerId,
                  lecturer => lecturer.Id,
                  (claim, lecturer) => new PayrollReportItem
                  {
                      LecturerName = lecturer.Name,
                      Email = lecturer.Email,
                      HoursWorked = claim.TotalHours,
                      HourlyRate = lecturer.HourlyRate,
                      TotalAmount = claim.TotalAmount,
                      ClaimMonth = claim.Month
                  })
            .ToList();

        if (!payrollData.Any())
        {
            TempData["ErrorMessage"] = "No payroll data found for the specified period.";
            return RedirectToAction(nameof(GenerateReport));
        }

        var pdfBytes = GeneratePayrollPdf(payrollData, model.Month, model.Year);
        return File(pdfBytes, "application/pdf", $"PayrollReport_{model.Month}_{model.Year}.pdf");
    }

    // Generate comprehensive claims report using LINQ
    [HttpPost]
    public async Task<IActionResult> GenerateClaimsReport(ClaimsReportParameters model)
    {
        var claimsQuery = _context.Claims
            .Where(c => c.Month >= model.StartDate && c.Month <= model.EndDate);

        // Apply status filter if provided
        if (model.Status.HasValue)
        {
            claimsQuery = claimsQuery.Where(c => c.Status == model.Status.Value);
        }

        var claimsData = claimsQuery
            .Join(_context.Lecturers,
                  claim => claim.LecturerId,
                  lecturer => lecturer.Id,
                  (claim, lecturer) => new ClaimsReportItem
                  {
                      LecturerName = lecturer.Name,
                      Email = lecturer.Email,
                      Month = claim.Month,
                      TotalHours = claim.TotalHours,
                      TotalAmount = claim.TotalAmount,
                      Status = claim.Status,
                      SubmittedDate = claim.SubmittedDate,
                      ApprovedDate = claim.ApprovedDate
                  })
            .ToList();

        if (!claimsData.Any())
        {
            TempData["ErrorMessage"] = "No claims data found for the specified criteria.";
            return RedirectToAction(nameof(GenerateReport));
        }

        var pdfBytes = GenerateClaimsReportPdf(claimsData, model);
        return File(pdfBytes, "application/pdf", $"Claims_Report_{DateTime.Now:yyyyMMdd}.pdf");
    }

    // Generate comprehensive lecturer report using LINQ
    [HttpPost]
    public async Task<IActionResult> GenerateLecturerReport()
    {
        var lecturers = _context.Lecturers
            .OrderBy(l => l.Name)
            .Select(l => new LecturerReportItem
            {
                Name = l.Name,
                Email = l.Email,
                HourlyRate = l.HourlyRate,
                IsActive = l.IsActive,
                CreatedDate = l.CreatedDate,
                TotalClaims = _context.Claims.Count(c => c.LecturerId == l.Id),
                ApprovedClaims = _context.Claims.Count(c => c.LecturerId == l.Id && c.Status == Claim.ClaimStatus.Paid),
                TotalAmount = _context.Claims
                    .Where(c => c.LecturerId == l.Id && c.Status == Claim.ClaimStatus.Paid)
                    .Sum(c => (decimal?)c.TotalAmount) ?? 0
            })
            .ToList();

        var pdfBytes = GenerateLecturerReportPdf(lecturers);
        return File(pdfBytes, "application/pdf", $"Lecturer_Report_{DateTime.Now:yyyyMMdd}.pdf");
    }

    // Generate staff report for Program Coordinators and Academic Managers
    [HttpPost]
    public async Task<IActionResult> GenerateStaffReport()
    {
        var programCoordinators = await _userManager.GetUsersInRoleAsync("Coordinator");
        var academicManagers = await _userManager.GetUsersInRoleAsync("Manager");

        var staffReport = programCoordinators.Select(u => new StaffReportItem
        {
            Name = u.FullName,
            Email = u.Email,
            Role = "Program Coordinator",
            IsActive = u.IsActive,
            CreatedDate = u.CreatedDate
        }).Concat(academicManagers.Select(u => new StaffReportItem
        {
            Name = u.FullName,
            Email = u.Email,
            Role = "Academic Manager",
            IsActive = u.IsActive,
            CreatedDate = u.CreatedDate
        })).OrderBy(s => s.Name).ToList();

        var pdfBytes = GenerateStaffReportPdf(staffReport);
        return File(pdfBytes, "application/pdf", $"Staff_Report_{DateTime.Now:yyyyMMdd}.pdf");
    }

    private byte[] GenerateInvoicePdf(dynamic invoiceSummary)
    {
        using (var memoryStream = new MemoryStream())
        {
            var document = new iTextSharp.text.Document();
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            document.Add(new Paragraph("MONTHLY INVOICE - SUMMARY", headerFont));
            document.Add(new Paragraph(" "));

            document.Add(new Paragraph($"Lecturer: {invoiceSummary.Lecturer.Name}"));
            document.Add(new Paragraph($"Email: {invoiceSummary.Lecturer.Email}"));
            document.Add(new Paragraph($"Period: {invoiceSummary.Month:MMMM yyyy}"));
            document.Add(new Paragraph($"Number of Claims: {invoiceSummary.ClaimCount}"));
            document.Add(new Paragraph($"Total Hours Worked: {invoiceSummary.TotalHours:F2}"));
            document.Add(new Paragraph($"Hourly Rate: ${invoiceSummary.Lecturer.HourlyRate:F2}"));

            var totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            document.Add(new Paragraph($"Total Amount: ${invoiceSummary.TotalAmount:F2}", totalFont));
            document.Add(new Paragraph(" "));
            document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}"));

            document.Close();
            return memoryStream.ToArray();
        }
    }

    private byte[] GeneratePayrollPdf(List<PayrollReportItem> payrollData, int month, int year)
    {
        using (var memoryStream = new MemoryStream())
        {
            var document = new iTextSharp.text.Document();
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            document.Add(new Paragraph($"PAYROLL REPORT - {month}/{year}", headerFont));
            document.Add(new Paragraph(" "));

            var table = new PdfPTable(5);
            table.WidthPercentage = 100;

            var headerFontStyle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            table.AddCell(new PdfPCell(new Phrase("Lecturer Name", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Email", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Hours", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Rate", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Total", headerFontStyle)));

            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            foreach (var item in payrollData)
            {
                table.AddCell(new PdfPCell(new Phrase(item.LecturerName, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.Email, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(item.HoursWorked.ToString("F2"), cellFont)));
                table.AddCell(new PdfPCell(new Phrase($"${item.HourlyRate:F2}", cellFont)));
                table.AddCell(new PdfPCell(new Phrase($"${item.TotalAmount:F2}", cellFont)));
            }

            document.Add(table);
            document.Add(new Paragraph(" "));
            document.Add(new Paragraph($"Total Lecturers: {payrollData.Count}"));
            document.Add(new Paragraph($"Grand Total: ${payrollData.Sum(x => x.TotalAmount):F2}"));
            document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}"));

            document.Close();
            return memoryStream.ToArray();
        }
    }

    private byte[] GenerateClaimsReportPdf(List<ClaimsReportItem> claimsData, ClaimsReportParameters parameters)
    {
        using (var memoryStream = new MemoryStream())
        {
            var document = new iTextSharp.text.Document();
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            document.Add(new Paragraph("CLAIMS MANAGEMENT REPORT", headerFont));
            document.Add(new Paragraph($"Period: {parameters.StartDate:MMMM yyyy} to {parameters.EndDate:MMMM yyyy}"));
            if (parameters.Status.HasValue)
                document.Add(new Paragraph($"Status: {parameters.Status}"));
            document.Add(new Paragraph(" "));

            var table = new PdfPTable(7);
            table.WidthPercentage = 100;

            var headerFontStyle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);
            table.AddCell(new PdfPCell(new Phrase("Lecturer", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Email", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Month", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Hours", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Amount", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Status", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Submitted", headerFontStyle)));

            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 7);
            foreach (var claim in claimsData)
            {
                table.AddCell(new PdfPCell(new Phrase(claim.LecturerName, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(claim.Email, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(claim.Month.ToString("MMM yyyy"), cellFont)));
                table.AddCell(new PdfPCell(new Phrase(claim.TotalHours.ToString("F2"), cellFont)));
                table.AddCell(new PdfPCell(new Phrase($"${claim.TotalAmount:F2}", cellFont)));
                table.AddCell(new PdfPCell(new Phrase(claim.Status.ToString(), cellFont)));
                table.AddCell(new PdfPCell(new Phrase(claim.SubmittedDate.ToString("yyyy-MM-dd"), cellFont)));
            }

            document.Add(table);
            document.Add(new Paragraph(" "));
            document.Add(new Paragraph($"Total Claims: {claimsData.Count}"));
            document.Add(new Paragraph($"Total Amount: ${claimsData.Sum(x => x.TotalAmount):F2}"));
            document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}"));

            document.Close();
            return memoryStream.ToArray();
        }
    }

    private byte[] GenerateLecturerReportPdf(List<LecturerReportItem> lecturers)
    {
        using (var memoryStream = new MemoryStream())
        {
            var document = new iTextSharp.text.Document();
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            document.Add(new Paragraph("LECTURER MANAGEMENT REPORT", headerFont));
            document.Add(new Paragraph(" "));

            var table = new PdfPTable(8);
            table.WidthPercentage = 100;

            var headerFontStyle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);
            table.AddCell(new PdfPCell(new Phrase("Name", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Email", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Hourly Rate", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Status", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Created", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Total Claims", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Paid Claims", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Total Earned", headerFontStyle)));

            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 7);
            foreach (var lecturer in lecturers)
            {
                table.AddCell(new PdfPCell(new Phrase(lecturer.Name, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(lecturer.Email, cellFont)));
                table.AddCell(new PdfPCell(new Phrase($"${lecturer.HourlyRate:F2}", cellFont)));
                table.AddCell(new PdfPCell(new Phrase(lecturer.IsActive ? "Active" : "Inactive", cellFont)));
                table.AddCell(new PdfPCell(new Phrase(lecturer.CreatedDate.ToString("yyyy-MM-dd"), cellFont)));
                table.AddCell(new PdfPCell(new Phrase(lecturer.TotalClaims.ToString(), cellFont)));
                table.AddCell(new PdfPCell(new Phrase(lecturer.ApprovedClaims.ToString(), cellFont)));
                table.AddCell(new PdfPCell(new Phrase($"${lecturer.TotalAmount:F2}", cellFont)));
            }

            document.Add(table);
            document.Add(new Paragraph(" "));
            document.Add(new Paragraph($"Total Lecturers: {lecturers.Count}"));
            document.Add(new Paragraph($"Active Lecturers: {lecturers.Count(l => l.IsActive)}"));
            document.Add(new Paragraph($"Total Amount Paid: ${lecturers.Sum(l => l.TotalAmount):F2}"));
            document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}"));

            document.Close();
            return memoryStream.ToArray();
        }
    }

    private byte[] GenerateStaffReportPdf(List<StaffReportItem> staff)
    {
        using (var memoryStream = new MemoryStream())
        {
            var document = new iTextSharp.text.Document();
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            document.Add(new Paragraph("STAFF MANAGEMENT REPORT", headerFont));
            document.Add(new Paragraph(" "));

            var table = new PdfPTable(5);
            table.WidthPercentage = 100;

            var headerFontStyle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            table.AddCell(new PdfPCell(new Phrase("Name", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Email", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Role", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Status", headerFontStyle)));
            table.AddCell(new PdfPCell(new Phrase("Created Date", headerFontStyle)));

            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            foreach (var staffMember in staff)
            {
                table.AddCell(new PdfPCell(new Phrase(staffMember.Name, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(staffMember.Email, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(staffMember.Role, cellFont)));
                table.AddCell(new PdfPCell(new Phrase(staffMember.IsActive ? "Active" : "Inactive", cellFont)));
                table.AddCell(new PdfPCell(new Phrase(staffMember.CreatedDate.ToString("yyyy-MM-dd"), cellFont)));
            }

            document.Add(table);
            document.Add(new Paragraph(" "));
            document.Add(new Paragraph($"Total Staff: {staff.Count}"));
            document.Add(new Paragraph($"Program Coordinators: {staff.Count(s => s.Role == "Coordinator")}"));
            document.Add(new Paragraph($"Academic Managers: {staff.Count(s => s.Role == "Manager")}"));
            document.Add(new Paragraph($"Active Staff: {staff.Count(s => s.IsActive)}"));
            document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}"));

            document.Close();
            return memoryStream.ToArray();
        }
    }
}
