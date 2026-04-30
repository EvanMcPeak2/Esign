namespace PdfSigning.Web.Services.Documents;

public interface IDocumentStatusService
{
    Task<bool> MarkReadyForSigningAsync(Guid documentId, string ownerUserId, CancellationToken cancellationToken = default);
}
