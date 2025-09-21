using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvBuilder.Data;
using CvBuilder.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CvBuilder.Controllers
{
    [Authorize]
    public class SkillsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SkillsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<UserProfile?> GetCurrentUserProfileAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;
            return await _context.UserProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        }

        // POST: Skills/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Proficiency")] Skill skill)
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return Unauthorized();

            skill.UserProfileId = userProfile.Id;
            ModelState.Remove(nameof(skill.UserProfile));

            if (ModelState.IsValid)
            {
                _context.Add(skill);
                await _context.SaveChangesAsync();
            }
            // Redirect back to the profile page
            return RedirectToAction("Index", "UserProfiles");
        }

        // POST: Skills/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return Unauthorized();

            var skill = await _context.Skills
                .FirstOrDefaultAsync(s => s.Id == id && s.UserProfileId == userProfile.Id);

            if (skill != null)
            {
                _context.Skills.Remove(skill);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "UserProfiles");
        }
    }
}