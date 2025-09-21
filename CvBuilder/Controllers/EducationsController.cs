using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvBuilder.Data;
using CvBuilder.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CvBuilder.Controllers
{
    [Authorize]
    public class EducationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EducationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<UserProfile?> GetCurrentUserProfileAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;
            return await _context.UserProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        }

        // GET: Educations
        public async Task<IActionResult> Index()
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return RedirectToAction("Index", "UserProfiles");

            var educations = await _context.Educations
                .Where(e => e.UserProfileId == userProfile.Id)
                .ToListAsync();

            return View(educations);
        }

        // GET: Educations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Educations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Institution,Degree,FieldOfStudy,StartDate,EndDate,Grade")] Education education)
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null)
            {
                ModelState.AddModelError("", "User profile not found. Please create a profile first.");
                return View(education);
            }

            education.UserProfileId = userProfile.Id;
            ModelState.Remove(nameof(education.UserProfile));

            if (ModelState.IsValid)
            {
                _context.Add(education);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "UserProfiles"); // Go back to the main profile page
            }
            return View(education);
        }

        // The secure Edit, Details, Delete methods are also needed here.
        // The ones from Step 14 are correct. Let me include them for completeness.

        // GET: Educations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return NotFound();
            var education = await _context.Educations.FirstOrDefaultAsync(m => m.Id == id && m.UserProfileId == userProfile.Id);
            if (education == null) return NotFound();
            return View(education);
        }

        // GET: Educations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return NotFound();
            var education = await _context.Educations.FirstOrDefaultAsync(e => e.Id == id && e.UserProfileId == userProfile.Id);
            if (education == null) return NotFound();
            return View(education);
        }

        // POST: Educations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Institution,Degree,FieldOfStudy,StartDate,EndDate,Grade,UserProfileId")] Education education)
        {
            if (id != education.Id) return NotFound();
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null || education.UserProfileId != userProfile.Id) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(education);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Educations.Any(e => e.Id == education.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Index", "UserProfiles"); // Go back to the main profile page
            }
            return View(education);
        }

        // GET: Educations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return NotFound();
            var education = await _context.Educations.FirstOrDefaultAsync(m => m.Id == id && m.UserProfileId == userProfile.Id);
            if (education == null) return NotFound();
            return View(education);
        }

        // POST: Educations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return Forbid();
            var education = await _context.Educations.FirstOrDefaultAsync(e => e.Id == id && e.UserProfileId == userProfile.Id);
            if (education != null)
            {
                _context.Educations.Remove(education);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "UserProfiles"); // Go back to the main profile page
        }
    }
}