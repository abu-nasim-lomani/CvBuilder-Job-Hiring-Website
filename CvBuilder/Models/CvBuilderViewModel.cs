using System.Collections.Generic;

namespace CvBuilder.Models
{
    public class CvBuilderViewModel
    {
        public UserProfile UserProfile { get; set; }
        public IEnumerable<CvTemplate> AvailableTemplates { get; set; }
        public int SelectedTemplateId { get; set; }
    }
}