namespace RawRealism.Parser.Models;

public class Site
{
    public required string Environment { get; set; }
    public required string Name { get; set; }
    public required string Domain { get; set; }
    public required string Header { get; set; }
    public required string ProjectRoot { get; set; }
    public required string GitHubParserRoot { get; set; }
    public required string Generator { get; set; }
}

public class  Template
{
    public required string Name { get; set; }
    public required string Content { get; set; }
}

// For directory structure and URL generation, we need to extract the year and month from the date.
public class YearMonth
{
    public required string Yyyy { get; set; }
    public required string Mm { get; set; }
}
