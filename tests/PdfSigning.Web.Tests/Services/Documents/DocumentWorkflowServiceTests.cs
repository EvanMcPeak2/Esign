using Microsoft.EntityFrameworkCore;
using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Tests.Services.Documents;

public class DocumentWorkflowServiceTests
{
    [Fact]
    public async Task CreateDocumentAsync_persists_document_and_signature_fields()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new DocumentWorkflowService(db, clock);

        var request = new CreateDocumentRequest(
            OwnerUserId: "user-123",
            Title: "NDA",
            OriginalFileName: "nda.pdf",
            ContentType: "application/pdf",
            StorageKey: "uploads/nda.pdf",
            SignatureFields:
            [
                new SignatureFieldRequest("Signer 1", 1, 120.5m, 700.25m, 200m, 60m, true),
                new SignatureFieldRequest("Initials", 2, 80m, 100m, 120m, 40m, false),
            ]);

        var result = await service.CreateDocumentAsync(request);

        var saved = await db.Documents.Include(x => x.SignatureFields).SingleAsync();

        Assert.Equal(result.Id, saved.Id);
        Assert.Equal("user-123", saved.OwnerUserId);
        Assert.Equal("NDA", saved.Title);
        Assert.Equal("nda.pdf", saved.OriginalFileName);
        Assert.Equal("application/pdf", saved.ContentType);
        Assert.Equal("uploads/nda.pdf", saved.StorageKey);
        Assert.Equal(DocumentStatus.Draft, saved.Status);
        Assert.Equal(clock.UtcNow, saved.CreatedAtUtc);
        Assert.Equal(2, saved.SignatureFields.Count);
        Assert.Contains(saved.SignatureFields, f =>
            f.Label == "Signer 1" &&
            f.PageNumber == 1 &&
            f.X == 120.5m &&
            f.Y == 700.25m &&
            f.Width == 200m &&
            f.Height == 60m &&
            f.IsRequired);
        Assert.Contains(saved.SignatureFields, f =>
            f.Label == "Initials" &&
            f.PageNumber == 2 &&
            f.X == 80m &&
            f.Y == 100m &&
            f.Width == 120m &&
            f.Height == 40m &&
            !f.IsRequired);
    }

    [Fact]
    public async Task CreateDocumentAsync_rejects_missing_owner_user_id()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        var service = new DocumentWorkflowService(db, new FixedClock(DateTimeOffset.UtcNow));

        var request = new CreateDocumentRequest(
            OwnerUserId: string.Empty,
            Title: "NDA",
            OriginalFileName: "nda.pdf",
            ContentType: "application/pdf",
            StorageKey: "uploads/nda.pdf",
            SignatureFields: [new SignatureFieldRequest("Signer 1", 1, 1m, 1m, 1m, 1m)]);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDocumentAsync(request));

        Assert.Contains(nameof(request.OwnerUserId), exception.Message);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
