using CvBuilder.Models;
using System.Text;

namespace CvBuilder.Services
{
    public static class LatexCvGenerator
    {
        // Helper to escape special LaTeX characters
        private static string Escape(string? input)
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

        public static string Generate(UserProfile profile)
        {
            var sb = new StringBuilder();

            // Preamble
            sb.AppendLine(@"\documentclass[a4paper,11pt]{article}");
            sb.AppendLine(@"\usepackage[margin=1in]{geometry}");
            sb.AppendLine(@"\usepackage{enumitem}");
            sb.AppendLine(@"\setlength{\parindent}{0pt}");
            sb.AppendLine(@"\pagestyle{empty}");

            // Document Start
            sb.AppendLine(@"\begin{document}");

            // Header - Name and Contact Info
            sb.AppendLine($@"\begin{{center}}");
            sb.AppendLine($@"{{\Huge \bfseries {Escape(profile.FullName)}}} \\");
            sb.AppendLine($@"\vspace{{2mm}}");
            if (!string.IsNullOrEmpty(profile.Address)) sb.AppendLine($@"{Escape(profile.Address)} \\");
            if (!string.IsNullOrEmpty(profile.Phone)) sb.Append($@"{Escape(profile.Phone)} $\cdot$ ");
            sb.AppendLine($@"\href{{mailto:{Escape(profile.ApplicationUser?.Email)}}}{{{Escape(profile.ApplicationUser?.Email)}}} \\");
            if (!string.IsNullOrEmpty(profile.WebsiteUrl)) sb.Append($@"\href{{{Escape(profile.WebsiteUrl)}}}{{{Escape(profile.WebsiteUrl)}}} $\cdot$ ");
            if (!string.IsNullOrEmpty(profile.LinkedInProfileUrl)) sb.AppendLine($@"\href{{{Escape(profile.LinkedInProfileUrl)}}}{{{Escape(profile.LinkedInProfileUrl)}}}");
            sb.AppendLine(@"\end{center}");
            sb.AppendLine(@"\vspace{5mm}");

            // Summary
            if (!string.IsNullOrEmpty(profile.Summary))
            {
                sb.AppendLine(@"\section*{Summary}");
                sb.AppendLine(Escape(profile.Summary));
                sb.AppendLine(@"\vspace{5mm}");
            }

            // Experience
            if (profile.Experiences?.Any() == true)
            {
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
            }

            // Education
            if (profile.Educations?.Any() == true)
            {
                sb.AppendLine(@"\section*{Education}");
                foreach (var edu in profile.Educations.OrderByDescending(e => e.StartDate))
                {
                    sb.AppendLine($@"\noindent {{\bfseries {Escape(edu.Institution)}}} \hfill {edu.StartDate:MMM, yyyy} -- {edu.EndDate?.ToString("MMM, yyyy") ?? "Present"} \\");
                    sb.AppendLine($@"\noindent {Escape(edu.Degree)} in {Escape(edu.FieldOfStudy)} \\");
                    if (!string.IsNullOrEmpty(edu.Grade)) sb.AppendLine($@"\noindent Grade: {Escape(edu.Grade)} \\");
                    sb.AppendLine(@"\vspace{3mm}");
                }
            }

            // Skills
            if (profile.Skills?.Any() == true)
            {
                sb.AppendLine(@"\section*{Skills}");
                sb.AppendLine(string.Join(", ", profile.Skills.Select(s => Escape(s.Name))));
                sb.AppendLine(@"\vspace{5mm}");
            }

            // Projects
            if (profile.Projects?.Any() == true)
            {
                sb.AppendLine(@"\section*{Projects}");
                foreach (var proj in profile.Projects)
                {
                    sb.AppendLine($@"\noindent {{\bfseries {Escape(proj.Name)}}} -- \href{{{Escape(proj.Url)}}}{{{Escape(proj.Url)}}} \\");
                    sb.AppendLine($@"{Escape(proj.Description)}\\");
                    sb.AppendLine(@"\vspace{3mm}");
                }
            }

            // Document End
            sb.AppendLine(@"\end{document}");

            return sb.ToString();
        }
    }
}