using Microsoft.EntityFrameworkCore;
using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;

namespace PdfSigning.Web.Services.Documents;

public sealed class DocumentFieldService : IDocumentFieldService
{
    private readonly ApplicationDbContext _db;
    private readonly IClock _clock;

    public DocumentFieldService(ApplicationDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SignatureFieldDto?> AddSignatureFieldAsync(Guid documentId, string ownerUserId, AddSignatureFieldRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Label);
        if (request.PageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PageNumber), "Page number must be at least 1.");
        }

        var documentExists = await _db.Documents.AnyAsync(x => x.Id == documentId && x.OwnerUserId == ownerUserId, cancellationToken);
        if (!documentExists)
        {
            return null;
        }

        var entity = new SignatureField
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Label = request.Label.Trim(),
            PageNumber = request.PageNumber,
            X = request.X,
            Y = request.Y,
            Width = request.Width,
            Height = request.Height,
            IsRequired = request.IsRequired,
            CreatedAtUtc = _clock.UtcNow,
        };

        _db.SignatureFields.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new SignatureFieldDto(
            entity.Id,
            entity.Label,
            entity.PageNumber,
            entity.X,
            entity.Y,
            entity.Width,
            entity.Height,
            entity.IsRequired,
            entity.CreatedAtUtc);
    }

    public async Task<bool> DeleteSignatureFieldAsync(Guid documentId, string ownerUserId, Guid signatureFieldId, CancellationToken cancellationToken = default)
    {
        var documentExists = await _db.Documents.AnyAsync(x => x.Id == documentId && x.OwnerUserId == ownerUserId, cancellationToken);
        if (!documentExists)
        {
            return false;
        }

        var field = await _db.SignatureFields
            .SingleOrDefaultAsync(x => x.Id == signatureFieldId && x.DocumentId == documentId, cancellationToken);

        if (field is null)
        {
            return false;
        }

        _db.SignatureFields.Remove(field);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
