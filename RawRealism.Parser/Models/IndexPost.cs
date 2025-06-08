namespace RawRealism.Parser.Models;

public class IndexPost // Class representing a summary of a blog post, used for listing posts on the website and in RSS feeds.
{
    public required string Lang { get; set; }
    public required string Title { get; set; }
    public required string SubTitle { get; set; }
    public required string Description { get; set; }
    public required string RelativeUrl { get; set; } // Canonical url is relative url + site.Domain.
    public required string DateIso8601 { get; set; }
    public required DateTime Date { get; set; } // This is a DateTime, not a string, for easier manipulation and formatting.
    public required string DisplayDateLang { get; set; } // e.g., `1 oktober 2023` (in Dutch) or `October 1, 2023` (in English).
    public required string Author { get; set; }
    public string? Intro { get; set; }
    public string? ImageRelativeUrl { get; set; }
    public string? ImageAlt { get; set; }
    public string[]? Tags { get; set; }
    public string? Category { get; set; }
}
