// Controllers/ClaimController.cs
using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;
using ContractMonthlyClaimSystem.Extensions;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class ClaimController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ClaimController> _logger;

        public ClaimController(AppDbContext db, ILogger<ClaimController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userEmail = User.Identity?.Name;
                var userName = HttpContext.Session.GetUserName();
                var userRole = HttpContext.Session.GetUserRole();

                Console.WriteLine($"=== CLAIM INDEX STARTED ===");
                Console.WriteLine($"User Email: {userEmail}, User Name: {userName}, Role: {userRole}");

                // Set user info in ViewBag for the layout
                ViewBag.UserName = userName;
                ViewBag.UserRole = userRole;
                ViewBag.UserEmail = userEmail;

                // 🎯 IMPROVED LECTURER MATCHING LOGIC (same as in Create method)
                Lecturer lecturer = null;

                // Method 1: Try matching by email from Identity
                if (!string.IsNullOrEmpty(userEmail))
                {
                    lecturer = await _db.Lecturers
                        .FirstOrDefaultAsync(l => l.Email == userEmail && l.IsActive);
                    Console.WriteLine($"Method 1 - Email match: {(lecturer != null ? "SUCCESS" : "FAILED")}");
                }

                // Method 2: Fallback to session user name if email match fails
                if (lecturer == null && !string.IsNullOrEmpty(userName))
                {
                    lecturer = await _db.Lecturers
                        .FirstOrDefaultAsync(l => l.Name.Contains(userName) && l.IsActive);
                    Console.WriteLine($"Method 2 - Name match: {(lecturer != null ? "SUCCESS" : "FAILED")}");
                }

                // Method 3: Final fallback - get first active lecturer (for testing/development)
                if (lecturer == null)
                {
                    lecturer = await _db.Lecturers
                        .FirstOrDefaultAsync(l => l.IsActive);
                    Console.WriteLine($"Method 3 - First active: {(lecturer != null ? "SUCCESS" : "FAILED")}");
                }

                if (lecturer == null)
                {
                    Console.WriteLine("❌ No lecturer found for current user");
                    TempData["ErrorMessage"] = "No active lecturer profile found matching your account. Please contact HR.";

                    // Still return view but with empty list
                    ViewBag.LecturerName = "Unknown Lecturer";
                    ViewBag.LecturerEmail = "Unknown";
                    ViewBag.LecturerId = 0;

                    // Set empty statistics
                    ViewData["TotalClaims"] = 0;
                    ViewData["PendingClaims"] = 0;
                    ViewData["ApprovedClaims"] = 0;
                    ViewData["TotalAmount"] = 0m;

                    return View(new List<Claim>());
                }

                Console.WriteLine($"🎯 Found lecturer: {lecturer.Name}, ID: {lecturer.Id}");

                // Set lecturer info in ViewBag
                ViewBag.LecturerName = lecturer.Name;
                ViewBag.LecturerEmail = lecturer.Email;
                ViewBag.LecturerId = lecturer.Id;

                // 🎯 FILTER CLAIMS FOR CURRENT LECTURER ONLY
                var claims = await _db.Claims
                    .Where(c => c.LecturerId == lecturer.Id) // Only get claims for this lecturer
                    .Include(c => c.ClaimItems)
                    .Include(c => c.Documents)
                    .OrderByDescending(c => c.SubmittedDate)
                    .ToListAsync();

                Console.WriteLine($"📊 Found {claims.Count} claims for lecturer {lecturer.Name}");

                // In each action method, after getting lecturer info, add:
                HttpContext.Session.SetString("UserName", lecturer?.Name ?? "Unknown");
                HttpContext.Session.SetString("UserRole", userRole);
                HttpContext.Session.SetString("UserEmail", userEmail);

                // Set additional ViewData for the view
                ViewData["TotalClaims"] = claims.Count;
                ViewData["PendingClaims"] = claims.Count(c => c.Status == Claim.ClaimStatus.Pending);
                ViewData["ApprovedClaims"] = claims.Count(c => c.Status == Claim.ClaimStatus.ApprovedByManager);
                ViewData["TotalAmount"] = claims.Where(c => c.Status == Claim.ClaimStatus.ApprovedByManager).Sum(c => c.TotalAmount);

                return View(claims);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR in Claim Index: {ex.Message}");
                _logger.LogError(ex, "Error in ClaimController.Index");

                // Return empty list on error with proper ViewBag values
                ViewBag.LecturerName = "Error Loading Data";
                ViewBag.LecturerEmail = "Error";
                ViewBag.LecturerId = 0;
                ViewData["TotalClaims"] = 0;
                ViewData["PendingClaims"] = 0;
                ViewData["ApprovedClaims"] = 0;
                ViewData["TotalAmount"] = 0m;

                return View(new List<Claim>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            // Set user info for the view
            var userName = HttpContext.Session.GetUserName();
            var userRole = HttpContext.Session.GetUserRole();
            var userEmail = User.Identity?.Name;

            ViewBag.UserName = userName;
            ViewBag.UserRole = userRole;
            ViewBag.UserEmail = userEmail;

            // In each action method, after getting lecturer info, add:
            HttpContext.Session.SetString("UserName", userName);
            HttpContext.Session.SetString("UserRole", userRole);
            HttpContext.Session.SetString("UserEmail", userEmail);

            var model = new ClaimCreateVm();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClaimCreateVm vm)
        {
            var userRole = HttpContext.Session.GetUserRole();
            var userEmail = User.Identity?.Name;
            var userName = HttpContext.Session.GetUserName();

            // Set user info for the view in case we need to return the form
            ViewBag.UserName = userName;
            ViewBag.UserRole = userRole;
            ViewBag.UserEmail = userEmail;

            Console.WriteLine("=== CLAIM SUBMISSION STARTED ===");
            Console.WriteLine($"User Email: {userEmail}, User Name: {userName}");

            // --- File validation for multiple files ---
            if (vm.Uploads != null && vm.Uploads.Count > 0)
            {
                Console.WriteLine($"Number of files: {vm.Uploads.Count}");
                var allowed = new List<string> { ".pdf", ".docx", ".xlsx" };
                const long maxBytes = 10 * 1024 * 1024;

                foreach (var file in vm.Uploads)
                {
                    Console.WriteLine($"File: {file.FileName}, Size: {file.Length} bytes");
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        Console.WriteLine($"File extension: {ext}");

                        if (!allowed.Contains(ext))
                        {
                            ModelState.AddModelError(nameof(vm.Uploads), $"File '{file.FileName}': Only PDF, DOCX, or XLSX files are allowed.");
                        }

                        if (file.Length > maxBytes)
                        {
                            ModelState.AddModelError(nameof(vm.Uploads), $"File '{file.FileName}' must be 10MB or smaller.");
                        }
                    }
                }
            }

            Console.WriteLine($"ModelState isValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model validation failed");
                return View(vm);
            }

            try
            {
                Console.WriteLine("Starting database operations...");

                // 🎯 IMPROVED LECTURER MATCHING LOGIC
                Lecturer lecturer = null;

                // Method 1: Try matching by email from Identity
                if (!string.IsNullOrEmpty(userEmail))
                {
                    lecturer = await _db.Lecturers
                        .FirstOrDefaultAsync(l => l.Email == userEmail && l.IsActive);
                    Console.WriteLine($"Method 1 - Email match: {(lecturer != null ? "SUCCESS" : "FAILED")}");
                }

                // Method 2: Fallback to session user name if email match fails
                if (lecturer == null && !string.IsNullOrEmpty(userName))
                {
                    lecturer = await _db.Lecturers
                        .FirstOrDefaultAsync(l => l.Name.Contains(userName) && l.IsActive);
                    Console.WriteLine($"Method 2 - Name match: {(lecturer != null ? "SUCCESS" : "FAILED")}");
                }

                // Method 3: Final fallback - get first active lecturer (for testing/development)
                if (lecturer == null)
                {
                    lecturer = await _db.Lecturers
                        .FirstOrDefaultAsync(l => l.IsActive);
                    Console.WriteLine($"Method 3 - First active: {(lecturer != null ? "SUCCESS" : "FAILED")}");
                }

                if (lecturer == null)
                {
                    ModelState.AddModelError("", "❌ No active lecturer profile found matching your account. Please contact HR to ensure your lecturer profile is properly linked.");
                    Console.WriteLine("❌ LECTURER MATCHING FAILED - No matching lecturer found");
                    return View(vm);
                }

                Console.WriteLine($"🎯 AUTOMATION: Found lecturer: {lecturer.Name}, Email: {lecturer.Email}, Rate: {lecturer.HourlyRate}");

                // 🎯 USE IMPROVED VALIDATION
                ValidateClaimSubmission(vm, lecturer);

                // Check if validation failed after our custom validation
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("Custom validation failed");
                    return View(vm);
                }

                // 🎯 AUTOMATIC CALCULATION
                var hourlyRate = lecturer.HourlyRate;
                var totalAmount = vm.HoursWorked * hourlyRate;

                // Create claim with AUTOMATIC values
                var claim = new Claim
                {
                    LecturerId = lecturer.Id,        // 🎯 From database
                    LecturerName = lecturer.Name,    // 🎯 From database  
                    Month = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
                    TotalHours = vm.HoursWorked,
                    TotalAmount = totalAmount,       // 🎯 Automatically calculated
                    Status = Claim.ClaimStatus.Pending,
                    SubmittedDate = DateTime.UtcNow
                };

                Console.WriteLine($"🎯 AUTOMATION: Claim calculated - Hours={vm.HoursWorked}, Rate={hourlyRate}, Total={totalAmount}");

                // Add claim item with AUTOMATIC rate
                claim.ClaimItems.Add(new ClaimItem
                {
                    Date = DateTime.UtcNow.Date,
                    Hours = vm.HoursWorked,
                    Rate = hourlyRate,  // 🎯 From database
                    Description = vm.Notes ?? $"Monthly claim for {claim.Month:MMMM yyyy}"
                });

                Console.WriteLine("Claim item added with automatic rate");

                // File processing
                if (vm.Uploads != null && vm.Uploads.Count > 0)
                {
                    Console.WriteLine("Processing files...");
                    foreach (var file in vm.Uploads.Where(f => f.Length > 0))
                    {
                        using var memoryStream = new MemoryStream();
                        await file.CopyToAsync(memoryStream);
                        var fileContent = memoryStream.ToArray();

                        claim.Documents.Add(new Document
                        {
                            FileName = GenerateSafeFileName(file.FileName),
                            OriginalFileName = file.FileName,
                            FileContent = fileContent,
                            ContentType = file.ContentType,
                            SizeBytes = file.Length,
                            UploadedDate = DateTime.UtcNow
                        });
                        Console.WriteLine($"Document added: {file.FileName}, Size: {file.Length} bytes");
                    }
                }

                Console.WriteLine("Adding claim to database context...");
                _db.Claims.Add(claim);

                Console.WriteLine("Saving changes to database...");
                var result = await _db.SaveChangesAsync();
                Console.WriteLine($"SaveChangesAsync completed. Result: {result} rows affected");
                Console.WriteLine($"New claim ID: {claim.Id}");

                // 🎯 IMPROVED SUCCESS MESSAGE
                TempData["Message"] =
                    $"✅ Claim Submitted Successfully!\n" +
                    $"• Lecturer: {lecturer.Name}\n" +
                    $"• Automatic Rate: {hourlyRate:C}/hr\n" +
                    $"• Hours Worked: {vm.HoursWorked}\n" +
                    $"• Total Amount: {totalAmount:C}\n" +
                    $"• Status: {claim.Status}\n" +
                    $"You can track your claim status in your claims list.";

                Console.WriteLine("=== CLAIM SUBMISSION COMPLETED SUCCESSFULLY ===");

                return RedirectToAction(nameof(Details), new { id = claim.Id });
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"=== DATABASE ERROR: {dbEx.Message}");
                _logger.LogError(dbEx, "Database error in ClaimController.Create");
                ModelState.AddModelError("",
                    "💾 Database Save Failed\n" +
                    "We couldn't save your claim to the database. This might be due to:\n" +
                    "• Network connectivity issues\n" +
                    "• Database maintenance\n" +
                    "Please try again in a few minutes or contact support if the problem persists.");
                return View(vm);
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"=== FILE IO ERROR: {ioEx.Message}");
                _logger.LogError(ioEx, "File IO error in ClaimController.Create");
                ModelState.AddModelError("",
                    "📎 File Processing Error\n" +
                    "We encountered an issue processing your uploaded files.\n" +
                    "Please check your files and try again, or contact support.");
                return View(vm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== UNEXPECTED ERROR: {ex.Message}");
                _logger.LogError(ex, "Unexpected error in ClaimController.Create");
                ModelState.AddModelError("",
                    "⚠️ Unexpected System Error\n" +
                    "An unexpected error occurred while processing your claim.\n" +
                    "Our technical team has been notified. Please try again later.\n" +
                    $"Error reference: {Guid.NewGuid():N}");

                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var claim = await _db.Claims
                    .Include(c => c.ClaimItems)
                    .Include(c => c.Documents)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Set user info for the view
                var userName = HttpContext.Session.GetUserName();
                var userRole = HttpContext.Session.GetUserRole();
                var userEmail = User.Identity?.Name;

                ViewBag.UserName = userName;
                ViewBag.UserRole = userRole;
                ViewBag.UserEmail = userEmail;
                ViewBag.LecturerName = claim.LecturerName;
                ViewBag.ClaimId = claim.Id;

                // Set additional view data that might be useful
                ViewData["TotalDocuments"] = claim.Documents.Count;
                ViewData["TotalClaimItems"] = claim.ClaimItems.Count;
                ViewData["IsPending"] = claim.Status == Claim.ClaimStatus.Pending;

                // In each action method, after getting lecturer info, add:
                HttpContext.Session.SetString("UserName", userName);
                HttpContext.Session.SetString("UserRole", userRole);
                HttpContext.Session.SetString("UserEmail", userEmail);

                return View(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClaimController.Details for claim ID {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading claim details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var document = await _db.Documents.FindAsync(id);
                if (document == null || document.FileContent == null)
                {
                    TempData["ErrorMessage"] = "Document not found.";
                    return RedirectToAction(nameof(Index));
                }

                return File(document.FileContent, document.ContentType ?? "application/octet-stream", document.OriginalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClaimController.DownloadDocument for document ID {DocumentId}", id);
                TempData["ErrorMessage"] = "An error occurred while downloading the document.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Coordinator,Manager")] // Restrict to approvers only
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var claim = await _db.Claims.FindAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                claim.Status = Claim.ClaimStatus.ApprovedByCoordinator;
                claim.ApprovedDate = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                TempData["Message"] = "Claim approved successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClaimController.Approve for claim ID {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while approving the claim.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Coordinator,Manager")] // Restrict to approvers only
        public async Task<IActionResult> Reject(int id, string reason)
        {
            try
            {
                var claim = await _db.Claims.FindAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                claim.Status = Claim.ClaimStatus.Rejected;
                // Consider adding a RejectionReason field to your Claim model
                // claim.RejectionReason = reason;

                await _db.SaveChangesAsync();

                TempData["Message"] = $"Claim rejected. Reason: {reason}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClaimController.Reject for claim ID {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while rejecting the claim.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                // Test if we can read from database
                var count = await _db.Claims.CountAsync();

                // Test if we can write to database
                var testClaim = new Claim
                {
                    LecturerId = 999,
                    LecturerName = "Test User",
                    Month = DateTime.UtcNow,
                    TotalHours = 1,
                    TotalAmount = 100,
                    Status = Claim.ClaimStatus.Pending,
                    SubmittedDate = DateTime.UtcNow
                };

                _db.Claims.Add(testClaim);
                var result = await _db.SaveChangesAsync();

                // Clean up test data
                _db.Claims.Remove(testClaim);
                await _db.SaveChangesAsync();

                TempData["Message"] = $"Database test successful! Existing claims: {count}, Write test: {(result > 0 ? "PASSED" : "FAILED")}";
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Database test failed: {ex.Message}";
                return RedirectToAction("Create");
            }
        }

        // 🎯 IMPROVED VALIDATION HELPER METHOD
        private void ValidateClaimSubmission(ClaimCreateVm vm, Lecturer lecturer)
        {
            // Clear previous model errors for these fields to avoid duplicates
            ModelState.Remove(nameof(vm.HoursWorked));

            // Hours validation with specific messages
            if (vm.HoursWorked <= 0)
            {
                ModelState.AddModelError(nameof(vm.HoursWorked), "🕒 Hours worked must be greater than 0. Please enter a valid number of hours.");
            }
            else if (vm.HoursWorked > 180)
            {
                ModelState.AddModelError(nameof(vm.HoursWorked),
                    $"🕒 Maximum 180 hours per month exceeded. You entered {vm.HoursWorked} hours. " +
                    "Please adjust your hours or contact HR if you need to claim more than 180 hours.");
            }
            else if (vm.HoursWorked < 0.25m)
            {
                ModelState.AddModelError(nameof(vm.HoursWorked),
                    "🕒 Minimum 0.25 hours required. Please enter at least 15 minutes of work.");
            }

            // Lecturer status validation
            if (lecturer == null)
            {
                ModelState.AddModelError("",
                    "👤 No lecturer profile found. Please ensure:\n" +
                    "• Your email matches your lecturer profile\n" +
                    "• Your account is active\n" +
                    "• Contact HR if this issue persists");
            }
            else if (!lecturer.IsActive)
            {
                ModelState.AddModelError("",
                    "👤 Account Inactive\n" +
                    $"Your lecturer account ({lecturer.Name}) is currently inactive.\n" +
                    "Please contact HR to reactivate your account before submitting claims.");
            }

            // Rate validation
            if (lecturer?.HourlyRate <= 0)
            {
                ModelState.AddModelError("",
                    "💵 Invalid Hourly Rate\n" +
                    "Your profile has an invalid hourly rate ($0.00).\n" +
                    "Please contact HR to update your rate before submitting claims.");
            }

            // File validation improvements
            if (vm.Uploads != null && vm.Uploads.Any())
            {
                foreach (var file in vm.Uploads.Where(f => f.Length > 0))
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var allowed = new List<string> { ".pdf", ".docx", ".xlsx" };

                    if (!allowed.Contains(ext))
                    {
                        ModelState.AddModelError(nameof(vm.Uploads),
                            $"📎 Invalid file type: {file.FileName}\n" +
                            "Allowed formats: PDF, DOCX, XLSX\n" +
                            "Please convert your file and try again.");
                        break;
                    }

                    if (file.Length > 10 * 1024 * 1024) // 10MB
                    {
                        ModelState.AddModelError(nameof(vm.Uploads),
                            $"📎 File too large: {file.FileName}\n" +
                            $"Size: {(file.Length / 1024f / 1024f):F2}MB (Max: 10MB)\n" +
                            "Please compress your file or use a smaller document.");
                        break;
                    }
                }
            }
        }

        private static string GenerateSafeFileName(string originalFileName)
        {
            var baseName = Path.GetFileNameWithoutExtension(originalFileName);
            var ext = Path.GetExtension(originalFileName);
            var safeBase = Regex.Replace(baseName, @"[^a-zA-Z0-9\-_]+", "-").Trim('-');
            return $"{safeBase}-{Guid.NewGuid():N}{ext}";
        }

        private async Task<Lecturer> GetCurrentLecturerAsync()
        {
            var userEmail = User.Identity?.Name;
            var userName = HttpContext.Session.GetUserName();

            Lecturer lecturer = null;

            // Method 1: Try matching by email from Identity
            if (!string.IsNullOrEmpty(userEmail))
            {
                lecturer = await _db.Lecturers
                    .FirstOrDefaultAsync(l => l.Email == userEmail && l.IsActive);
            }

            // Method 2: Fallback to session user name if email match fails
            if (lecturer == null && !string.IsNullOrEmpty(userName))
            {
                lecturer = await _db.Lecturers
                    .FirstOrDefaultAsync(l => l.Name.Contains(userName) && l.IsActive);
            }

            // Method 3: Final fallback - get first active lecturer (for testing/development)
            if (lecturer == null)
            {
                lecturer = await _db.Lecturers
                    .FirstOrDefaultAsync(l => l.IsActive);
            }

            return lecturer;
        }
    }
}