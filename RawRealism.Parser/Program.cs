﻿using Microsoft.Extensions.Configuration;
using RawRealism.Parser.Models;
using System.Runtime.InteropServices;
using System.Text.Json;
using Markdig;

namespace RawRealism.Parser;

internal class Program
{
    static void Main(string[] args)
    {
        ///
        /// TODO
        ///
        /// - Create statric templates with a <head></head> template for several pages, like index.html, about.html, etc.
        ///
        /// - Generate Index.html, sitemap.xml, feed.xml, and other static files.
        ///
        /// - Use <meta property="og:type" content="website" /> (lowercase `og` is important!) for Index.html ('article' for blog posts)
        ///
        /// - Add date to each blog post, maybe before `intro`? Only date, not the time,
        ///     and it should look like this: `1 oktober 2023` (in Dutch) or `October 1, 2023` (in English).
        ///
        /// - Default <head></head> in "index.html, about.html, and other? pages.
        ///     Need a default `<head>` template for the index.html, about.html, and other pages?
        ///     But in 2 languages, English and Dutch, so we need to load the correct template based on the language.
        ///
        /// - Check language of browser with JavaScript, only use Dutch if browser language is Dutch, otherwise English.
        ///     For Index.html, about.html, "Over" or "About" (etc).
        ///     Maybe select a language at top of the page, so the user can switch between languages?
        ///
        /// - Create and image converter, so we can convert images to WebP format with 75% quality, and a width of 800px, and variable height.
        ///     Maybe use jpeg for images, if I don't need an extra external library for that, but WebP is better for quality and size.
        ///     Use ImageSharp for this. Supports Webp too.
        ///
        /// END TODO
        ///


        ///
        /// Start of the program
        ///
        /// Step 1
        ///
        /// check if the appsettings.*.json file exists and load it.
        ///

        // Determine environment based on OS, Windows is Development, others are Production
        // TODO: Use environment variable or command line argument to set environment
        string environment = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "Development"
            : "Production";

        // Print the Base Directory for debugging purposes, we need to load appsettings.*.json from there
        //Console.WriteLine($"AppContext Base Directory: {AppContext.BaseDirectory}");

        // Build configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .Build();

        // Bind to Site class
        var site = config.GetSection("Settings").Get<Site>();
        if (site == null)
        {
            ExitError("ERROR: Failed to load 'appsettigs.*.json'.");
            return;
        }

        // If last character from site.Url is `/` remove it, for better/clearer processing later on
        if (site.Domain.EndsWith('/'))
            site.Domain = site.Domain.TrimEnd('/');

        // For debugging purposes, print the loaded appsettings
        //Console.WriteLine($"Loaded appsetting for: {site.Environment} {site.Name} ({site.Domain})");
        //Console.WriteLine($"ProjectRoot: {site.ProjectRoot}");
        //Console.WriteLine($"GitHubParserRoot: {site.GitHubParserRoot}");

        if (!Directory.Exists(site.ProjectRoot))
            ExitError($"ERROR: Directory [site.ProjectRoot]: '{site.ProjectRoot}' doesn't exist.");

        ///
        /// Step 2
        /// Load the markdown file `content.md` from /RawRealism/content/
        /// For every new blogpost I use the same file `content.md` (for now) to write the new content.
        /// I'm doing this because I want to figure out the title (and slug) when writing my blogpost.
        /// So then I have my title and slug when the parser runs and it will create a new file in the:
        /// '/RawRealism/content/posts/[language]/[year]/[month]' directory, with the day (with leading zero if necessary) as the filename as the first part of the filename.
        /// The first, top part of the markdown file has the properties of the blogpost, like title, slug, date, etc. in JSON format.
        /// Then there's the divider `---` and then the mixed Markdown/HTML content of the blogpost.
        /// I use HTML in the markdown file, so I can use some special HTML tags, not supported by markdown.
        ///

        // Path to the new blog post source, the `content.md` file.

        var contentPathRoot = Path.Combine(site.ProjectRoot, "content");
        var contentMdPath = Path.Combine(contentPathRoot, "post_content.md");
        if (!File.Exists(contentMdPath))
            ExitError($"ERROR: File [post_content.md] '{contentMdPath}' does not exist.");

        // Read the content of the content.md file and parse it to a Meta object
        Meta contentMetaData = LoadMetaFromMarkdown(contentMdPath);
        contentMetaData.Language = contentMetaData.Locale.Length >= 2 ? contentMetaData.Locale[..2] : contentMetaData.Locale;

        ///
        /// Step 3, Image handling
        ///

        string contentImgPath = Path.Combine(contentPathRoot, "post_content.webp");
        contentMetaData.Image.Alt = contentMetaData.Image.Alt;

        ///
        /// Step 4
        /// Previously I made an error in my thought process. I wrongly thought I could directly parse the markdown file and generate the HTML file from it, but now we
        /// actually need to copy the `content.md` file to the `www/content/posts/[language]/[year]/[month]` directory, with the slugified title as the filename, like `my-first-post.html`.
        /// So the `/content/posts/` directory is the directory where the sources of the blog posts  (the `.md` files) are stored, and the `content.md` file is the template for a new blog post.
        ///

        /// Copy the content.md file to the target directory, with the slugified title as the filename
        YearMonth yearMonth = GetYearMonthFromIso8601(contentMetaData.DateIso8601);
        string targetContentDir = Path.Combine(site.ProjectRoot, "content", "posts", contentMetaData.Language, yearMonth.Yyyy, yearMonth.Mm);
        Directory.CreateDirectory(targetContentDir);

        string targetFileName = $"{contentMetaData.Slug}.md";
        string targetFilePath = Path.Combine(targetContentDir, targetFileName);

        // Check if the target file already exists, if it does, exit with an error, we should not overwrite existing blog posts!
        // We can always edit the sources of the blog posts, but we should not overwrite existing blog posts.
        if (File.Exists(targetFilePath))
            ExitError($"ERROR: Target file '{targetFilePath}' already exists.");
        File.Copy(contentMdPath, targetFilePath, overwrite: false);

        // Copy the image file to the target directory too.
        // The image file is copied to the same directory as the content file, so it can be used in the blog post.
        string targetImageName = $"{contentMetaData.Slug}.webp";
        string targetImagePath = Path.Combine(targetContentDir, targetImageName);
        if (File.Exists(targetImagePath))
            ExitError($"ERROR: Target image file '{targetImagePath}' already exists.");
        File.Copy(contentImgPath, targetImagePath, overwrite: false);

        ///
        /// Step 5
        /// After the source file is copied, I need to recursively enumerate all .md files in the target directory, and then parse them to HTML files.
        /// So, every time we generate all the HTML files.
        ///

        // Target Root Directory for for/each
        string targetRootDir = Path.Combine(site.ProjectRoot, "content", "posts");
        var indexPosts = new List<IndexPost>();
        foreach (string mdFile in Directory.EnumerateFiles(targetRootDir, "*.md", SearchOption.AllDirectories))
        {
            // Read the contents of the current .md file
            //string fileContent = File.ReadAllText(mdFile);

            contentMetaData = LoadMetaFromMarkdown(mdFile);
            if (contentMetaData == null)
            {
                Console.WriteLine($"ERROR: Skipping file '{mdFile}' due to null contentMetaData.");
                continue;
            }

            contentMetaData.Language = contentMetaData.Locale.Length >= 2 ? contentMetaData.Locale[..2] : contentMetaData.Locale;
            contentMetaData.ContentPath = Path.GetDirectoryName(mdFile);

            contentMetaData.PostLangDirName = contentMetaData.Locale switch
            {
                "nl_NL" => "archief",
                _ => "archive"
            };

            contentMetaData.PostDirectory = Path.Combine(site.ProjectRoot, "www", contentMetaData.PostLangDirName, yearMonth.Yyyy, yearMonth.Mm);
            contentMetaData.ImageDirectory = Path.Combine(site.ProjectRoot, "www", "img", contentMetaData.Language!, yearMonth.Yyyy, yearMonth.Mm);

            Directory.CreateDirectory(contentMetaData.PostDirectory);
            Directory.CreateDirectory(contentMetaData.ImageDirectory);

            // Generate Relative URL
            //contentMetaData.RelativeUrl = $"/{contentMetaData.PostLangDirName}/{yearMonth.Yyyy}/{yearMonth.Mm}/{contentMetaData.Slug}.html";
            contentMetaData.RelativeUrl = $"/{contentMetaData.PostLangDirName}/{yearMonth.Yyyy}/{yearMonth.Mm}/{contentMetaData.Slug}";
            // Generate Relative Image URL
            contentMetaData.Image.RelativeUrl = $"/img/{contentMetaData.Language}/{yearMonth.Yyyy}/{yearMonth.Mm}/{contentMetaData.Slug}.webp";

            GenerateBlogPostHtml(contentMetaData, site);

            indexPosts.Add(new IndexPost
            {
                Lang = contentMetaData.Language.ToUpper(), // Use uppercase for the language code, e.g., "EN", "NL" for Display on Index.html
                Title = contentMetaData.PostTitle,
                SubTitle = contentMetaData.SubTitle,
                RelativeUrl = contentMetaData.RelativeUrl,
                Description = contentMetaData.Description,
                Date = DateTimeOffset.Parse(contentMetaData.DateIso8601).UtcDateTime,
                DateIso8601 = contentMetaData.DateIso8601,
                DisplayDateLang = DateTimeOffset
                    .Parse(contentMetaData.DateIso8601).UtcDateTime
                    .ToString(contentMetaData.Locale == "nl_NL" ? "d MMMM yyyy" : "MMMM d, yyyy",
                    System.Globalization.CultureInfo.GetCultureInfo(contentMetaData.Locale)),
                Author = contentMetaData.Author.Name,
                ImageRelativeUrl = contentMetaData.Image.RelativeUrl,
                ImageAlt = contentMetaData.Image.Alt,
                Tags = contentMetaData.Tags,
                Category = contentMetaData.Category,
                Intro = contentMetaData.Intro
            });
        }
        GenerateIndexHtml(indexPosts, site, contentMetaData!.YearToday);
        GenerateRss(indexPosts, site);
    }

    private static void GenerateRss(List<IndexPost> indexPosts, Site site)
    {
        var now = DateTime.UtcNow;

        // Local Function Helper to create a channel for a given language
        // Local functions need to be declared before calling them, so we declared it here.
        void CreateFeed(string lang, string fileName)
        {
            var posts = indexPosts
                .Where(p => p.Lang.Equals(lang, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.Date)
                .ToList();

            if (posts.Count == 0) return;

            var channel = new RssChannel
            {
                Title = "RSS Feed " +  site.Name + (lang == "NL" ? " (Nederlands)" : " (English)"),
                Link = site.Domain + "/",
                Image = site.Domain + "/img/rawrealism-header.webp",
                Description = lang == "NL" ? "Laatste Nederlandstalige posts van Raw Realism" : "Latest English posts from Raw Realism",
                Language = lang == "NL" ? "nl-NL" : "en-US",
                Copyright = $"Copyright © {now.Year} Raw Realism. " + (lang == "NL" ? "Alle rechten voorbehouden" : "All rights reserved"),
                Generator = site.Generator,
                LastBuildDate = now,
                // Instead of `.ToList()`, we use the array initializer syntax to create a new array = Simplified Collection initialization
                RssItems = [.. posts.Select(p => new RssItem
                {
                    Title = p.Title,
                    Link = site.Domain + p.RelativeUrl,
                    Description = p.Description,
                    Category = p.Category ?? "",
                    EnclosureUrl = !string.IsNullOrEmpty(p.ImageRelativeUrl) ? site.Domain + p.ImageRelativeUrl : "",
                    EnclosureType = !string.IsNullOrEmpty(p.ImageRelativeUrl) ? "image/webp" : "",
                    PubDate = p.Date,
                    Guid = site.Domain + p.RelativeUrl,
                    Author = p.Author
                })]
            };

            // Serialize to XML (simple manual approach)
            var xml = GenerateRssXml(channel);
            var path = Path.Combine(site.ProjectRoot, "www", fileName);
            File.WriteAllText(path, xml, System.Text.Encoding.UTF8);
            Console.WriteLine($"RSS feed generated: {path}");
        }

        CreateFeed("EN", "feed-en.xml");
        CreateFeed("NL", "feed-nl.xml");
    }

    // Minimal RSS XML generator (expand as needed)
    private static string GenerateRssXml(RssChannel channel)
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

    private static void GenerateIndexHtml(List<IndexPost> indexPosts, Site site, string YearToday) // TODO, refactor YearToday.
    {
        // Load the index template from the templates directory
        string IndexTemplate = Path.Combine(site.ProjectRoot, "content", "templates", "index.html");
        if (!File.Exists(IndexTemplate))
            ExitError($"ERROR: File [index.html] '{IndexTemplate}' does not exist.");

        string indexHtml = File.ReadAllText(IndexTemplate);
        string articlesHtml = string.Join("\n", indexPosts
            .OrderByDescending(s => s.Date)
            .Select(s =>
                $"<article><time>{s.DisplayDateLang}</time><h2><a href=\"{s.RelativeUrl}\">{s.Title}</a></h2><h3>{s.SubTitle}</h3>{s.Description}</article><hr>"));

        // TODO: Refactor, with JavaScript (browser language detection) we can select the correct language for the index.html page.
        indexHtml = indexHtml
            .Replace("{{ content Articles }}", articlesHtml)
            .Replace("{{ config Site.Name }}", site.Name)
            .Replace("{{ var YearToday }}", YearToday);

        string indexFilePath = Path.Combine(site.ProjectRoot, "www", "index.html");
        File.WriteAllText(indexFilePath, indexHtml, System.Text.Encoding.UTF8);
    }

    private static void GenerateBlogPostHtml(Meta contentMetaData, Site site)
    {
        // Prepare values
        YearMonth yearMonth = GetYearMonthFromIso8601(contentMetaData.DateIso8601);

        // Convert Markdown to HTML
        string postMd2HtmlContent = Markdown.ToHtml(contentMetaData.MarkDownContent!);

        //
        // Load the post_header template
        //
        string postHeaderTemplate = Path.Combine(site.ProjectRoot, "content", "templates", "post_top.html");
        if (!File.Exists(postHeaderTemplate))
            ExitError($"ERROR: File [post_top.html] '{postHeaderTemplate}' does not exist.");

        string postHeaderHtml = File.ReadAllText(postHeaderTemplate);
        postHeaderHtml = postHeaderHtml
            .Replace("{{ meta PostTitle }}", contentMetaData.PostTitle)
            .Replace("{{ meta SubTitle }}", contentMetaData.SubTitle)
            .Replace("{{ meta Image.Alt }}", contentMetaData.Image.Alt)
            .Replace("{{ meta Intro }}", contentMetaData.Intro)
            .Replace("{{ meta Image.Url }}", contentMetaData.Image.RelativeUrl);

        //
        // Load the post_footer template
        //
        string footerTemplate = Path.Combine(site.ProjectRoot, "content", "templates", "footer.html");
        if (!File.Exists(postHeaderTemplate))
            ExitError($"ERROR: File [footer.html] '{footerTemplate}' does not exist.");

        string postFooterHtml = File.ReadAllText(footerTemplate);
        string textAbout = (contentMetaData.Language == "nl") ? "Over" : "About";
        //string pagesAbout = $"{textAbout.ToLower()}.html";
        string pagesAbout = $"{textAbout.ToLower()}"; // no .html extension, we use the /pages/ directory for static pages.
        string pagesAboutPath = $"/pages/{contentMetaData.Language}/{pagesAbout}";

        postFooterHtml = postFooterHtml
            .Replace("{{ config Site.Name }}", site.Name)
            .Replace("{{ var YearToday }}", contentMetaData.YearToday)
            .Replace("{{ link About }}", pagesAboutPath)
            .Replace("{{ var TextAbout }}", textAbout);

        //
        // Load the post_layout template
        //
        string postLayoutTemplate = Path.Combine(site.ProjectRoot, "content", "templates", "post_layout.html");
        if (!File.Exists(postLayoutTemplate))
            ExitError($"ERROR: File [post_layout.html] '{postLayoutTemplate}' does not exist.");

        string postLayoutHtml = File.ReadAllText(postLayoutTemplate);

        // Replace placeholders with actual values
        postLayoutHtml = postLayoutHtml
            .Replace("{{ meta Language }}", contentMetaData.Language)
            .Replace("{{ meta PostTitle }}", contentMetaData.PostTitle)
            .Replace("{{ meta Image.Url }}", site.Domain + contentMetaData.Image.RelativeUrl)
            .Replace("{{ config Site.Name }}", site.Name)
            .Replace("{{ meta Description }}", contentMetaData.Description)
            .Replace("{{ meta DateIso8601 }}", contentMetaData.DateIso8601)
            .Replace("{{ meta Category }}", contentMetaData.Category)
            .Replace("{{ meta Locale }}", contentMetaData.Locale)
            .Replace("{{ meta Author.Name }}", contentMetaData.Author.Name)
            .Replace("{{ config Site.Generator }}", site.Generator)
            .Replace("{{ meta CanonicalUrl }}", site.Domain + contentMetaData.RelativeUrl);

        // Copy the image file to the img directory
        string imageFileName = $"{contentMetaData.Slug}.webp";
        string imageSourcePath = Path.Combine(contentMetaData.ContentPath!, imageFileName);
        string imageDestPath = Path.Combine(site.ProjectRoot, "www", "img", contentMetaData.Language!, yearMonth.Yyyy, yearMonth.Mm, imageFileName);
        File.Copy(imageSourcePath, imageDestPath, overwrite: true);
        //Console.WriteLine($"Image file copied: {imageDestPath}");

        // TODO Check if meta properties are null, if so, don't use the meta property in the HTML, but remove it from the layoutHtml string.
        // Do the same as with the Tag placeholder for these properties, generate the HTML here. Not in the layout.html file.

        // Tags
        if (contentMetaData.Tags != null && contentMetaData.Tags.Length > 0)
        {
            string tagsHtml = string.Join(Environment.NewLine, contentMetaData.Tags.Select(tag => $"<meta property=\"article:tag\" content=\"{tag}\" />"));
            postLayoutHtml = postLayoutHtml.Replace("{{ array Tags }}", tagsHtml);
        }
        else
        {
            postLayoutHtml = postLayoutHtml.Replace("{{ array Tags }}", string.Empty);
        }

        postLayoutHtml = postLayoutHtml
            .Replace("{{ include post_top.html }}", postHeaderHtml)
            .Replace("{{ include post_content.md }}", postMd2HtmlContent)
            .Replace("{{ include footer.html }}", postFooterHtml);

        // Save the generated HTML to the appropriate directory
        string htmlFileName = $"{contentMetaData.Slug}.html";
        string htmlFilePath = Path.Combine(site.ProjectRoot, "www", contentMetaData.PostLangDirName!, yearMonth.Yyyy, yearMonth.Mm, htmlFileName);
        File.WriteAllText(htmlFilePath, postLayoutHtml, System.Text.Encoding.UTF8);
        Console.WriteLine($"HTML file created: {htmlFilePath}");
    }

    private static Meta LoadMetaFromMarkdown(string filePath)
    {
        if (!File.Exists(filePath))
            ExitError($"ERROR: File '{filePath}' does not exist.");

        var lines = File.ReadAllLines(filePath);

        // Find the first divider line (---)
        int dividerIndex = Array.FindIndex(lines, line => line.Trim().StartsWith("---"));
        if (dividerIndex == -1)
            ExitError($"ERROR: No '---' divider found in '{filePath}'.");

        // Extract JSON (everything before the divider)
        string json = string.Join(Environment.NewLine, lines.Take(dividerIndex)).Trim();

        // Deserialize JSON to Meta
        Meta? contentMetaData;
        try
        {
            contentMetaData = JsonSerializer.Deserialize<Meta>(json, CachedSerializerOptions);
        }
        catch (Exception ex)
        {
            ExitError($"ERROR: Failed to deserialize Meta in '{filePath}': {ex.Message}");
            return null!; // Unreachable, but for compiler safety
        }

        if (contentMetaData == null)
        {
            ExitError($"ERROR: Meta is null after deserialization in '{filePath}'.");
            return null!; // Unreachable, but for compiler safety
        }

        // Extract Markdown content (everything after the divider)
        contentMetaData.MarkDownContent = string.Join(Environment.NewLine, lines.Skip(dividerIndex + 1)).Trim();

        // For debugging: print some meta info
        //Console.WriteLine($"Meta loaded: {contentMetaData.PostTitle} ({contentMetaData.Locale}) - {contentMetaData.DateIso8601}");
        //Console.WriteLine($"Markdown content length: {contentMetaData.MarkDownContent.Length}");

        return contentMetaData;
    }

    private static void ExitError(string message)
    {
        Console.Error.WriteLine(message);
        Environment.Exit(1);
        return; // unreachable, but for compiler safety. Environment.Exit will terminate the process
    }

    // Cache the JsonSerializerOptions instance as a static readonly field
    private static readonly JsonSerializerOptions CachedSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static YearMonth GetYearMonthFromIso8601(string iso8601Date) // TODO, move this to a separate utility class, so we can use it in other parts of the code.
    {
        DateTimeOffset dto = DateTimeOffset.Parse(iso8601Date);
        DateTime utcTime = dto.UtcDateTime;

        return new YearMonth
        {
            Yyyy = utcTime.Year.ToString(),
            Mm = utcTime.Month.ToString("D2")
        };
    }
}
