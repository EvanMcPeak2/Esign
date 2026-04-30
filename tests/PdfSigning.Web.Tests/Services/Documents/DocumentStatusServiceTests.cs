using Microsoft.EntityFrameworkCore;
using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Tests.Services.Documents;

public class DocumentStatusServiceTests
{
    [Fact]
    public async Task MarkReadyForSigningAsync_updates_status_when_document_has_fields()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var documentId = Guid.NewGuid();

        await using (var db = new ApplicationDbContext(options))
        {
            db.Documents.Add(new Document
            {
                Id = documentId,
                OwnerUserId = "user-123",
                Title = "Contract",
                OriginalFileName = "contract.pdf",
                StorageKey = "uploads/contract.pdf",
                Status = DocumentStatus.Draft,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 2, 15, 30, 0, TimeSpan.Zero),
                SignatureFields =
                [
                    new SignatureField
                    {
                        Id = Guid.NewGuid(),
                        Label = "Signer 1",
                        PageNumber = 1,
                        X = 1m,
                        Y = 1m,
                        Width = 1m,
                        Height = 1m,
                        IsRequired = true,
                        CreatedAtUtc = new DateTimeOffset(2026, 1, 2, 15, 35, 0, TimeSpan.Zero),
                    }
                ]
            });
            await db.SaveChangesAsync();
        }

        await using var verifyDb = new ApplicationDbContext(options);
        var service = new DocumentStatusService(verifyDb);

        await service.MarkReadyForSigningAsync(documentId);

        var saved = await verifyDb.Documents.SingleAsync();

        Assert.Equal(DocumentStatus.ReadyForSigning, saved.Status);
    }

    [Fact]
    public async Task MarkReadyForSigningAsync_rejects_documents_without_fields()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var documentId = Guid.NewGuid();

        await using (var db = new ApplicationDbContext(options))
        {
            db.Documents.Add(new Document
            {
                Id = documentId,
                OwnerUserId = "user-123",
                Title = "Contract",
                OriginalFileName = "contract.pdf",
                StorageKey = "uploads/contract.pdf",
                Status = DocumentStatus.Draft,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 2, 15, 30, 0, TimeSpan.Zero),
            });
            await db.SaveChangesAsync();
        }

        await using var verifyDb = new ApplicationDbContext(options);
        var service = new DocumentStatusService(verifyDb);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkReadyForSigningAsync(documentId));

        Assert.Contains("signature field", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
