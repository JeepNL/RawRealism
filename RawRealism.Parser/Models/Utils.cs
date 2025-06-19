using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

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
            Console.WriteLine($"(Pages) Image encoded and saved: {outputImgFile}");
        }
        catch (Exception ex)
        {
            ExitError($"ERROR: Failed to process image '{inputImgFile}': {ex.Message}");
        }
    }
}
