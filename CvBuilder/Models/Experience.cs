using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvBuilder.Models
{
    public class Experience
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; }

        [Required]
        [StringLength(100)]
        public string Position { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; } // Nullable for current job

        public string? Description { get; set; }

        // Foreign Key to UserProfile
        public int UserProfileId { get; set; }

        [ForeignKey("UserProfileId")]
        public virtual UserProfile? UserProfile { get; set; }
    }
}