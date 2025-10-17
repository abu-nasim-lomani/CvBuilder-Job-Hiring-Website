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

        // POST: /UserProfiles/GeneratePreview
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GeneratePreview(int selectedTemplateId) // সরাসরি প্যারামিটার হিসেবে গ্রহণ করুন
        {
            var template = await _context.CvTemplates.FindAsync(selectedTemplateId); // সরাসরি ব্যবহার করুন
            if (template == null)
            {
                return NotFound("Template not found.");
            }

            var userProfileFromDb = await GetFullUserProfileAsync();
            if (userProfileFromDb == null)
            {
                return Unauthorized();
            }

            await TryUpdateModelAsync(userProfileFromDb, "UserProfile", p => p.FullName, p => p.Profession, p => p.Summary);

            string latexCode = _latexCvGenerator.Generate(template, userProfileFromDb);

            var (pdfBytes, error) = await CompileLatexToPdfAsync(latexCode);
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

        #endregion
    }
}