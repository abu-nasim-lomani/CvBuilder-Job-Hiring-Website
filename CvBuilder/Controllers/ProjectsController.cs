using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CvBuilder.Data;
using CvBuilder.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CvBuilder.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<UserProfile?> GetCurrentUserProfileAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;
            return await _context.UserProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Url")] Project project)
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return Unauthorized();

            project.UserProfileId = userProfile.Id;
            ModelState.Remove(nameof(project.UserProfile));

            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "UserProfiles");
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Url,UserProfileId")] Project project)
        {
            if (id != project.Id) return NotFound();

            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null || project.UserProfileId != userProfile.Id) return Forbid();

            if (ModelState.IsValid)
            {
                _context.Update(project);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "UserProfiles");
        }


        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null) return Unauthorized();

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserProfileId == userProfile.Id);

            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "UserProfiles");
        }
    }
}