using PdfSigning.Web.Models.Documents;

namespace PdfSigning.Web.Services.Documents;

public interface IDocumentWorkflowService
{
    Task<Document> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default);
}
