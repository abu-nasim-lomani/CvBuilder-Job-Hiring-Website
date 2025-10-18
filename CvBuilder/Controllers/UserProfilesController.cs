using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvBuilder.Data;
using CvBuilder.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using CvBuilder.Services;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace CvBuilder.Controllers
{
    [Authorize]
    public class UserProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LatexCvGenerator _latexCvGenerator;

        public UserProfilesController(ApplicationDbContext context, LatexCvGenerator latexCvGenerator)
        {
            _context = context;
            _latexCvGenerator = latexCvGenerator;
        }

        // GET: /UserProfiles/Builder
        [HttpGet]
        public async Task<IActionResult> Builder()
        {
            var userProfile = await GetFullUserProfileAsync();
            if (userProfile == null)
            {
                // If the user has no profile, redirect them to create one
                return RedirectToAction("Index");
            }

            var templates = await _context.CvTemplates.ToListAsync();

            var viewModel = new CvBuilderViewModel
            {
                UserProfile = userProfile,
                AvailableTemplates = templates,
                SelectedTemplateId = templates.FirstOrDefault()?.Id ?? 0
            };

            return View(viewModel);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GeneratePreview()
        {
            // 1. Get template ID from form
            if (!int.TryParse(Request.Form["SelectedTemplateId"], out var templateId))
            {
                return BadRequest("Invalid Template ID.");
            }
            var template = await _context.CvTemplates.FindAsync(templateId);
            if (template == null)
            {
                return NotFound("Template not found.");
            }

            // 2. Create an empty UserProfile to bind form data to
            var userProfileForPreview = new UserProfile();

            // 3. Bind all form data (including new Experience items)
            // IMPORTANT: Make sure your form names match the UserProfile properties
            // For lists like Experiences, ensure names are like 'UserProfile.Experiences[0].Position'
            await TryUpdateModelAsync(userProfileForPreview, "UserProfile",
                p => p.FullName, p => p.Profession, p => p.Summary,
                p => p.Phone, p => p.Address, p => p.LinkedInProfileUrl, p => p.WebsiteUrl, // Added basic fields
                p => p.Educations, p => p.Experiences, p => p.Skills, p => p.Projects // Added list fields
            );

            // 4. Add ApplicationUser info from the original profile (needed for email in template)
            var originalProfile = await GetFullUserProfileAsync();
            if (originalProfile == null) return Unauthorized();
            userProfileForPreview.ApplicationUser = originalProfile.ApplicationUser; // Crucial for header

            // 5. Generate LaTeX code using the form-bound data
            string latexCode = _latexCvGenerator.Generate(template, userProfileForPreview);

            // 6. Compile PDF using the CORRECT method name
            var (pdfBytes, error) = await CompileLatexToPdfAsync(latexCode); // <-- Corrected name
            if (pdfBytes == null)
            {
                return StatusCode(500, error);
            }

            return File(pdfBytes, "application/pdf");
        }

        #region Standard CRUD Actions (Index, Details, Create, etc.)

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            return userProfile == null ? RedirectToAction(nameof(Create)) : RedirectToAction(nameof(Details), new { id = userProfile.Id });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var userProfile = await GetFullUserProfileAsync();
            if (userProfile == null || userProfile.Id != id) return NotFound();
            return View(userProfile);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Phone,Address,Profession,Summary,LinkedInProfileUrl,WebsiteUrl")] UserProfile userProfile)
        {
            ModelState.Remove("ApplicationUserId");
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                userProfile.ApplicationUserId = userId;
                _context.Add(userProfile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(userProfile);
        }

        #endregion

        #region Private Helper Methods

        private async Task<(byte[]? pdfBytes, string errorMessage)> CompileLatexToPdfAsync(string latexCode)
        {
            if (string.IsNullOrEmpty(latexCode))
            {
                return (null, "LaTeX code cannot be empty.");
            }

            return await Task.Run(() =>
            {
                var tempId = Guid.NewGuid().ToString();
                var tempDir = Path.Combine(Path.GetTempPath(), tempId);
                try
                {
                    Directory.CreateDirectory(tempDir);
                    System.IO.File.WriteAllText(Path.Combine(tempDir, "cv.tex"), latexCode);

                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = @"C:\texlive\2025\bin\windows\pdflatex.exe",
                        Arguments = $"-output-directory=\"{tempDir}\" -interaction=nonstopmode cv.tex",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    using (var process = Process.Start(processStartInfo))
                    {
                        if (process == null) return (null, "Failed to start pdflatex process.");
                        bool exited = process.WaitForExit(30000);
                        if (!exited) { process.Kill(true); return (null, "PDF compilation timed out."); }

                        var pdfPath = Path.Combine(tempDir, "cv.pdf");
                        if (!System.IO.File.Exists(pdfPath))
                        {
                            return (null, "Compilation failed. Log: " + process.StandardOutput.ReadToEnd());
                        }

                        byte[] pdfBytes;
                        using (var fs = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var ms = new MemoryStream()) { fs.CopyTo(ms); pdfBytes = ms.ToArray(); }
                        }
                        return (pdfBytes, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    return (null, "A critical server error occurred: " + ex.ToString());
                }
                finally
                {
                    for (int i = 0; i < 5; i++)
                    {
                        try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); break; }
                        catch (IOException) { Thread.Sleep(100); }
                    }
                }
            });
        }

        private async Task<UserProfile?> GetFullUserProfileAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;

            return await _context.UserProfiles
                .Include(p => p.ApplicationUser)
                .Include(p => p.Educations)
                .Include(p => p.Experiences)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        }


        [HttpGet]
        public IActionResult AddExperienceItem()
        {
            // Create a new, empty Experience object to pass to the partial view
            var newExperience = new Experience();

            // Return the partial view, passing in the empty model
            return PartialView("_ExperienceItem", newExperience);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Important for security
        public async Task<IActionResult> DeleteExperienceItem(int experienceId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var experience = await _context.Experiences
                .FirstOrDefaultAsync(e => e.Id == experienceId && e.UserProfile.ApplicationUserId == userId);

            if (experience == null)
            {
                return NotFound(); // Or Forbidden()
            }

            _context.Experiences.Remove(experience);
            await _context.SaveChangesAsync();

            // Return a success status, no content needed
            return Ok();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExperienceField(int experienceId, string fieldName, string value)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var experience = await _context.Experiences
                .FirstOrDefaultAsync(e => e.Id == experienceId && e.UserProfile.ApplicationUserId == userId);

            if (experience == null)
            {
                return NotFound(); // Or Forbidden()
            }

            // Use reflection to update the correct property based on fieldName
            var propertyInfo = typeof(Experience).GetProperty(fieldName);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                try
                {
                    // Convert the value to the property's type and set it
                    var convertedValue = Convert.ChangeType(value, propertyInfo.PropertyType);
                    propertyInfo.SetValue(experience, convertedValue, null);

                    _context.Experiences.Update(experience);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Saved ✓" }); // Send success confirmation
                }
                catch (Exception ex)
                {
                    // Handle potential errors during conversion or saving
                    return BadRequest($"Error updating field '{fieldName}': {ex.Message}");
                }
            }
            else
            {
                return BadRequest($"Invalid field name: {fieldName}");
            }
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBuilderData()
        {
            // Fetch the user's current profile, including existing experiences, from the DB
            var userProfileFromDb = await GetFullUserProfileAsync();
            if (userProfileFromDb == null)
            {
                return Unauthorized();
            }

            try
            {
                // Try to apply ALL incoming form data (including new/edited experiences)
                // onto the existing database model.
                // ASP.NET Core's model binder is smart enough to handle adding/updating list items.
                await TryUpdateModelAsync(userProfileFromDb, "UserProfile",
                    p => p.FullName, p => p.Profession, p => p.Summary,
                    p => p.Phone, p => p.Address, p => p.LinkedInProfileUrl, p => p.WebsiteUrl,
                    p => p.Educations, p => p.Experiences // Crucial: Include the list name
                                                          // Add Skills, Projects lists here if they are editable in the form
                );

                // Save all changes (new items, updated items) to the database
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "All changes saved successfully!" });
            }
            catch (Exception ex)
            {
                // Log the detailed error in a real application
                Console.WriteLine($"Error saving builder data: {ex}"); // For debugging
                return StatusCode(500, new { success = false, message = "An error occurred while saving changes." });
            }
        }


        // GET: /UserProfiles/AddEducationItem
        [HttpGet]
        public IActionResult AddEducationItem()
        {
            return PartialView("_EducationItem", new Education());
        }

        // POST: /UserProfiles/DeleteEducationItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducationItem(int educationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var education = await _context.Educations
                .FirstOrDefaultAsync(e => e.Id == educationId && e.UserProfile.ApplicationUserId == userId);

            if (education == null)
            {
                return NotFound();
            }

            _context.Educations.Remove(education);
            await _context.SaveChangesAsync();
            return Ok();
        }

        #endregion
    }
}