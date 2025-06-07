namespace RawRealism;

using Microsoft.Extensions.FileProviders;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        WebApplication app = builder.Build();

        // Middleware: Rewrite "/foo" to "/foo.html" if "/foo" does not exist but "/foo.html" does
        // Tip:
        // You can further customize the middleware to handle directories, index files, or other extensions as needed.
        // This approach keeps your local dev experience close to your production static hosting.

        app.Use(async (context, next) =>
        {
            IWebHostEnvironment env = app.Environment;
            string wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "www");
            string path = context.Request.Path.Value ?? "";

            // Only rewrite if not already ending with .html, .css, .js, etc.
            if (!Path.HasExtension(path))
            {
                string htmlPath = Path.Combine(wwwRoot, path.TrimStart('/')) + ".html";
                if (File.Exists(htmlPath))
                    context.Request.Path = path + ".html";

            }
            await next();
        });

        // Serve static files from the "www" directory
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "www")),
            RequestPath = ""
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "www")),
            RequestPath = ""
        });

        app.Run();
    }
}
