namespace RawRealism.Parser.Models;

public class Templates
{
    public IDictionary<string, string> StaticTemplates { get; set; }

    public Templates()
    {
        StaticTemplates = new Dictionary<string, string>();
    }

    public void AddTemplate(string fileName, string projectRoot)
    {
        StaticTemplates[fileName] = LoadTemplate(Path.Combine(projectRoot, "content", "templates",  fileName + ".html"));
    }

    public string LoadTemplate(string path) // includes full path with .html extension
    {
        if (!File.Exists(path))
            Utils.ExitError($"Template file: '{path}' not found.");

        return File.ReadAllText(path);
    }

    public void LoadDefaultTemplates(string projectRoot)
    {
        // Add this to appsettings.json? // "Templates": { "ProjectRoot": "/path/to/project/root" } ?
        // Load list, and then for each file in the list, call AddTemplate
        AddTemplate("head", projectRoot); // Parts of pages
        AddTemplate("header", projectRoot);
        AddTemplate("footer", projectRoot);
        AddTemplate("index", projectRoot); // Pages
        AddTemplate("about", projectRoot); // About page EN
        AddTemplate("over", projectRoot); // About page NL
        AddTemplate("404", projectRoot);
    }
}
