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
