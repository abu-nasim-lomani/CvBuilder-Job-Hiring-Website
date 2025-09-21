using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvBuilder.Models
{
    public class Skill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } // e.g., "C#", "ASP.NET Core", "Photoshop"

        [StringLength(50)]
        public string? Proficiency { get; set; } // e.g., "Expert", "Intermediate", "Beginner"

        // Foreign Key to UserProfile
        public int UserProfileId { get; set; }

        [ForeignKey("UserProfileId")]
        public virtual UserProfile? UserProfile { get; set; }
    }
}