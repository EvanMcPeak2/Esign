using Microsoft.EntityFrameworkCore;
using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;

namespace PdfSigning.Web.Services.Documents;

public sealed class DocumentStatusService : IDocumentStatusService
{
    private readonly ApplicationDbContext _db;

    public DocumentStatusService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task MarkReadyForSigningAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents
            .Include(x => x.SignatureFields)
            .SingleOrDefaultAsync(x => x.Id == documentId, cancellationToken);

        if (document is null)
        {
            throw new InvalidOperationException($"Document '{documentId}' was not found.");
        }

        if (document.SignatureFields.Count == 0)
        {
            throw new InvalidOperationException("Add at least one signature field before marking the document ready for signing.");
        }

        document.Status = DocumentStatus.ReadyForSigning;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
