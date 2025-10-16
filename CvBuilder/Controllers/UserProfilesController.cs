// Controllers/UserProfilesController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvBuilder.Data;
using CvBuilder.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using CvBuilder.Services; // নতুন সার্ভিস ব্যবহারের জন্য এটি যোগ করুন
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks; // async Task ব্যবহারের জন্য
using System.Linq; // LINQ ব্যবহারের জন্য

namespace CvBuilder.Controllers
{
    [Authorize]
    public class UserProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LatexCvGenerator _latexCvGenerator; // আমাদের নতুন সার্ভিসটি এখানে যোগ করেছি

        // Constructor আপডেট করেছি যাতে LatexCvGenerator সার্ভিসটি এখানে আসে
        public UserProfilesController(ApplicationDbContext context, LatexCvGenerator latexCvGenerator)
        {
            _context = context;
            _latexCvGenerator = latexCvGenerator;
        }

        // --- আমাদের নতুন পেজের জন্য মূল অ্যাকশন ---
        // GET: /UserProfiles/Builder
        [HttpGet]
        public async Task<IActionResult> Builder()
        {
            var userProfile = await GetFullUserProfileAsync();
            if (userProfile == null)
            {
                // যদি ব্যবহারকারীর কোনো প্রোফাইল না থাকে, তাকে নতুন প্রোফাইল তৈরির পেজে পাঠান
                return RedirectToAction("Create", "UserProfiles");
            }

            // ডাটাবেস থেকে সব টেমপ্লেট লোড করুন
            var templates = await _context.CvTemplates.ToListAsync();

            // আমাদের ViewModel বা "মডেল বক্স" তৈরি করুন
            var viewModel = new CvBuilderViewModel
            {
                UserProfile = userProfile,
                AvailableTemplates = templates,
                // ডিফল্টভাবে প্রথম টেমপ্লেটটি সিলেক্ট করে রাখুন
                SelectedTemplateId = templates.FirstOrDefault()?.Id ?? 0
            };

            // ViewModel-টিকে নতুন একটি View ফাইলে পাঠান
            return View(viewModel);
        }


        // --- AJAX call এর জন্য নতুন Action ---
        // POST: /UserProfiles/GeneratePreview
        [HttpPost]
        public async Task<IActionResult> GeneratePreview([FromForm] UserProfile userProfile, [FromForm] int templateId)
        {
            var template = await _context.CvTemplates.FindAsync(templateId);
            if (template == null)
            {
                return NotFound("Template not found.");
            }

            var originalProfile = await GetFullUserProfileAsync();
            if (originalProfile == null)
            {
                return Unauthorized();
            }

            userProfile.ApplicationUser = originalProfile.ApplicationUser;

            string latexCode = _latexCvGenerator.Generate(template, userProfile);

            var pdfBytes = CompileLatexToPdf(latexCode, out string error);
            if (pdfBytes == null)
            {
                return BadRequest(error);
            }

            return File(pdfBytes, "application/pdf");
        }


        // --- PDF কম্পাইল করার জন্য Helper Method ---
        private byte[]? CompileLatexToPdf(string latexCode, out string errorMessage)
        {
            errorMessage = string.Empty;
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
                        errorMessage = "PDF compilation failed. Log: " + process.StandardOutput.ReadToEnd() + "\n" + process.StandardError.ReadToEnd();
                        return null;
                    }
                }
                return System.IO.File.ReadAllBytes(pdfFilePath);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
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
            if (userProfile == null || userProfile.Id != id) return NotFound();
            return View(userProfile);
        }

        // GET: UserProfiles/Create
        public IActionResult Create() => View();

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

        // --- Helper Methods ---
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

        // --- (আমরা Edit, Delete, এবং পুরোনো BuildCv/CompilePdf মেথডগুলো পরে যোগ করব) ---
    }
}