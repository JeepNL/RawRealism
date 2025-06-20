using Markdig;
using Microsoft.Extensions.Configuration;
using RawRealism.Parser.Models;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RawRealism.Parser;

internal class Program
{
    static void Main(string[] args)
    {
        ///
        /// TODO
        ///
        /// - Raw Realism met een spatie in de naam, dus niet RawRealism, maar Raw Realism. Checken.
        ///
        /// - Tags voor default pages.
        ///
        /// - Create static templates with a <head></head> template for several pages, like index.html, about.html, etc.
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
            Utils.ExitError("ERROR: Failed to load 'appsettigs.*.json'.");
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
            Utils.ExitError($"ERROR: Directory [site.ProjectRoot]: '{site.ProjectRoot}' doesn't exist.");

        ///
        /// Step 2
        /// Load the markdown file `post.md` from /RawRealism/content/
        /// For every new blogpost I use the same file `post.md` (for now) to write the new content.
        /// I'm doing this because I want to figure out the title (and slug) when writing my blogpost.
        /// So then I have my title and slug when the parser runs and it will create a new file in the:
        /// '/RawRealism/content/posts/[language]/[year]/[month]' directory, with the day (with leading zero if necessary) as the filename as the first part of the filename.
        /// The first, top part of the markdown file has the properties of the blogpost, like title, slug, date, etc. in JSON format.
        /// Then there's the divider `---` and then the mixed Markdown/HTML content of the blogpost.
        /// I use HTML in the markdown file, so I can use some special HTML tags, not supported by markdown.
        ///

        // Path to the new blog post source, the `content.md` file.

        var contentPathRoot = Path.Combine(site.ProjectRoot, "content");
        var postMd = Path.Combine(contentPathRoot, "post.md");
        if (!File.Exists(postMd))
            Utils.ExitError($"ERROR: File [post.md] '{postMd}' does not exist.");

        // Read the content of the post.md file and parse it to a Meta object
        Meta contentMetaData = LoadMetaFromMarkdown(postMd);
        contentMetaData.Language = contentMetaData.Locale.Length >= 2 ? contentMetaData.Locale[..2] : contentMetaData.Locale;

        string originalImgPath = Path.Combine(contentPathRoot, "post.webp");
        //contentMetaData.Graphic.Alt = contentMetaData.Graphic.Alt;

        ///
        /// Step 3, Image handling
        ///

        if (!File.Exists(originalImgPath))
            Utils.ExitError($"ERROR: Image file '{originalImgPath}' does not exist.");

        // Load the image file and convert it to WebP format, with 75% quality and a width of 800px, and variable height.
        string encodedImgPath = Path.Combine(contentPathRoot, "post_encoded.webp");
        Utils.ConvertImage(originalImgPath, encodedImgPath);

        ///
        /// Step 4
        ///

        /// Copy the content.md file to the target directory, with the slugified title as the filename
        YearMonth yearMonth = Utils.GetYearMonthFromIso8601(contentMetaData.DateIso8601);
        string targetContentDir = Path.Combine(site.ProjectRoot, "content", "posts", contentMetaData.Language, yearMonth.Yyyy, yearMonth.Mm);
        Directory.CreateDirectory(targetContentDir);

        string targetFileName = $"{contentMetaData.Slug}.md";
        string targetFilePath = Path.Combine(targetContentDir, targetFileName);

        // Old
        // Check if the target file already exists, if it does, exit with an error, we should not overwrite existing blog posts!
        // We can always edit the sources of the blog posts, but we should not overwrite existing blog posts.
        //if (File.Exists(targetFilePath))
        //    Utils.ExitError($"ERROR: Target file '{targetFilePath}' already exists.");

        // New, no check, we overwrite the existing file.
        File.Copy(postMd, targetFilePath, overwrite: true);


        // Copy the image file to the target directory too.
        // The image file is copied to the same directory as the content file, so it can be used in the blog post.
        string targetImageName = $"{contentMetaData.Slug}.webp";
        string targetImagePath = Path.Combine(targetContentDir, targetImageName);

        //old
        //if (File.Exists(targetImagePath))
        //    Utils.ExitError($"ERROR: Target image file '{targetImagePath}' already exists.");

        // New, no check, we overwrite the existing file.
        File.Copy(encodedImgPath, targetImagePath, overwrite: true);

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
            contentMetaData.GraphicDirectory = Path.Combine(site.ProjectRoot, "www", "img", contentMetaData.Language!, yearMonth.Yyyy, yearMonth.Mm);

            Directory.CreateDirectory(contentMetaData.PostDirectory);
            Directory.CreateDirectory(contentMetaData.GraphicDirectory);

            // Generate Relative URL
            //contentMetaData.RelativeUrl = $"/{contentMetaData.PostLangDirName}/{yearMonth.Yyyy}/{yearMonth.Mm}/{contentMetaData.Slug}.html";
            contentMetaData.RelativeUrl = $"/{contentMetaData.PostLangDirName}/{yearMonth.Yyyy}/{yearMonth.Mm}/{contentMetaData.Slug}";
            // Generate Relative Image URL
            contentMetaData.Graphic.RelativeUrl = $"/img/{contentMetaData.Language}/{yearMonth.Yyyy}/{yearMonth.Mm}/{contentMetaData.Slug}.webp";

            // In this for/each-loop generate the individual HTML files for the blog posts.
            GenerateBlogPostHtml(contentMetaData, site);

            // This is for the generation of index.html and RSS feeds, list of posts.
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
                ImageRelativeUrl = contentMetaData.Graphic.RelativeUrl,
                ImageAlt = contentMetaData.Graphic.Alt,
                Tags = contentMetaData.Tags,
                Category = contentMetaData.Category,
                Intro = contentMetaData.Intro
            });
        }

        // TODO
        // for each default pages, like index.html, about.html, etc. generate the HTML files.
        // The default pages are in `var contentPathRoot = Path.Combine(site.ProjectRoot, "default");`
        // There are 2 HTML pages with 'special needs': index.html and 404.html. They need to be saved in the www root directory.
        // The other pages are saved in the /pages/[language]/ directory, e.g., /pages/en/about.html and /pages/nl/over.html.
        // maybe should'mt do a foreach loop for these.
        // There's another thing about the index.html, we need to replace the {{ content Articles }} placeholder with the articles HTML.

        // let's start with a foreach loop for the default pages located in `var contentPathRoot = Path.Combine(site.ProjectRoot, "default");`

        ///
        /// Step 6
        /// Default pages generation
        ///

        var defaultMdPath = Path.Combine(site.ProjectRoot, "content", "default");
        if (!Directory.Exists(defaultMdPath))
            Utils.ExitError($"ERROR: Directory [default pages] '{defaultMdPath}' does not exist.");

        foreach (string defaultMdFile in Directory.EnumerateFiles(defaultMdPath, "*.md"))
        {
            string defaultPageContent = File.ReadAllText(defaultMdFile);

            contentMetaData = LoadMetaFromMarkdown(defaultMdFile); // Load the meta data from the default page markdown file.
            contentMetaData.Language = contentMetaData.Locale.Length >= 2 ? contentMetaData.Locale[..2] : contentMetaData.Locale;
            //contentMetaData.RelativeUrl = $"/{contentMetaData.PostLangDirName}/{yearMonth.Yyyy}/{yearMonth.Mm}/{contentMetaData.Slug}";
            //contentMetaData.Graphic.RelativeUrl = $"/img/{contentMetaData.Language}/{yearMonth.Yyyy}/{yearMonth.Mm}/{contentMetaData.Slug}.webp";

            if (contentMetaData == null)
            {
                Console.WriteLine($"ERROR: Skipping file '{defaultMdFile}' due to null contentMetaData.");
                continue;
            }

            if (contentMetaData.Slug == "index") // Special case for index.html, replace the content Articles placeholder with the articles HTML.
                GenerateDefaultPages(site, contentMetaData, indexPosts);
            else
                GenerateDefaultPages(site, contentMetaData);
        }

        GenerateRss(indexPosts, site);
    }

    private static void GenerateDefaultPages(Site site, Meta contentMetaData, List<IndexPost>? indexPosts = null) // TODO, refactor YearToday.
    {
        // The markdown content is already loaded in the contentMetaData.MarkdownContent property.
        if (string.IsNullOrEmpty(contentMetaData.MarkdownContent))
            Utils.ExitError($"ERROR: The markdown content for '{contentMetaData.Slug}' is empty or null.");

        // Init
        string defaultTemplatePath = Path.Combine(site.ProjectRoot, "content", "templates", "page.html");
        string defaultTemplateContent = File.ReadAllText(defaultTemplatePath);
        string pageHeaderFileName = (contentMetaData.Slug == "index") ? "page_header_index.html" : "page_header.html";

        // Read the templates for head, header, and footer.
        string headContent = File.ReadAllText(Path.Combine(site.ProjectRoot, "content", "templates", "head.html"));
        string siteHeaderContent = File.ReadAllText(Path.Combine(site.ProjectRoot, "content", "templates", "site_header.html"));
        string pageHeader = File.ReadAllText(Path.Combine(site.ProjectRoot, "content", "templates", pageHeaderFileName));
        string footerContent = File.ReadAllText(Path.Combine(site.ProjectRoot, "content", "templates", "footer.html"));

        // Replace the {{ meta Graphic.Url }} in the PageHeader (HTML Page, not a <head></head> property) with the RELATIVE URL of the graphic.
        contentMetaData.Graphic.RelativeUrl = $"/img/{contentMetaData.Slug}.webp";
        pageHeader = pageHeader.Replace("{{ meta Graphic.Url }}", contentMetaData.Graphic.RelativeUrl);

        // Convert Markdown to HTML, Note: MarkDig places <p> tags around the content, if there are no HTML tags in the Markdown content.
        // In the index.md, there's now only `{{ content Articles }}` so MarkDig will places <p> tags around it.
        string markdownHtmlContent = Markdown.ToHtml(contentMetaData.MarkdownContent!);

        defaultTemplateContent = defaultTemplateContent
            .Replace("{{ include head.html }}", headContent)
            .Replace("{{ include site_header.html }}", siteHeaderContent)
            .Replace("{{ var PageHeader }}", pageHeader)
            .Replace("{{ meta MarkdownContent }}", markdownHtmlContent)
            .Replace("{{ include footer.html }}", footerContent);

        // Generate the relative URL for the page.
        if (contentMetaData.Slug == "index" || contentMetaData.Slug == "404")
            contentMetaData.RelativeUrl = $"/{contentMetaData.Slug}";
        else
        {
            // Create the directory for the page if it doesn't exist.
            string pageDir = Path.Combine(site.ProjectRoot, "www", "pages", contentMetaData.Language);
            Directory.CreateDirectory(pageDir);
            contentMetaData.RelativeUrl = $"/pages/{contentMetaData.Language}/{contentMetaData.Slug}";
        }

        /// Image handling
        string origDefImgPath = Path.Combine(site.ProjectRoot, "content", "default", $"{contentMetaData.Slug}.webp");
        string encDefImgPath = Path.Combine(site.ProjectRoot, "www", "img", $"{contentMetaData.Slug}.webp");

        if (!File.Exists(origDefImgPath))
            Utils.ExitError($"ERROR: (Source) Image file '{origDefImgPath}' does not exist.");

        Utils.ConvertImage(origDefImgPath, encDefImgPath);

        // Generate the graphic URL, this is the relative URL to the image file.
        contentMetaData.Graphic.RelativeUrl = $"/img/{contentMetaData.Slug}.webp";
        string targetImgDir = Path.Combine(site.ProjectRoot, "www", "img");

        // footer
        string textAbout = (contentMetaData.Language == "nl") ? "Over" : "About";
        string textContact = "Contact";
        string textPrivacy = "Privacy";
        string TextRights = (contentMetaData.Language == "nl") ? "Alle rechten voorbehouden" : "All rights reserved";
        string pagesAbout = $"{textAbout.ToLower()}"; // no .html extension, we use the /pages/ directory for static pages.
        string pagesAboutPath = $"/pages/{contentMetaData.Language}/{pagesAbout}";
        string pagesPrivacy = $"{textPrivacy.ToLower()}"; // no .html extension, we use the /pages/ directory for static pages.
        string pagesPrivacyPath = $"/pages/{contentMetaData.Language}/{pagesPrivacy}";
        string pagesContact = $"{textContact.ToLower()}"; // no .html extension, we use the /pages/ directory for static pages.
        string pagesContactPath = $"/pages/{contentMetaData.Language}/{pagesContact}";

        string postDate = DateTimeOffset // To display on the blog post page.
            .Parse(contentMetaData.DateIso8601).UtcDateTime
            .ToString(contentMetaData.Locale == "nl_NL" ? "d MMMM yyyy" : "MMMM d, yyyy",
            System.Globalization.CultureInfo.GetCultureInfo(contentMetaData.Locale));

        string postDateTime = DateTimeOffset // For the HTML <time> tag.
            .Parse(contentMetaData.DateIso8601).UtcDateTime
            .ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);

        string by = (contentMetaData.Locale == "nl_NL" ? "door" : "by");

        // Now we have the full defaultTemplateContent with the head, header and footer included.
        // We need to replace all of the placeholders with the actual content.
        defaultTemplateContent = defaultTemplateContent
            .Replace("{{ meta Language }}", contentMetaData.Language)
            .Replace("{{ meta PostTitle }}", contentMetaData.PostTitle)
            .Replace("{{ config Site.Name }}", site.Name)
            .Replace("{{ meta CanonicalUrl }}", site.Domain + contentMetaData.RelativeUrl)
            .Replace("{{ meta Description }}", contentMetaData.Description)
            .Replace("{{ meta Author.Name }}", contentMetaData.Author.Name)
            .Replace("{{ config Site.Generator }}", site.Generator)
            .Replace("{{ meta Graphic.Url }}", site.Domain + contentMetaData.Graphic.RelativeUrl)
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
            .Replace("{{ meta PostDate }}", postDate)
            .Replace("{{ meta PostDateTime }}", postDateTime)
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

        // Index and 404 pages have special handling, and they need to save in the www root directory.
        string htmlFilePath = string.Empty;
        if (contentMetaData.Slug == "index")
        {
            string articlesHtml = string.Join("\n", indexPosts!
                .OrderByDescending(s => s.Date)
                .Select(s =>
                    $"<article><time>{s.DisplayDateLang}</time><h2><a href=\"{s.RelativeUrl}\">{s.Title}</a></h2><h3>{s.SubTitle}</h3>{s.Description}</article><hr>"));

            // Remove the last <hr> tag, so we don't have a double <hr> tag with the footer.
            if (articlesHtml.EndsWith("<hr>"))
                articlesHtml = articlesHtml[..^"<hr>".Length];

            // TODO: Refactor, with JavaScript (browser language detection) we can select the correct language for the index.html page.
            defaultTemplateContent = defaultTemplateContent
                .Replace("{{ content Articles }}", articlesHtml);

            htmlFilePath = Path.Combine(site.ProjectRoot, "www", "index.html");
            File.WriteAllText(htmlFilePath, defaultTemplateContent, System.Text.Encoding.UTF8);
        }
        else if (contentMetaData.Slug == "404")
        {
            htmlFilePath = Path.Combine(site.ProjectRoot, "www", "404.html");
            File.WriteAllText(htmlFilePath, defaultTemplateContent, System.Text.Encoding.UTF8);
        }
        else // For other pages, we save them in the /pages/[language]/ directory.
        {
            htmlFilePath = Path.Combine(site.ProjectRoot, "www", "pages", contentMetaData.Language, $"{contentMetaData.Slug}.html");
            File.WriteAllText(htmlFilePath, defaultTemplateContent, System.Text.Encoding.UTF8);
        }

        Console.WriteLine($"(Pages) HTML file created: {htmlFilePath}");
    }

    private static void GenerateBlogPostHtml(Meta contentMetaData, Site site)
    {
        // Prepare values
        YearMonth yearMonth = Utils.GetYearMonthFromIso8601(contentMetaData.DateIso8601);

        // Load 'post.html' template from the templates directory.
        string postTemplate = Path.Combine(site.ProjectRoot, "content", "templates", "post.html");
        if (!File.Exists(postTemplate))
            Utils.ExitError($"ERROR: File [post.html] '{postTemplate}' does not exist.");

        string postDate = DateTimeOffset // To display on the blog post page.
            .Parse(contentMetaData.DateIso8601).UtcDateTime
            .ToString(contentMetaData.Locale == "nl_NL" ? "d MMMM yyyy" : "MMMM d, yyyy",
            System.Globalization.CultureInfo.GetCultureInfo(contentMetaData.Locale));

        string postDateTime = DateTimeOffset // For the HTML <time> tag.
            .Parse(contentMetaData.DateIso8601).UtcDateTime
            .ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);

        string by = (contentMetaData.Locale == "nl_NL" ? "door" : "by");

        string postHtml = File.ReadAllText(postTemplate);
        postHtml = postHtml
            .Replace("{{ meta Language }}", contentMetaData.Language)
            .Replace("{{ meta PostTitle }}", contentMetaData.PostTitle)
            .Replace("{{ meta SubTitle }}", contentMetaData.SubTitle)
            .Replace("{{ meta Intro }}", contentMetaData.Intro)
            .Replace("{{ meta Graphic.Alt }}", contentMetaData.Graphic.Alt)
            .Replace("{{ meta Graphic.Url }}", contentMetaData.Graphic.RelativeUrl)
            .Replace("{{ meta PostDate }}", postDate)
            .Replace("{{ meta PostDateTime }}", postDateTime) // For the HTML <time> tag, e.g., <time datetime="2023-10-01T12:00:00Z">1 oktober 2023</time>
            .Replace("{{ var ByAuthor }}", $", {by} {contentMetaData.Author.Name}");

        // TODO Check if meta properties are null, if so, don't use the meta property in the HTML, but remove it from the layoutHtml string.
        // Do the same as with the Tag placeholder for these properties, generate the HTML here. Not in the layout.html file.

        // Load the '<head></head>' template from the templates directory.
        string headTemplate = Path.Combine(site.ProjectRoot, "content", "templates", "head.html");
        if (!File.Exists(headTemplate))
            Utils.ExitError($"ERROR: File [head.html] '{headTemplate}' does not exist.");

        string headHtml = File.ReadAllText(headTemplate);
        headHtml = headHtml
            .Replace("{{ meta PostTitle }}", contentMetaData.PostTitle)
            .Replace("{{ meta Graphic.Url }}", site.Domain + contentMetaData.Graphic.RelativeUrl)
            .Replace("{{ config Site.Name }}", site.Name)
            .Replace("{{ meta Description }}", contentMetaData.Description)
            .Replace("{{ meta DateIso8601 }}", contentMetaData.DateIso8601)
            .Replace("{{ meta Category }}", contentMetaData.Category)
            .Replace("{{ meta Locale }}", contentMetaData.Locale)
            .Replace("{{ meta Author.Name }}", contentMetaData.Author.Name)
            .Replace("{{ config Site.Generator }}", site.Generator)
            .Replace("{{ meta CanonicalUrl }}", site.Domain + contentMetaData.RelativeUrl)
            .Replace("{{ meta OGType }}", contentMetaData.OGType);


        // Tags (in head.html)
        if (contentMetaData.Tags != null && contentMetaData.Tags.Length > 0)
        {
            string tagsHtml = string.Join(Environment.NewLine, contentMetaData.Tags.Select(tag => $"<meta property=\"article:tag\" content=\"{tag}\" />"));
            headHtml = headHtml.Replace("{{ array Tags }}", tagsHtml);
        }
        else // If there are no tags, remove the placeholder from the postHtml string.
        {
            headHtml = headHtml.Replace("{{ array Tags }}", string.Empty);
        }

        // Load the header template {{ include site_header.html }}
        string headerTemplate = Path.Combine(site.ProjectRoot, "content", "templates", "site_header.html");
        if (!File.Exists(headerTemplate))
            Utils.ExitError($"ERROR: File [site_header.html] '{headerTemplate}' does not exist.");
        string headerHtml = File.ReadAllText(headerTemplate);

        // Convert Markdown to HTML
        string postMd2HtmlContent = Markdown.ToHtml(contentMetaData.MarkdownContent!);

        // Load the footer template
        string footerTemplate = Path.Combine(site.ProjectRoot, "content", "templates", "footer.html");
        if (!File.Exists(postTemplate))
            Utils.ExitError($"ERROR: File [footer.html] '{footerTemplate}' does not exist.");

        string footerHtml = File.ReadAllText(footerTemplate);
        string TextRights = (contentMetaData.Language == "nl") ? "Alle rechten voorbehouden" : "All rights reserved";
        string textAbout = (contentMetaData.Language == "nl") ? "Over" : "About";
        string textContact = "Contact";
        string pagesAbout = $"{textAbout.ToLower()}"; // no .html extension, we use the /pages/ directory for static pages.
        string pagesAboutPath = $"/pages/{contentMetaData.Language}/{pagesAbout}";
        string textPrivacy = "Privacy";
        string pagesPrivacy = $"{textPrivacy.ToLower()}"; // no .html extension, we use the /pages/ directory for static pages.
        string pagesPrivacyPath = $"/pages/{contentMetaData.Language}/{pagesPrivacy}";
        string pagesContact = $"{textContact.ToLower()}"; // no .html extension, we use the /pages/ directory for static pages.
        string pagesContactPath = $"/pages/{contentMetaData.Language}/{pagesContact}";

        footerHtml = footerHtml
            .Replace("{{ config Site.Name }}", site.Name)
            .Replace("{{ var YearToday }}", DateTime.Now.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture))
            .Replace("{{ var TextRights }}", TextRights)
            .Replace("{{ link About }}", pagesAboutPath)
            .Replace("{{ var TextAbout }}", textAbout)
            .Replace("{{ link Privacy }}", pagesPrivacyPath)
            .Replace("{{ var TextPrivacy }}", textPrivacy)
            .Replace("{{ link Contact }}", pagesContactPath)
            .Replace("{{ var TextContact }}", textContact);

        // Copy the image file to the img directory
        string imageFileName = $"{contentMetaData.Slug}.webp";
        string imageSourcePath = Path.Combine(contentMetaData.ContentPath!, imageFileName);
        string imageDestPath = Path.Combine(site.ProjectRoot, "www", "img", contentMetaData.Language!, yearMonth.Yyyy, yearMonth.Mm, imageFileName);
        File.Copy(imageSourcePath, imageDestPath, overwrite: true);
        //Console.WriteLine($"Image file copied: {imageDestPath}");

        postHtml = postHtml
            .Replace("{{ include head.html }}", headHtml)
            .Replace("{{ include site_header.html }}", headerHtml)
            .Replace("{{ meta MarkdownContent }}", postMd2HtmlContent)
            .Replace("{{ include footer.html }}", footerHtml);

        // Save the generated HTML to the appropriate directory
        string htmlFileName = $"{contentMetaData.Slug}.html";
        string htmlFilePath = Path.Combine(site.ProjectRoot, "www", contentMetaData.PostLangDirName!, yearMonth.Yyyy, yearMonth.Mm, htmlFileName);
        File.WriteAllText(htmlFilePath, postHtml, System.Text.Encoding.UTF8);
        Console.WriteLine($"(Posts) HTML file created: {htmlFilePath}");
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

        // For debugging: print some meta info
        //Console.WriteLine($"Meta loaded: {contentMetaData.PostTitle} ({contentMetaData.Locale}) - {contentMetaData.DateIso8601}");
        //Console.WriteLine($"Markdown content length: {contentMetaData.MarkdownContent.Length}");

        return contentMetaData;
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
                    EnclosureUrl = !string.IsNullOrEmpty(p.ImageRelativeUrl) ? site.Domain + p.ImageRelativeUrl : "",
                    EnclosureType = !string.IsNullOrEmpty(p.ImageRelativeUrl) ? "image/webp" : "",
                    PubDate = p.Date,
                    Guid = site.Domain + p.RelativeUrl,
                    Author = p.Author
                })]
            };

            // Serialize to XML (simple manual approach)
            var xml = Utils.GenerateRssXml(channel);
            var path = Path.Combine(site.ProjectRoot, "www", fileName);
            File.WriteAllText(path, xml, System.Text.Encoding.UTF8);
            Console.WriteLine($"RSS feed generated: {path}");
        }

        CreateFeed("EN", "feed-en.xml");
        CreateFeed("NL", "feed-nl.xml");
    }
}
