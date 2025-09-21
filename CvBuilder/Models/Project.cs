using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvBuilder.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Url]
        public string? Url { get; set; }

        // Foreign Key to UserProfile
        public int UserProfileId { get; set; }

        [ForeignKey("UserProfileId")]
        public virtual UserProfile? UserProfile { get; set; }
    }
}