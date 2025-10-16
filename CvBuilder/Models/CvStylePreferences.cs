using System.Collections.Generic;

namespace CvBuilder.Models
{
    // This class holds all the styling and layout options for a user's CV
    public class CvStylePreferences
    {
        // Layout & Content
        public List<string> SectionOrder { get; set; } = new List<string> { "Summary", "Experience", "Education", "Projects", "Skills" };
        public Dictionary<string, bool> SectionVisibility { get; set; } = new Dictionary<string, bool>();

        // Design & Style
        public string PrimaryColor { get; set; } = "#000000"; // Default to black
        public string TextColor { get; set; } = "#333333"; // Default dark gray

        public string HeadingFont { get; set; } = "Arial";
        public string BodyFont { get; set; } = "Arial";

        public int BodyTextSize { get; set; } = 11;
        public int NameSize { get; set; } = 24;
    }
}