namespace PdfSigning.Web.Services.Documents;

public interface IDocumentStatusService
{
    Task MarkReadyForSigningAsync(Guid documentId, CancellationToken cancellationToken = default);
}
