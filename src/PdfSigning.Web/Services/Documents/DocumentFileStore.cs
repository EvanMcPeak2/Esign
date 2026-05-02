using Microsoft.AspNetCore.Hosting;

namespace PdfSigning.Web.Services.Documents;

public sealed class DocumentFileStore : IDocumentFileStore
{
    private const string PrivateRootFolderName = "App_Data";
    private const string PrivateDocumentsFolderName = "documents";
    private const string SignedDocumentsFolderName = "documents/signed";

    private readonly IWebHostEnvironment _environment;

    public DocumentFileStore(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string CreateStorageKey(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            extension = ".pdf";
        }

        return $"{PrivateDocumentsFolderName}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
    }

    public string CreateSignedStorageKey(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            extension = ".pdf";
        }

        return $"{SignedDocumentsFolderName}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
    }

    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentNullException.ThrowIfNull(content);

        var path = ResolvePrivatePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Could not determine the storage directory."));

        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var path = ResolvePrivatePath(storageKey);
        if (File.Exists(path))
        {
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        var legacyPath = ResolveLegacyWebRootPath(storageKey);
        if (File.Exists(legacyPath))
        {
            return File.Open(legacyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        await Task.CompletedTask;
        return null;
    }

    private string ResolvePrivatePath(string storageKey)
    {
        var safeRelativePath = storageKey.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        return Path.Combine(_environment.ContentRootPath, PrivateRootFolderName, safeRelativePath);
    }

    private string ResolveLegacyWebRootPath(string storageKey)
    {
        var safeRelativePath = storageKey.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        return Path.Combine(_environment.WebRootPath, safeRelativePath);
    }
}
