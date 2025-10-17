using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CvBuilder.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? Profession { get; set; }

        public string? Summary { get; set; }

        [Url]
        public string? LinkedInProfileUrl { get; set; }

        [Url]
        public string? WebsiteUrl { get; set; }

        // This property stores the user's design preferences as a JSON string
        public string? StylePreferencesJson { get; set; }

        // Foreign Key to the Identity User table (AspNetUsers)
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public virtual IdentityUser? ApplicationUser { get; set; }

        // Navigation properties to related CV sections
        public virtual List<Education>? Educations { get; set; }
        public virtual List<Experience>? Experiences { get; set; }
        public virtual List<Skill>? Skills { get; set; }
        public virtual List<Project>? Projects { get; set; }

        // Helper method to safely read and deserialize the style preferences JSON
        public CvStylePreferences GetStylePreferences()
        {
            if (string.IsNullOrEmpty(StylePreferencesJson))
            {
                // If no preferences are saved, return a new default object
                return new CvStylePreferences();
            }

            try
            {
                // Otherwise, deserialize the saved JSON string
                return JsonSerializer.Deserialize<CvStylePreferences>(StylePreferencesJson) ?? new CvStylePreferences();
            }
            catch (JsonException)
            {
                // If the saved JSON is invalid for any reason, return a default object to prevent crashing
                return new CvStylePreferences();
            }
        }
    }
}