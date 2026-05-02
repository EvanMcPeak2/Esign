namespace PdfSigning.Web.Services.Documents;

public interface IDocumentFileStore
{
    string CreateStorageKey(string originalFileName);

    string CreateSignedStorageKey(string originalFileName);

    Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
