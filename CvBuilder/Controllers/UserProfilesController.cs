using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvBuilder.Data;
using CvBuilder.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using CvBuilder.Services;
using System.Diagnostics;
using System.IO;

namespace CvBuilder.Controllers
{
    [Authorize]
    public class UserProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserProfilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: UserProfiles
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

            if (userProfile == null)
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Details), new { id = userProfile.Id });
        }

        // GET: UserProfiles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userProfile = await GetFullUserProfileAsync();

            // Security check: ensure the id from URL matches the logged-in user's profile
            if (userProfile == null || userProfile.Id != id)
            {
                return NotFound();
            }

            return View(userProfile);
        }

        // GET: UserProfiles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: UserProfiles/Create
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

        // GET: UserProfiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userProfile = await GetFullUserProfileAsync();

            if (userProfile == null || userProfile.Id != id)
            {
                return NotFound();
            }

            return View(userProfile);
        }

        // POST: UserProfiles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Phone,Address,Profession,Summary,LinkedInProfileUrl,WebsiteUrl,ApplicationUserId")] UserProfile userProfile)
        {
            if (id != userProfile.Id) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userProfile.ApplicationUserId != userId) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userProfile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.UserProfiles.Any(e => e.Id == userProfile.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(userProfile);
        }

        // GET: UserProfiles/BuildCv
        public async Task<IActionResult> BuildCv()
        {
            var userProfile = await GetFullUserProfileAsync();
            if (userProfile == null)
            {
                return RedirectToAction(nameof(Index));
            }

            string latexCode = LatexCvGenerator.Generate(userProfile);
            return View((object)latexCode);
        }

        // POST: /UserProfiles/CompilePdf
        [HttpPost]
        public IActionResult CompilePdf([FromForm] string latexCode)
        {
            if (string.IsNullOrEmpty(latexCode)) return BadRequest("LaTeX code cannot be empty.");

            var tempId = Guid.NewGuid().ToString();
            var tempDir = Path.Combine(Path.GetTempPath(), tempId);
            Directory.CreateDirectory(tempDir);
            var texFilePath = Path.Combine(tempDir, "cv.tex");
            var pdfFilePath = Path.Combine(tempDir, "cv.pdf");

            try
            {
                System.IO.File.WriteAllText(texFilePath, latexCode);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "pdflatex",
                    Arguments = $"-output-directory=\"{tempDir}\" -interaction=nonstopmode \"{texFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (var process = Process.Start(processStartInfo))
                {
                    process.WaitForExit(30000);
                    if (!System.IO.File.Exists(pdfFilePath))
                    {
                        string errorLog = process.StandardOutput.ReadToEnd() + "\n" + process.StandardError.ReadToEnd();
                        return StatusCode(500, "PDF compilation failed. Error: " + errorLog);
                    }
                }

                byte[] pdfBytes = System.IO.File.ReadAllBytes(pdfFilePath);
                return File(pdfBytes, "application/pdf", "MyCV_LaTeX.pdf");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        #region Helper Methods

        // Helper method to get the current user's complete profile with all related data.
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

        // Note: Delete methods were omitted for brevity but should be in your final code.
        // They can also be updated to use the helper method for extra security checks if needed.
        // GET: UserProfiles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var userProfile = await GetFullUserProfileAsync();
            if (userProfile == null || userProfile.Id != id) return NotFound();
            return View(userProfile);
        }

        // POST: UserProfiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userProfile = await GetFullUserProfileAsync();
            if (userProfile == null || userProfile.Id != id) return Forbid();

            _context.UserProfiles.Remove(userProfile);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}