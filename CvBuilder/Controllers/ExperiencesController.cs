using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvBuilder.Data;
using CvBuilder.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CvBuilder.Controllers
{
    [Authorize]
    public class ExperiencesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExperiencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<UserProfile?> GetCurrentUserProfileAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;
            return await _context.UserProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        }

        // GET: Experiences
        public async Task<IActionResult> Index()
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return RedirectToAction("Index", "UserProfiles");

            var experiences = await _context.Experiences
                .Where(e => e.UserProfileId == userProfile.Id)
                .ToListAsync();

            return View(experiences);
        }

        // GET: Experiences/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Experiences/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CompanyName,Position,StartDate,EndDate,Description")] Experience experience)
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null)
            {
                ModelState.AddModelError("", "User profile not found.");
                return View(experience);
            }

            experience.UserProfileId = userProfile.Id;
            ModelState.Remove(nameof(experience.UserProfile));

            if (ModelState.IsValid)
            {
                _context.Add(experience);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "UserProfiles");
            }

            return View(experience);
        }

        // GET: Experiences/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return NotFound();

            var experience = await _context.Experiences
                .FirstOrDefaultAsync(e => e.Id == id && e.UserProfileId == userProfile.Id);

            if (experience == null) return NotFound();

            return View(experience);
        }

        // POST: Experiences/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CompanyName,Position,StartDate,EndDate,Description,UserProfileId")] Experience experience)
        {
            if (id != experience.Id) return NotFound();

            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null || experience.UserProfileId != userProfile.Id) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(experience);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Experiences.Any(e => e.Id == experience.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Index", "UserProfiles");
            }
            return View(experience);
        }

        // GET: Experiences/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return NotFound();

            var experience = await _context.Experiences
                .FirstOrDefaultAsync(m => m.Id == id && m.UserProfileId == userProfile.Id);

            if (experience == null) return NotFound();

            return View(experience);
        }

        // POST: Experiences/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return Forbid();

            var experience = await _context.Experiences
                .FirstOrDefaultAsync(e => e.Id == id && e.UserProfileId == userProfile.Id);

            if (experience != null)
            {
                _context.Experiences.Remove(experience);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "UserProfiles");
        }
    }
}