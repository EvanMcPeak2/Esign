namespace PdfSigning.Web.Services.Documents;

public interface IDocumentReadService
{
    Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentSummaryDto>> GetRecentDocumentsAsync(int take, CancellationToken cancellationToken = default);
}
