using Microsoft.AspNetCore.Hosting;
using Project.Application.Files;

namespace Project.Infrastructure.Files;

public class LocalFileStorage : IFileStorage
{
    private readonly string _uploadRoot;
    private readonly string _documentRoot;

    public LocalFileStorage(IWebHostEnvironment env)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _uploadRoot = Path.Combine(webRoot, "uploads", "listings");
        _documentRoot = Path.Combine(webRoot, "uploads", "documents");
        Directory.CreateDirectory(_uploadRoot);
        Directory.CreateDirectory(_documentRoot);
    }

    public async Task<string> SaveListingPhotoAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
        {
            throw new InvalidOperationException("Дозволені формати: JPG, PNG, WebP.");
        }

        var stored = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(_uploadRoot, stored);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
        return $"/uploads/listings/{stored}";
    }

    public async Task<string> SaveHostDocumentAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp" and not ".pdf")
        {
            throw new InvalidOperationException("Дозволені формати документа: JPG, PNG, WebP, PDF.");
        }

        var stored = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(_documentRoot, stored);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
        return $"/uploads/documents/{stored}";
    }

    public void TryDeleteLocal(string url)
    {
        if (!url.StartsWith("/uploads/listings/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var name = Path.GetFileName(url);
        var path = Path.Combine(_uploadRoot, name);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
