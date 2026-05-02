namespace PdfSigning.Web.Services.Documents;

public interface ISignedDocumentArtifactService
{
    Task<SignedDocumentArtifactResult> CreateSignedArtifactAsync(Models.Documents.Document document, Models.Documents.SigningSession session, CancellationToken cancellationToken = default);
}

public sealed record SignedDocumentArtifactResult(string StorageKey);
