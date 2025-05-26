using System;
using System.IO;

namespace RawRealism.Parser;

internal class Program
{
    static void Main(string[] args)
    {
        // Pad naar de www-map van het hoofdproject (relatief vanaf de repo-root)
        var wwwPath = Path.Combine("..", "RawRealism", "www");
        Directory.CreateDirectory(wwwPath);

        // Bestandsnaam: GUID.txt
        var fileName = $"{Guid.NewGuid()}.txt";
        var filePath = Path.Combine(wwwPath, fileName);

        // Inhoud: datum en tijd
        var content = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

        File.WriteAllText(filePath, content);

        Console.WriteLine($"Bestand aangemaakt: {filePath}");
    }
}
