using Markdig;
using Microsoft.Extensions.Configuration;
using RawRealism.Parser.Models;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace RawRealism.Parser;

internal class Program
{
    static void Main(string[] args)
    {
        ///
        /// TODO
        ///
        /// Strucure:
        ///     1) default root pages, index, 404.
        ///     2) default pages, about, contact etc.
        ///     3) posts
        ///
        /// - Generate Index.html, sitemap.xml, feed.xml, and other static files.
        ///
        /// - Support, webp, png, and jpg as input image formats.
        ///
        /// - Check language of browser with JavaScript, only use Dutch if browser language is Dutch, otherwise English.
        ///     For Index.html, about.html, "Over" or "About" (etc).
        ///     Maybe select a language at top of the page, so the user can switch between languages?
        ///
        /// END TODO
        ///

        // Determine environment based on OS, Windows is Development, others are Production
        // TODO: Use environment variable or command line argument to set environment
        string environment = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "Development"
            : "Production";

        // Set the path delimiter based on the OS
        string pDl = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";

        /// Step 1, check if the appsettings.*.json file exists and load it.
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
            Utils.ExitError("ERROR: Failed to load 'appsettigs.*.json'.");
            return;
        }

        // If last character from site.Url is `/` remove it, for better/clearer processing later on
        if (site.Domain.EndsWith('/'))
            site.Domain = site.Domain.TrimEnd('/');

        if (!Directory.Exists(site.ProjectRoot))
            Utils.ExitError($"ERROR: Directory [site.ProjectRoot]: '{site.ProjectRoot}' doesn't exist.");

        /// Step 2, Load the markdown file `post.md` from /RawRealism/content/
        string contentRootPath = Path.Combine(site.ProjectRoot, "content");
        string newPostName = "new_post";
        string newPostMd = $"{newPostName}.md";
        string newPostImg = $"{newPostName}.webp";
        string newPostMdPath = Path.Combine(contentRootPath, newPostMd);
        string newPostImgPath = Path.Combine(contentRootPath, newPostImg);

        if (!File.Exists(newPostMdPath))
            Utils.ExitError($"ERROR: File '{newPostMdPath}' does not exist.");
        if (!File.Exists(newPostImgPath))
            Utils.ExitError($"ERROR: File '{newPostImgPath}' does not exist.");

        // Read the content of the post.md file and parse it to a Meta object
        Meta? contentMetaData = LoadMetaFromMarkdown(newPostMdPath);
        if (contentMetaData == null)
        {
            Utils.ExitError($"ERROR: Failed to load metadata from '{newPostMdPath}'.");
            return;
        }
        Meta? indexMetaData = null; // Used for the index.html page later on.

        /// Step 3, Copy the new post files to the target directory, with the slugified title as the filename
        YearMonth yearMonth = Utils.GetYearMonthFromIso8601(contentMetaData.DateIso8601);
        string targetContentDir = Path.Combine(site.ProjectRoot, "content", "traverse", "posts", contentMetaData.Lang, yearMonth.Yyyy, yearMonth.Mm);
        Directory.CreateDirectory(targetContentDir);

        string targetFilePath = Path.Combine(targetContentDir, $"{contentMetaData.Slug}.md");
        string targetImgPath = Path.Combine(targetContentDir, $"{contentMetaData.Slug}.webp");
        File.Copy(newPostMdPath, targetFilePath, overwrite: true);
        File.Copy(newPostImgPath, targetImgPath, overwrite: true);

        /// Step 4, Load all templates from the content/templates directory
        string templatesPath = Path.Combine(site.ProjectRoot, "content", "templates");
        if (!Directory.Exists(templatesPath))
            Utils.ExitError($"ERROR: Directory [templates] '{templatesPath}' does not exist.");

        // Enumerate all .html files in the content/templates directory and load them into a list of Template objects.
        var templates = new List<Template>();
        foreach (string templateFile in Directory.EnumerateFiles(templatesPath, "*.html"))
        {
            string templateContent = File.ReadAllText(templateFile);
            string templateName = Path.GetFileNameWithoutExtension(templateFile);
            templates.Add(new Template { Name = templateName, Content = templateContent });
        }

        /// Step 5, Enumerate all .md files in the content/traverse directory and generate HTML files for each (default) page and post.
        var indexPosts = new List<IndexPost>(); // for index.html and RSS feeds.
        string traverseContentPath = Path.Combine(site.ProjectRoot, "content", "traverse");
        foreach (string mdFile in Directory.EnumerateFiles(traverseContentPath, "*.md", SearchOption.AllDirectories))
        {
            // /pages/root/ -> 404.html and index.html
            // /pages/ -> about.html, contact.html, etc.
            // /traverse/posts/ -> posts in the format /posts/[language]/[year]/[month]/[slug].html

            contentMetaData = LoadMetaFromMarkdown(mdFile);
            if (contentMetaData == null)
            {
                Utils.ExitError($"ERROR: Failed to load metadata from '{mdFile}'.");
                return;
            }

            /// What do we have? 1) default root pages, 2) default pages, and 3) posts.
            /// But in the root we have 1 special page and that is the index.html page, with a list of posts.
            /// if path contains "posts" then it's a blog post, otherwise it's a default page, or a root page like index.html or 404.html.
            if (mdFile.Contains($"{pDl}posts{pDl}")) // pDl = path delimiter for windows and linux
            {
                contentMetaData.PageType = PageType.Post;
                string publishLangDir = contentMetaData.Lang == "nl" ? "archief" : "archive"; // Use "archief" for Dutch, "archive" for English.
                contentMetaData.PublishDir = Path.Combine(site.ProjectRoot, "www", publishLangDir, yearMonth.Yyyy, yearMonth.Mm);
                contentMetaData.Graphic.PublishDir = Path.Combine(site.ProjectRoot, "www", "img", contentMetaData.Lang, yearMonth.Yyyy, yearMonth.Mm);
                contentMetaData.RelativeUrl = $"/{publishLangDir}/{yearMonth.Yyyy}/{yearMonth.Mm}/{contentMetaData.Slug}.html"; // Relative URL for the post.
                contentMetaData.Graphic.RelativeUrl = $"/img/{contentMetaData.Lang}/{yearMonth.Yyyy}/{yearMonth.Mm}/{contentMetaData.Slug}.webp"; // Relative URL for the graphic.

                // Refactor this to create a sitemap to. Not only RSS. So we need to add another property to the Meta class: for example: Bool IsRss
                // and then put this add the end, and create a sitemap of all pages, but on and RSS when IsRss = true.,
                indexPosts.Add(new IndexPost // for index.html and RSS feeds.
                {
                    Lang = contentMetaData.Lang.ToUpper(), // Use uppercase for the language code, e.g., "EN", "NL" for Display on Index.html
                    Title = contentMetaData.PostTitle,
                    SubTitle = contentMetaData.SubTitle,
                    Description = contentMetaData.Description,
                    Date = DateTimeOffset.Parse(contentMetaData.DateIso8601).UtcDateTime,
                    DateIso8601 = contentMetaData.DateIso8601,
                    DisplayDateLang = DateTimeOffset
                        .Parse(contentMetaData.DateIso8601).UtcDateTime
                        .ToString(contentMetaData.Locale == "nl_NL" ? "d MMMM yyyy" : "MMMM d, yyyy",
                        System.Globalization.CultureInfo.GetCultureInfo(contentMetaData.Locale)),
                    Author = contentMetaData.Author.Name,
                    RelativeUrl = contentMetaData.RelativeUrl,
                    GraphicRelativeUrl = contentMetaData.Graphic.RelativeUrl,
                    GraphicAlt = contentMetaData.Graphic.Alt,
                    GraphicCaption = contentMetaData.Graphic.Caption,
                    Tags = contentMetaData.Tags,
                    Category = contentMetaData.Category,
                    Intro = contentMetaData.Intro
                });
            }
            else // root or default pages
            {
                if (mdFile.Contains($"{pDl}pages{pDl}root{pDl}")) // if path containts "root" then it's a default page in the www root, like index.html or 404.html.
                {
                    contentMetaData.PageType = mdFile.Contains("index.md") ? PageType.Index : PageType.Root; // index.html is special, it has a list of posts, so we set pageType to PageType.Index.
                    contentMetaData.PublishDir = Path.Combine(site.ProjectRoot, "www");
                    contentMetaData.RelativeUrl = contentMetaData.PageType == PageType.Index ? $"/" : $"/{contentMetaData.Slug}";
                    contentMetaData.Graphic.RelativeUrl = $"/img/{contentMetaData.Slug}.webp";
                }
                else // if (mdFile.Contains("/pages/"))
                {
                    contentMetaData.PageType = PageType.Default;
                    contentMetaData.PublishDir = Path.Combine(site.ProjectRoot, "www", "pages", contentMetaData.Lang);
                    contentMetaData.RelativeUrl = $"/pages/{contentMetaData.Lang}/{contentMetaData.Slug}";
                    contentMetaData.Graphic.RelativeUrl = $"/img/{contentMetaData.Slug}.webp";
                }
                // root and default pages use the same graphic directory, so we can use the same path.
                contentMetaData.Graphic.PublishDir = Path.Combine(site.ProjectRoot, "www", "img");
            }

            // Create the directories if they don't exist
            Directory.CreateDirectory(contentMetaData.PublishDir);
            Directory.CreateDirectory(contentMetaData.Graphic.PublishDir);

            // Image handling
            string? origImgPath = Path.Combine(Path.GetDirectoryName(mdFile)!, $"{contentMetaData.Slug}.webp");
            string encImgPath = Path.Combine(contentMetaData.Graphic.PublishDir, $"{contentMetaData.Slug}.webp");

            if (!File.Exists(origImgPath) || string.IsNullOrEmpty(origImgPath))
                Utils.ExitError($"ERROR: (Source) Image file '{origImgPath}' does not exist.");

            //Console.WriteLine($"origImgPath: {origImgPath}");
            //Console.WriteLine($"encImgPath: {encImgPath}");
            Utils.ConvertImage(origImgPath, encImgPath);

            // If `PageType.Index` save `contentMetaData` in another variable, so we can use it later for the index.html page.

            if (contentMetaData.PageType == PageType.Index) // Don't generate HTML for index.html yet, we will do that later, we need the list of posts first.
                indexMetaData = contentMetaData;
            else
                PublishPage(contentMetaData, site, templates); // Generate the HTML file for the page or post
        }

        // Last: Generate index.html and RSS feeds
        PublishPage(indexMetaData!, site, templates, indexPosts); // Generate the HTML file for index.html
        PublishRss(indexPosts, site);
    }

    private static void PublishPage(Meta contentMetaData, Site site, List<Template> templates, List<IndexPost>? indexPosts = null)
    {
        Console.WriteLine($"Processing file: {contentMetaData.Slug + ".md"}");

        // Load the default template content from the templates list
        string? defaultTemplateContent = templates.FirstOrDefault(t => t.Name == "page")?.Content;
        if (defaultTemplateContent == null)
        {
            Utils.ExitError("ERROR: Default template 'page.html' not found in templates directory.");
            return; // Unreachable, but for compiler safety
        }

        string pageHeaderTemplateName = (contentMetaData.PageType == PageType.Index) ? "page_header_index" : "page_header";
        string markdown2htmlContent = Markdown.ToHtml(contentMetaData.MarkdownContent);

        defaultTemplateContent = defaultTemplateContent
            .Replace("{{ include head.html }}", templates.FirstOrDefault(t => t.Name == "head")?.Content)
            .Replace("{{ include site_header.html }}", templates.FirstOrDefault(t => t.Name == "site_header")?.Content)
            .Replace("{{ var PageHeader }}", templates.FirstOrDefault(t => t.Name == pageHeaderTemplateName)?.Content)
            .Replace("{{ meta MarkdownContent }}", markdown2htmlContent)
            .Replace("{{ include footer.html }}", templates.FirstOrDefault(t => t.Name == "footer")?.Content);

        // footer
        bool dutchLang = contentMetaData.Lang == "nl";

        string textAbout = dutchLang ? "Over" : "About";
        string textPrivacy = "Privacy";
        string textContact = "Contact";
        string TextRss = "RSS";
        string pagesAbout = $"{textAbout.ToLower()}"; // no .html extension, we use the /pages/ directory for static pages.
        string pagesAboutPath = $"/pages/{contentMetaData.Lang}/{pagesAbout}";
        string pagesPrivacy = $"{textPrivacy.ToLower()}"; // no .html extension, we use the /pages/ directory for static pages.
        string pagesPrivacyPath = $"/pages/{contentMetaData.Lang}/{pagesPrivacy}";
        string pagesContact = $"{textContact.ToLower()}"; // no .html extension, we use the /pages/ directory for static pages.
        string pagesContactPath = $"/pages/{contentMetaData.Lang}/{pagesContact}";
        string pagesRss = $"{TextRss.ToLower()}";
        string pagesRssPath = $"/pages/{contentMetaData.Lang}/{pagesRss}";

        string TextRights = dutchLang ? "Alle rechten voorbehouden" : "All rights reserved";

        string postDate = DateTimeOffset // To display on the blog post page.
            .Parse(contentMetaData.DateIso8601).UtcDateTime
            .ToString(dutchLang ? "d MMMM yyyy" : "MMMM d, yyyy",
            System.Globalization.CultureInfo.GetCultureInfo(contentMetaData.Locale));

        string by = dutchLang ? "door" : "by";

        // Now we have the full defaultTemplateContent with the head, header and footer included.
        // We need to replace all of the placeholders with the actual content.
        defaultTemplateContent = defaultTemplateContent
            .Replace("{{ var BaseHref }}", (site.Environment == "Development") ? "/" : $"{site.Domain}/")
            .Replace("{{ meta Language }}", contentMetaData.Lang)
            .Replace("{{ meta PostTitle }}", contentMetaData.PostTitle)
            .Replace("{{ config Site.Name }}", site.Name)
            .Replace("{{ var CanonicalUrl }}", $"{site.Domain}{contentMetaData.RelativeUrl}")
            .Replace("{{ meta Description }}", contentMetaData.Description)
            .Replace("{{ meta Author.Name }}", contentMetaData.Author.Name)
            .Replace("{{ config Site.Generator }}", site.Generator)
            .Replace("{{ meta Graphic.Url }}", contentMetaData.Graphic.RelativeUrl)
            .Replace("{{ var FullGraphicUrl }}", $"{site.Domain}{contentMetaData.Graphic.RelativeUrl}")
            .Replace("{{ meta Graphic.Alt }}", contentMetaData.Graphic.Alt)
            .Replace("{{ meta Locale }}", contentMetaData.Locale)
            .Replace("{{ meta Category }}", contentMetaData.Category)
            .Replace("{{ meta OGType }}", contentMetaData.OGType)
            .Replace("{{ meta SubTitle }}", contentMetaData.SubTitle)
            .Replace("{{ meta Intro }}", contentMetaData.Intro)
            .Replace("{{ var YearToday }}", DateTime.Now.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture)) // footer
            .Replace("{{ meta DateIso8601 }}", contentMetaData.DateIso8601)
            .Replace("{{ meta MarkdownContent }}", contentMetaData.MarkdownContent)
            .Replace("{{ var YearToday }}", DateTime.Now.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture))
            .Replace("{{ var TextRights }}", TextRights)
            .Replace("{{ link About }}", pagesAboutPath)
            .Replace("{{ var TextAbout }}", textAbout)
            .Replace("{{ link Contact }}", pagesContactPath)
            .Replace("{{ var TextContact }}", textContact)
            .Replace("{{ link Privacy }}", pagesPrivacyPath)
            .Replace("{{ var TextPrivacy }}", textPrivacy)
            .Replace("{{ link Rss }}", pagesRssPath)
            .Replace("{{ var TextRss }}", TextRss)
            .Replace("{{ var PostDate }}", postDate)
            .Replace("{{ var ByAuthor }}", $", {by} {contentMetaData.Author.Name}");

        // Tags (in head.html)
        if (contentMetaData.Tags != null && contentMetaData.Tags.Length > 0)
        {
            string tagsHtml = string.Join(Environment.NewLine, contentMetaData.Tags.Select(tag => $"<meta property=\"article:tag\" content=\"{tag}\" />"));
            defaultTemplateContent = defaultTemplateContent.Replace("{{ array Tags }}", tagsHtml);
        }
        else // If there are no tags, remove the placeholder from the postHtml string.
        {
            defaultTemplateContent = defaultTemplateContent.Replace("{{ array Tags }}", string.Empty);
        }

        if (contentMetaData.PageType == PageType.Index)
        {
            string articlesHtml = string.Join("\n", indexPosts!
                .OrderByDescending(s => s.Date)
                .Select(s =>
                    $"<article><time datetime=\"{s.DateIso8601}\">{s.DisplayDateLang}</time><h2><a href=\"{s.RelativeUrl}\">{s.Title}</a></h2><h3>{s.SubTitle}</h3>{s.Description}</article><hr>"));

            // Remove the last <hr> tag, so we don't have a double <hr> tag with the footer.
            if (articlesHtml.EndsWith("<hr>"))
                articlesHtml = articlesHtml[..^"<hr>".Length];

            // TODO: Refactor, with JavaScript (browser language detection) we can select the correct language for the index.html page.
            defaultTemplateContent = defaultTemplateContent
                .Replace("{{ content Articles }}", articlesHtml);

            // Replace the <article> html start en end tags in the 'page.html' template with the <div> tags, so we can use the same template for the index.html page.
            defaultTemplateContent = defaultTemplateContent
                .Replace("<article", "<div")
                .Replace("</article>", "</div>");
        }

        File.WriteAllText(Path.Combine(contentMetaData.PublishDir, contentMetaData.Slug + ".html"), defaultTemplateContent, Encoding.UTF8);
    }

    private static void PublishRss(List<IndexPost> indexPosts, Site site)
    {
        var now = DateTime.UtcNow;

        // Local Function Helper to create a channel for a given language
        // Local functions need to be declared before calling them, so we declared it here.
        void CreateFeed(string lang, string fileName)
        {
            Console.WriteLine($"Processing file: {fileName}");

            var posts = indexPosts
                .Where(p => p.Lang.Equals(lang, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.Date)
                .ToList();

            if (posts.Count == 0) return;

            var channel = new RssChannel
            {
                Title = "RSS Feed " + site.Name + (lang == "NL" ? " (Nederlands)" : " (English)"),
                Link = site.Domain + "/",
                Image = site.Domain + "/img/rawrealism-header.webp",
                Description = lang == "NL" ? "Laatste Nederlandstalige posts van Raw Realism" : "Latest English posts from Raw Realism",
                Language = lang == "NL" ? "nl-NL" : "en-US", // Use hyphen for RSS feeds, not underscore (og property uses underscore)
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
                    EnclosureUrl = !string.IsNullOrEmpty(p.GraphicRelativeUrl) ? site.Domain + p.GraphicRelativeUrl : "",
                    EnclosureType = !string.IsNullOrEmpty(p.GraphicRelativeUrl) ? "image/webp" : "",
                    PubDate = p.Date,
                    Guid = site.Domain + p.RelativeUrl,
                    Author = p.Author
                })]
            };

            // Serialize to XML (simple manual approach)
            var xml = Utils.GenerateRssXml(channel);
            var path = Path.Combine(site.ProjectRoot, "www", fileName);
            File.WriteAllText(path, xml, Encoding.UTF8);
        }

        CreateFeed("EN", "feed-en.xml");
        CreateFeed("NL", "feed-nl.xml");
    }

    private static Meta LoadMetaFromMarkdown(string filePath)
    {
        if (!File.Exists(filePath))
            Utils.ExitError($"ERROR: File '{filePath}' does not exist.");

        var lines = File.ReadAllLines(filePath);

        // Find the first divider line (---)
        int dividerIndex = Array.FindIndex(lines, line => line.Trim().StartsWith("---"));
        if (dividerIndex == -1)
            Utils.ExitError($"ERROR: No '---' divider found in '{filePath}'.");

        // Extract JSON (everything before the divider)
        string json = string.Join(Environment.NewLine, lines.Take(dividerIndex)).Trim();

        // Deserialize JSON to Meta
        Meta? contentMetaData;
        try
        {
            contentMetaData = JsonSerializer.Deserialize<Meta>(json, Utils.CachedSerializerOptions);
        }
        catch (Exception ex)
        {
            Utils.ExitError($"ERROR: Failed to deserialize Meta in '{filePath}': {ex.Message}");
            return null!; // Unreachable, but for compiler safety
        }

        if (contentMetaData == null)
        {
            Utils.ExitError($"ERROR: Meta is null after deserialization in '{filePath}'.");
            return null!; // Unreachable, but for compiler safety
        }

        // Extract Markdown content (everything after the divider)
        contentMetaData.MarkdownContent = string.Join(Environment.NewLine, lines.Skip(dividerIndex + 1)).Trim();
        contentMetaData.Lang = contentMetaData.Locale[..2]; // locale is "nl_NL" or "en_US", so we take the first 2 characters for the language code, e.g., "nl" or "en".

        return contentMetaData;
    }
}
