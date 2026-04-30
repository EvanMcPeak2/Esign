using Microsoft.EntityFrameworkCore;
using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Tests.Services.Documents;

public class DocumentReadServiceTests
{
    [Fact]
    public async Task GetDocumentDetailsAsync_returns_document_with_fields()
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
                ContentType = "application/pdf",
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
                        X = 100.25m,
                        Y = 680.5m,
                        Width = 220m,
                        Height = 64m,
                        IsRequired = true,
                        CreatedAtUtc = new DateTimeOffset(2026, 1, 2, 15, 31, 0, TimeSpan.Zero),
                    }
                ]
            });

            await db.SaveChangesAsync();
        }

        await using var verifyDb = new ApplicationDbContext(options);
        var service = new DocumentReadService(verifyDb);

        var result = await service.GetDocumentDetailsAsync(documentId);

        Assert.NotNull(result);
        Assert.Equal(documentId, result!.Id);
        Assert.Equal("user-123", result.OwnerUserId);
        Assert.Equal("Contract", result.Title);
        Assert.Equal("contract.pdf", result.OriginalFileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("uploads/contract.pdf", result.StorageKey);
        Assert.Equal(DocumentStatus.Draft, result.Status);
        Assert.Single(result.SignatureFields);
        var field = result.SignatureFields[0];
        Assert.Equal("Signer 1", field.Label);
        Assert.Equal(1, field.PageNumber);
        Assert.Equal(100.25m, field.X);
        Assert.Equal(680.5m, field.Y);
        Assert.Equal(220m, field.Width);
        Assert.Equal(64m, field.Height);
        Assert.True(field.IsRequired);
    }

    [Fact]
    public async Task GetDocumentDetailsAsync_returns_null_when_document_is_missing()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        var service = new DocumentReadService(db);

        var result = await service.GetDocumentDetailsAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecentDocumentsAsync_returns_newest_documents_first()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var db = new ApplicationDbContext(options))
        {
            db.Documents.AddRange(
                new Document
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = "user-1",
                    Title = "Older",
                    OriginalFileName = "older.pdf",
                    StorageKey = "uploads/older.pdf",
                    Status = DocumentStatus.Draft,
                    CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
                },
                new Document
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = "user-2",
                    Title = "Newer",
                    OriginalFileName = "newer.pdf",
                    StorageKey = "uploads/newer.pdf",
                    Status = DocumentStatus.ReadyForSigning,
                    CreatedAtUtc = new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero),
                });
            await db.SaveChangesAsync();
        }

        await using var verifyDb = new ApplicationDbContext(options);
        var service = new DocumentReadService(verifyDb);

        var results = await service.GetRecentDocumentsAsync(10);

        Assert.Equal(2, results.Count);
        Assert.Equal("Newer", results[0].Title);
        Assert.Equal("Older", results[1].Title);
        Assert.Equal(DocumentStatus.ReadyForSigning, results[0].Status);
    }
}
