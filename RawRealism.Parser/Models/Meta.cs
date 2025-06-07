namespace RawRealism.Parser.Models;

public class Author
{
    public required string Name { get; set; }
    public string? Publisher { get; set; }
}

public class Image
{
    // Images are first copied and stored in the same output directory (/content/etc..) as the content file,
    // but when copied to the www directory, they are moved to the "img/posts/[en or nl]/[year]/[month]" directory.
    public string? RelativeUrl { get; set; }
    public required string Alt { get; set; } // Alternative text for the image, used for accessibility and SEO.
    public string? Caption { get; set; } // Text over the image, maybe for copyrights.
}

public class Meta
{
    public required string Locale { get; set; } // e.g., "en-US", "nl-NL", etc.
    public required string DateIso8601 { get; set; } // ISO 8601 format for date, e.g., "2023-10-01T12:00:00Z" (for RSS feeds)
    public required string PostTitle { get; set; } // Title of the blog post, used in the HTML title tag and RSS feed title
    public required string Description { get; set; } // Short description for SEO, social media and RSS feeds
    public required string Slug { get; set; } // "/content/posts/[language]/[year]/[month]/[slug].html" - Note: no [day], this is cleaner.
    public required string SubTitle { get; set; } // Optional subtitle for the blog post
    public required string Intro { get; set; } // Optional introduction for the blog post.
    public required Image Image { get; set; } // Image for the blog post, used in the header and social media previews.
    public required Author Author { get; set; } // Author information.
    public required string[] Tags { get; set; } // Tags are used for categorization and(!) hashtags in the content. And for SEO.
    public required string? Category { get; set; } // Category of the blog post, used for categorization and SEO.

    // Properties below are propagated later, so that's why they can be null here, but not in the final output.
    public string? RelativeUrl { get; set; } // Relative URL for the blog post, e.g., "/posts/en/2023/10/my-blog-post.html". Canonical url is relative url + site.Domain.
    public string? ContentPath { get; set; } // The path to the content file, e.g., "/content/posts/en/2023/10/my-blog-post.md"
    public string? Language { get; set; } // e.g., "en", "nl", etc. (for directory structure and URLs)
    public string? MarkDownContent { get; set; } // The actual content of the blog post in mixed Markdown/HTML format.
    public string? PostDirectory { get; set; } // The directory where the post is stored, e.g., "[archive/archief]/2023/10/"
    public string? PostLangDirName { get; set; } // The directory name for the language, e.g., "archief or "archive".
    public string? ImageDirectory { get; set; } // The directory where the image is stored, e.g., "/img/en/2023/10/"

    // Extra
    public string YearToday { get; set; } = DateTime.Now.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture); // For copyright in the footer, e.g., "2023".
}

// For directory structure and URL generation, we need to extract the year and month from the date.
public class YearMonth
{
    public required string Yyyy { get; set; }
    public required string Mm { get; set; }
}

