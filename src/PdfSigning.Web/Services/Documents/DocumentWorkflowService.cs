using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;

namespace PdfSigning.Web.Services.Documents;

public sealed class DocumentWorkflowService : IDocumentWorkflowService
{
    private readonly ApplicationDbContext _db;
    private readonly IClock _clock;

    public DocumentWorkflowService(ApplicationDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Document> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OwnerUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OriginalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.StorageKey);
        ArgumentNullException.ThrowIfNull(request.SignatureFields);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            OwnerUserId = request.OwnerUserId,
            Title = request.Title,
            OriginalFileName = request.OriginalFileName,
            ContentType = request.ContentType,
            StorageKey = request.StorageKey,
            Status = DocumentStatus.Draft,
            CreatedAtUtc = _clock.UtcNow,
            SignatureFields = request.SignatureFields.Select(field => new SignatureField
            {
                Id = Guid.NewGuid(),
                Label = field.Label,
                PageNumber = field.PageNumber,
                X = field.X,
                Y = field.Y,
                Width = field.Width,
                Height = field.Height,
                IsRequired = field.IsRequired,
                CreatedAtUtc = _clock.UtcNow,
            }).ToList(),
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        return document;
    }
}
