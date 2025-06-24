using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;

namespace RawRealism.Parser.Models;

public static class Utils
{
    public static void ExitError(string message)
    {
        Console.Error.WriteLine(message);
        Environment.Exit(1);
        return; // unreachable, but for compiler safety. Environment.Exit will terminate the process
    }

    public static YearMonth GetYearMonthFromIso8601(string iso8601Date) // Changed to public
    {
        DateTimeOffset dto = DateTimeOffset.Parse(iso8601Date);
        DateTime utcTime = dto.UtcDateTime;

        return new YearMonth
        {
            Yyyy = utcTime.Year.ToString(),
            Mm = utcTime.Month.ToString("D2")
        };
    }

    // Cnvert image to WebP format, with 75% quality and a width of 800px, and variable height.
    public static void ConvertImage(string inputImgFile, string outputImgFile)
    {
        try
        {
            using Image image = Image.Load(inputImgFile);
            // Resize the image to a width of 800px, maintaining the aspect ratio
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(800, 0), // Width 800px, height is auto-calculated
                Mode = ResizeMode.Max
            }));
            // Save the image as WebP with 75% quality
            image.Save(outputImgFile, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder { Quality = 75 });
            //Console.WriteLine($"(Pages) Image encoded and saved: {outputImgFile}");
        }
        catch (Exception ex)
        {
            ExitError($"ERROR: Failed to process image '{inputImgFile}': {ex.Message}");
        }
    }

    // Minimal RSS XML generator (expand as needed)
    public static string GenerateRssXml(RssChannel channel)
    {
        var itemsXml = string.Join("\n", channel.RssItems.Select(item =>
            $"""
        <item>
            <title>{System.Security.SecurityElement.Escape(item.Title)}</title>
            <link>{item.Link}</link>
            <description>{System.Security.SecurityElement.Escape(item.Description)}</description>
            <category>{item.Category}</category>
            {(string.IsNullOrEmpty(item.EnclosureUrl) ? "" : $"<enclosure url=\"{item.EnclosureUrl}\" type=\"{item.EnclosureType}\" />")}
            <pubDate>{item.PubDate:R}</pubDate>
            <guid isPermaLink="true">{item.Guid}</guid>
            {(string.IsNullOrEmpty(item.Author) ? "" : $"<author>{item.Author}</author>")}
        </item>
        """));

        return
            $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
        <channel>
            <title>{System.Security.SecurityElement.Escape(channel.Title)}</title>
            <link>{channel.Link}</link>
            <description>{System.Security.SecurityElement.Escape(channel.Description)}</description>
            <language>{channel.Language}</language>
            <copyright>{System.Security.SecurityElement.Escape(channel.Copyright)}</copyright>
            <generator>{System.Security.SecurityElement.Escape(channel.Generator)}</generator>
            <lastBuildDate>{channel.LastBuildDate:R}</lastBuildDate>
            {(string.IsNullOrEmpty(channel.Image) ? "" : $"<image><url>{channel.Image}</url><title>{System.Security.SecurityElement.Escape(channel.Title)}</title><link>{channel.Link}</link></image>")}
            {itemsXml}
        </channel>
        </rss>
        """;
    }

    // Cache the JsonSerializerOptions instance as a static readonly field
    public static readonly JsonSerializerOptions CachedSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

}
