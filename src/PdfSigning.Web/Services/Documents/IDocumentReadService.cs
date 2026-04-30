namespace PdfSigning.Web.Services.Documents;

public interface IDocumentReadService
{
    Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, CancellationToken cancellationToken = default);
}
