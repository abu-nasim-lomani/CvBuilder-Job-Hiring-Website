using CvBuilder.Models;
using System.Linq;
using System.Text;

namespace CvBuilder.Services
{
    public class LatexCvGenerator
    {
        public string Generate(CvTemplate template, UserProfile profile)
        {
            if (template == null || profile == null)
            {
                return string.Empty;
            }

            string finalLatex = template.LatexCode;

            finalLatex = finalLatex.Replace("[[HEADER]]", BuildHeader(profile));
            finalLatex = finalLatex.Replace("[[SUMMARY_SECTION]]", BuildSummarySection(profile));
            finalLatex = finalLatex.Replace("[[EXPERIENCE_SECTION]]", BuildExperienceSection(profile));
            finalLatex = finalLatex.Replace("[[EDUCATION_SECTION]]", BuildEducationSection(profile));
            finalLatex = finalLatex.Replace("[[SKILLS_SECTION]]", BuildSkillsSection(profile));
            finalLatex = finalLatex.Replace("[[PROJECTS_SECTION]]", BuildProjectsSection(profile));

            return finalLatex;
        }

        private string Escape(string? input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            var sb = new StringBuilder(input);
            sb.Replace(@"\", @"\textbackslash{}");
            sb.Replace("&", @"\&");
            sb.Replace("%", @"\%");
            sb.Replace("$", @"\$");
            sb.Replace("#", @"\#");
            sb.Replace("_", @"\_");
            sb.Replace("{", @"\{");
            sb.Replace("}", @"\}");
            sb.Replace("~", @"\textasciitilde{}");
            sb.Replace("^", @"\textasciicircum{}");
            return sb.ToString();
        }

        private string BuildHeader(UserProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"\begin{center}");
            sb.AppendLine($@"{{\Huge \bfseries {Escape(profile.FullName)}}} \\");
            sb.AppendLine(@"\vspace{2mm}");
            if (!string.IsNullOrEmpty(profile.Address)) sb.AppendLine($@"{Escape(profile.Address)} \\");
            if (!string.IsNullOrEmpty(profile.Phone)) sb.Append($@"{Escape(profile.Phone)} $\cdot$ ");
            if (profile.ApplicationUser != null) sb.AppendLine($@"\href{{mailto:{Escape(profile.ApplicationUser.Email)}}}{{{Escape(profile.ApplicationUser.Email)}}} \\");
            if (!string.IsNullOrEmpty(profile.WebsiteUrl)) sb.Append($@"\href{{{Escape(profile.WebsiteUrl)}}}{{{Escape(profile.WebsiteUrl)}}} $\cdot$ ");
            if (!string.IsNullOrEmpty(profile.LinkedInProfileUrl)) sb.AppendLine($@"\href{{{Escape(profile.LinkedInProfileUrl)}}}{{{Escape(profile.LinkedInProfileUrl)}}}");
            sb.AppendLine(@"\end{center}");
            sb.AppendLine(@"\vspace{5mm}");
            return sb.ToString();
        }

        private string BuildSummarySection(UserProfile profile)
        {
            if (string.IsNullOrEmpty(profile.Summary)) return "";
            var sb = new StringBuilder();
            sb.AppendLine(@"\section*{Summary}");
            sb.AppendLine(Escape(profile.Summary));
            sb.AppendLine(@"\vspace{5mm}");
            return sb.ToString();
        }

        private string BuildExperienceSection(UserProfile profile)
        {
            if (profile.Experiences?.Any() != true) return "";
            var sb = new StringBuilder();
            sb.AppendLine(@"\section*{Experience}");
            foreach (var exp in profile.Experiences.OrderByDescending(e => e.StartDate))
            {
                sb.AppendLine($@"\noindent {{\bfseries {Escape(exp.Position)}}} \hfill {exp.StartDate:MMM, yyyy} -- {exp.EndDate?.ToString("MMM, yyyy") ?? "Present"} \\");
                sb.AppendLine($@"\noindent {{\itshape {Escape(exp.CompanyName)}}} \\");
                if (!string.IsNullOrEmpty(exp.Description))
                {
                    sb.AppendLine(@"\begin{itemize}[leftmargin=*]");
                    sb.AppendLine($@"\item {Escape(exp.Description)}");
                    sb.AppendLine(@"\end{itemize}");
                }
                sb.AppendLine(@"\vspace{3mm}");
            }
            return sb.ToString();
        }

        private string BuildEducationSection(UserProfile profile)
        {
            if (profile.Educations?.Any() != true) return "";
            var sb = new StringBuilder();
            sb.AppendLine(@"\section*{Education}");
            foreach (var edu in profile.Educations.OrderByDescending(e => e.StartDate))
            {
                sb.AppendLine($@"\noindent {{\bfseries {Escape(edu.Institution)}}} \hfill {edu.StartDate:MMM, yyyy} -- {edu.EndDate?.ToString("MMM, yyyy") ?? "Present"} \\");
                sb.AppendLine($@"\noindent {Escape(edu.Degree)} in {Escape(edu.FieldOfStudy)} \\");
                if (!string.IsNullOrEmpty(edu.Grade)) sb.AppendLine($@"\noindent Grade: {Escape(edu.Grade)} \\");
                sb.AppendLine(@"\vspace{3mm}");
            }
            return sb.ToString();
        }

        private string BuildSkillsSection(UserProfile profile)
        {
            if (profile.Skills?.Any() != true) return "";
            var sb = new StringBuilder();
            sb.AppendLine(@"\section*{Skills}");
            sb.AppendLine(string.Join(", ", profile.Skills.Select(s => Escape(s.Name))));
            sb.AppendLine(@"\vspace{5mm}");
            return sb.ToString();
        }

        private string BuildProjectsSection(UserProfile profile)
        {
            if (profile.Projects?.Any() != true) return "";
            var sb = new StringBuilder();
            sb.AppendLine(@"\section*{Projects}");
            foreach (var proj in profile.Projects)
            {
                sb.AppendLine($@"\noindent {{\bfseries {Escape(proj.Name)}}} -- \href{{{Escape(proj.Url)}}}{{{Escape(proj.Url)}}} \\");
                sb.AppendLine($@"{Escape(proj.Description)}\\");
                sb.AppendLine(@"\vspace{3mm}");
            }
            return sb.ToString();
        }
    }
}