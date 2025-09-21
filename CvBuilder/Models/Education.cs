using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CvBuilder.Models
{
    public class Education
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Institution name is required.")]
        [StringLength(150)]
        public string Institution { get; set; }

        [Required(ErrorMessage = "Degree is required.")]
        [StringLength(100)]
        public string Degree { get; set; }

        [StringLength(100)]
        public string? FieldOfStudy { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        [StringLength(50)]
        public string? Grade { get; set; }

        // Foreign Key to UserProfile table
        public int UserProfileId { get; set; }

        [ForeignKey("UserProfileId")]
        public virtual UserProfile? UserProfile { get; set; }
    }
}