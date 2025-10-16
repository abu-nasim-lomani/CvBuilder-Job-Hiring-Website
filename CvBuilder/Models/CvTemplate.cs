// CvTemplate.cs
public class CvTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } // যেমন: "Modern Chronological", "Classic Professional"
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; } // টেমপ্লেটের ছোট ছবি দেখানোর জন্য
    public string LatexCode { get; set; } // প্লেসহোল্ডারসহ মূল LaTeX কোড
}