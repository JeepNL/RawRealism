namespace RawRealism.Parser.Models;

public class RssChannel
{
    public required string Title { get; set; } // Title can contain special characters like "é" or "ë"
    public required string Link { get; set; } // Link to the website or blog that this RSS feed belongs to.
    public string Image { get; set; } = string.Empty; // URL to the image that represents the channel, e.g., a logo or banner.
    public required string Description { get; set; } // Description can contain special characters like "é" or "ë"
    public required string Language { get; set; } // e.g., "en_US" for English (United States), "nl_NL" for Dutch (Netherlands), etc.
    public required string Copyright { get; set; } // e.g., "Copyright © 2025-{{ var YearToday }} Raw Realism. All rights reserved."
    public required string Generator { get; set; }// e.g., "Jaap's Minimalistic RSS Feed Generator"
    public required DateTime LastBuildDate { get; set; } //e.g., "Sun, 08 Jun 2025 15:57:30 +0000" (RFC 822 format? but with +0000 instead of 'GMT')
    public List<RssItem> RssItems { get; set; } = [];
}

public class RssItem
{
    public required string Title { get; set; } // Title can contain special characters like "é" or "ë"
    public required string Link { get; set; } // Use the slugified URL of the post, e.g., "https://example.com/blog/my-first-post"
    public required string Description { get; set; } // Description can contain special characters like "é" or "ë"
    public string Category { get; set; } = string.Empty; // e.g., "Blog", "Tutorial", etc.
    public string EnclosureUrl { get; set; } = string.Empty; // URL to the image that represents the post
    public string EnclosureType { get; set; } = string.Empty; // always "image/webp" for images (I create them manually)
    public required DateTime PubDate { get; set; } //e.g., "Sun, 08 Jun 2025 15:57:30 +0000" (RFC 822 format? but with +0000 instead of 'GMT')
    public required string Guid { get; set; } // Is the canonical URL of the post, not a GUID, and 'Permalink' should be set to true.
    public string Author { get; set; } = string.Empty; // Optional.
}
