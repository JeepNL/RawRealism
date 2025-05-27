namespace RawRealism.Parser;

internal class Program
{
    static void Main(string[] args)
    {
        // Pad naar de www-map van het hoofdproject (relatief vanaf de repo-root)
        var wwwPath = Path.Combine("RawRealism", "www");

        if (!Directory.Exists(wwwPath))
        {
            Console.Error.WriteLine($"ERROR: Directory '{wwwPath}' bestaat niet.");
            Environment.Exit(1);
        }

        // Bestandsnaam: GUID.txt
        var fileName = $"{Guid.NewGuid()}.txt";
        var filePath = Path.Combine(wwwPath, fileName);

        // Inhoud: datum en tijd
        var content = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

        File.WriteAllText(filePath, content);

        Console.WriteLine($"Bestand aangemaakt: {Path.GetFullPath(filePath)}");

        // Toon de inhoud van de www-directory (vergelijkbaar met 'ls -la')
        Console.WriteLine($"\nInhoud van {wwwPath}:");
        foreach (FileInfo? info in from string file in Directory.GetFiles(wwwPath)
                             let info = new FileInfo(file)
                             select info)
        {
            Console.WriteLine($"{info.Name}\t{info.Length} bytes\t{info.LastWriteTimeUtc:yyyy-MM-dd HH:mm:ss} UTC");
        }
    }
}
