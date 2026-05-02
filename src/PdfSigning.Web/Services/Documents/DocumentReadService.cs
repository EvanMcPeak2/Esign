using Microsoft.EntityFrameworkCore;
using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;

namespace PdfSigning.Web.Services.Documents;

public sealed class DocumentReadService : IDocumentReadService
{
    private readonly ApplicationDbContext _db;

    public DocumentReadService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, string ownerUserId, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents
            .AsNoTracking()
            .Include(x => x.SignatureFields)
            .SingleOrDefaultAsync(x => x.Id == documentId && x.OwnerUserId == ownerUserId, cancellationToken);

        if (document is null)
        {
            return null;
        }

        return new DocumentDetailsDto(
            document.Id,
            document.OwnerUserId,
            document.Title,
            document.OriginalFileName,
            document.ContentType,
            document.StorageKey,
            document.SignedArtifactStorageKey,
            document.Status,
            document.CreatedAtUtc,
            document.CompletedAtUtc,
            document.SignatureFields
                .OrderBy(x => x.PageNumber)
                .ThenBy(x => x.CreatedAtUtc)
                .Select(x => new SignatureFieldDto(
                    x.Id,
                    x.Label,
                    x.PageNumber,
                    x.X,
                    x.Y,
                    x.Width,
                    x.Height,
                    x.IsRequired,
                    x.CreatedAtUtc))
                .ToList());
    }

    public async Task<IReadOnlyList<DocumentSummaryDto>> GetRecentDocumentsAsync(int take, string ownerUserId, CancellationToken cancellationToken = default)
    {
        if (take < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(take));
        }

        return await _db.Documents
            .AsNoTracking()
            .Include(x => x.SignatureFields)
            .Where(x => x.OwnerUserId == ownerUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .Select(x => new DocumentSummaryDto(
                x.Id,
                x.Title,
                x.OriginalFileName,
                x.StorageKey,
                x.Status,
                x.CreatedAtUtc,
                x.SignatureFields.Count))
            .ToListAsync(cancellationToken);
    }
}
