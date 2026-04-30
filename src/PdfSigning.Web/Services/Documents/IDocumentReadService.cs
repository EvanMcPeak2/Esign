namespace PdfSigning.Web.Services.Documents;

public interface IDocumentReadService
{
    Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, string ownerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentSummaryDto>> GetRecentDocumentsAsync(int take, string ownerUserId, CancellationToken cancellationToken = default);
}
