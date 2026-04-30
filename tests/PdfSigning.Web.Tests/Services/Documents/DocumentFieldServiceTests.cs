using Microsoft.EntityFrameworkCore;
using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Tests.Services.Documents;

public class DocumentFieldServiceTests
{
    [Fact]
    public async Task AddSignatureFieldAsync_persists_field_for_existing_document()
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
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 2, 16, 0, 0, TimeSpan.Zero));
        var service = new DocumentFieldService(verifyDb, clock);

        var result = await service.AddSignatureFieldAsync(documentId, new AddSignatureFieldRequest(
            Label: "Signer 1",
            PageNumber: 2,
            X: 111.25m,
            Y: 222.5m,
            Width: 333m,
            Height: 44m,
            IsRequired: true));

        var saved = await verifyDb.SignatureFields.SingleAsync();

        Assert.Equal(result.Id, saved.Id);
        Assert.Equal(documentId, saved.DocumentId);
        Assert.Equal("Signer 1", saved.Label);
        Assert.Equal(2, saved.PageNumber);
        Assert.Equal(111.25m, saved.X);
        Assert.Equal(222.5m, saved.Y);
        Assert.Equal(333m, saved.Width);
        Assert.Equal(44m, saved.Height);
        Assert.True(saved.IsRequired);
        Assert.Equal(clock.UtcNow, saved.CreatedAtUtc);
    }

    [Fact]
    public async Task AddSignatureFieldAsync_rejects_missing_document()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        var service = new DocumentFieldService(db, new FixedClock(DateTimeOffset.UtcNow));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddSignatureFieldAsync(Guid.NewGuid(), new AddSignatureFieldRequest("Signer 1", 1, 1m, 1m, 1m, 1m)));

        Assert.Contains("Document", exception.Message);
    }

    [Fact]
    public async Task DeleteSignatureFieldAsync_removes_field_from_document()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var documentId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();

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
                        Id = fieldId,
                        Label = "Signer 1",
                        PageNumber = 1,
                        X = 10m,
                        Y = 20m,
                        Width = 100m,
                        Height = 30m,
                        IsRequired = true,
                        CreatedAtUtc = new DateTimeOffset(2026, 1, 2, 15, 35, 0, TimeSpan.Zero),
                    }
                ]
            });
            await db.SaveChangesAsync();
        }

        await using var verifyDb = new ApplicationDbContext(options);
        var service = new DocumentFieldService(verifyDb, new FixedClock(DateTimeOffset.UtcNow));

        await service.DeleteSignatureFieldAsync(documentId, fieldId);

        Assert.Empty(await verifyDb.SignatureFields.ToListAsync());
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
