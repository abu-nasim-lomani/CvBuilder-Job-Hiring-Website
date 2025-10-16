using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Foreign Key for the user from AspNetUsers table
        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public virtual IdentityUser? ApplicationUser { get; set; }

        // New, corrected code
        public virtual List<Education>? Educations { get; set; }
        public virtual List<Experience>? Experiences { get; set; }
        public virtual List<Skill>? Skills { get; set; }
        public virtual List<Project>? Projects { get; set; }
    }
}