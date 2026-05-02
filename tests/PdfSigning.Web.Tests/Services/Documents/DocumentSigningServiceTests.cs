using Microsoft.EntityFrameworkCore;
using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Tests.Services.Documents;

public class DocumentSigningServiceTests
{
    [Fact]
    public async Task CreateSigningSessionAsync_creates_secure_link_and_assigns_fields()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var documentId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var existingSessionId = Guid.NewGuid();

        await using (var db = new ApplicationDbContext(options))
        {
            db.Documents.Add(new Document
            {
                Id = documentId,
                OwnerUserId = "owner-1",
                Title = "NDA",
                OriginalFileName = "nda.pdf",
                StorageKey = "documents/nda.pdf",
                Status = DocumentStatus.ReadyForSigning,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 3, 9, 0, 0, TimeSpan.Zero),
                SignatureFields =
                [
                    new SignatureField
                    {
                        Id = fieldId,
                        Label = "Signer 1",
                        PageNumber = 1,
                        X = 25m,
                        Y = 100m,
                        Width = 200m,
                        Height = 50m,
                        IsRequired = true,
                        CreatedAtUtc = new DateTimeOffset(2026, 1, 3, 9, 5, 0, TimeSpan.Zero),
                    }
                ],
                SigningSessions =
                [
                    new SigningSession
                    {
                        Id = existingSessionId,
                        RecipientEmail = "old@example.com",
                        AccessTokenHash = "old-token-hash",
                        CreatedAtUtc = new DateTimeOffset(2026, 1, 3, 10, 0, 0, TimeSpan.Zero),
                    }
                ]
            });
            await db.SaveChangesAsync();
        }

        await using var verifyDb = new ApplicationDbContext(options);
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 4, 12, 0, 0, TimeSpan.Zero));
        var service = new DocumentSigningService(verifyDb, clock, new StubSignedDocumentArtifactService());

        var result = await service.CreateSigningSessionAsync(documentId, "owner-1", new CreateSigningSessionRequest(
            RecipientEmail: "signer@example.com"));

        var savedDocument = await verifyDb.Documents
            .Include(x => x.SignatureFields)
            .Include(x => x.SigningSessions)
            .SingleAsync();

        Assert.NotNull(result);
        Assert.Equal(documentId, result!.DocumentId);
        Assert.Equal("signer@example.com", result.RecipientEmail);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal(DocumentSigningService.HashAccessToken(result.AccessToken), savedDocument.SigningSessions.Single(x => x.Id == result.SessionId).AccessTokenHash);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal(DocumentStatus.InSigning, savedDocument.Status);
        Assert.Equal(existingSessionId, savedDocument.SigningSessions.Single(x => x.Id == existingSessionId).Id);
        Assert.NotNull(savedDocument.SigningSessions.Single(x => x.Id == existingSessionId).RevokedAtUtc);
        Assert.Single(savedDocument.SignatureFields);
        Assert.All(savedDocument.SignatureFields, field => Assert.Equal(result.SessionId, field.SigningSessionId));
        Assert.Equal(clock.UtcNow.AddHours(24), savedDocument.SigningSessions.Single(x => x.Id == result.SessionId).ExpiresAtUtc);
    }

    [Fact]
    public async Task GetSigningSessionAsync_rejects_wrong_email_or_token()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var documentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await using (var db = new ApplicationDbContext(options))
        {
            db.Documents.Add(new Document
            {
                Id = documentId,
                OwnerUserId = "owner-1",
                Title = "NDA",
                OriginalFileName = "nda.pdf",
                StorageKey = "documents/nda.pdf",
                Status = DocumentStatus.InSigning,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 3, 9, 0, 0, TimeSpan.Zero),
                SigningSessions =
                [
                    new SigningSession
                    {
                        Id = sessionId,
                        RecipientEmail = "signer@example.com",
                        AccessTokenHash = DocumentSigningService.HashAccessToken("valid-token"),
                        CreatedAtUtc = new DateTimeOffset(2026, 1, 3, 10, 0, 0, TimeSpan.Zero),
                        ExpiresAtUtc = new DateTimeOffset(2026, 1, 10, 10, 0, 0, TimeSpan.Zero),
                    }
                ]
            });
            await db.SaveChangesAsync();
        }

        await using var verifyDb = new ApplicationDbContext(options);
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 4, 12, 0, 0, TimeSpan.Zero));
        var service = new DocumentSigningService(verifyDb, clock, new StubSignedDocumentArtifactService());

        var badToken = await service.GetSigningSessionAsync(sessionId, "wrong-token", "signer@example.com");
        var badEmail = await service.GetSigningSessionAsync(sessionId, "valid-token", "other@example.com");
        var good = await service.GetSigningSessionAsync(sessionId, "valid-token", "signer@example.com");

        Assert.Null(badToken);
        Assert.Null(badEmail);
        Assert.NotNull(good);
        Assert.Equal(documentId, good!.DocumentId);
        Assert.Equal("NDA", good.DocumentTitle);
        Assert.Equal("signer@example.com", good.RecipientEmail);
    }

    [Fact]
    public async Task CompleteSigningAsync_marks_document_signed_for_matching_email_and_token()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var documentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await using (var db = new ApplicationDbContext(options))
        {
            db.Documents.Add(new Document
            {
                Id = documentId,
                OwnerUserId = "owner-1",
                Title = "NDA",
                OriginalFileName = "nda.pdf",
                StorageKey = "documents/nda.pdf",
                Status = DocumentStatus.InSigning,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 3, 9, 0, 0, TimeSpan.Zero),
                SignatureFields =
                [
                    new SignatureField
                    {
                        Id = Guid.NewGuid(),
                        Label = "Signer 1",
                        PageNumber = 1,
                        X = 25m,
                        Y = 100m,
                        Width = 200m,
                        Height = 50m,
                        IsRequired = true,
                        CreatedAtUtc = new DateTimeOffset(2026, 1, 3, 9, 5, 0, TimeSpan.Zero),
                        SigningSessionId = sessionId,
                    }
                ],
                SigningSessions =
                [
                    new SigningSession
                    {
                        Id = sessionId,
                        RecipientEmail = "signer@example.com",
                        AccessTokenHash = DocumentSigningService.HashAccessToken("valid-token"),
                        CreatedAtUtc = new DateTimeOffset(2026, 1, 3, 10, 0, 0, TimeSpan.Zero),
                        ExpiresAtUtc = new DateTimeOffset(2026, 1, 10, 10, 0, 0, TimeSpan.Zero),
                    }
                ]
            });
            await db.SaveChangesAsync();
        }

        await using var verifyDb = new ApplicationDbContext(options);
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 4, 12, 0, 0, TimeSpan.Zero));
        var artifactService = new StubSignedDocumentArtifactService
        {
            StorageKey = "documents/signed/nda-signed.pdf"
        };
        var service = new DocumentSigningService(verifyDb, clock, artifactService);

        var result = await service.CompleteSigningAsync(sessionId, "valid-token", new CompleteSigningRequest(
            RecipientEmail: "signer@example.com",
            SignedByName: "Jamie Example"));

        var savedDocument = await verifyDb.Documents.Include(x => x.SigningSessions).SingleAsync();
        var savedSession = savedDocument.SigningSessions.Single();

        Assert.NotNull(result);
        Assert.Equal(documentId, result!.DocumentId);
        Assert.Equal(DocumentStatus.Signed, savedDocument.Status);
        Assert.Equal(clock.UtcNow, savedDocument.CompletedAtUtc);
        Assert.Equal(clock.UtcNow, savedSession.CompletedAtUtc);
        Assert.Equal("Jamie Example", savedSession.SignedByName);
        Assert.Equal("signer@example.com", savedSession.RecipientEmail);
        Assert.Equal("documents/signed/nda-signed.pdf", savedDocument.SignedArtifactStorageKey);
        Assert.Equal("documents/nda.pdf", savedDocument.StorageKey);
        Assert.Equal(documentId, artifactService.ObservedDocumentId);
        Assert.Equal(sessionId, artifactService.ObservedSessionId);
        Assert.Equal("Jamie Example", artifactService.ObservedSignedByName);
    }

    [Fact]
    public async Task RevokeSigningSessionAsync_only_allows_owner_to_revoke()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var documentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await using (var db = new ApplicationDbContext(options))
        {
            db.Documents.Add(new Document
            {
                Id = documentId,
                OwnerUserId = "owner-1",
                Title = "NDA",
                OriginalFileName = "nda.pdf",
                StorageKey = "documents/nda.pdf",
                Status = DocumentStatus.InSigning,
                CreatedAtUtc = new DateTimeOffset(2026, 1, 3, 9, 0, 0, TimeSpan.Zero),
                SigningSessions =
                [
                    new SigningSession
                    {
                        Id = sessionId,
                        RecipientEmail = "signer@example.com",
                        AccessTokenHash = DocumentSigningService.HashAccessToken("valid-token"),
                        CreatedAtUtc = new DateTimeOffset(2026, 1, 3, 10, 0, 0, TimeSpan.Zero),
                        ExpiresAtUtc = new DateTimeOffset(2026, 1, 10, 10, 0, 0, TimeSpan.Zero),
                    }
                ]
            });
            await db.SaveChangesAsync();
        }

        await using var verifyDb = new ApplicationDbContext(options);
        var service = new DocumentSigningService(verifyDb, new FixedClock(new DateTimeOffset(2026, 1, 4, 12, 0, 0, TimeSpan.Zero)), new StubSignedDocumentArtifactService());

        var bad = await service.RevokeSigningSessionAsync(sessionId, "other-owner");
        var good = await service.RevokeSigningSessionAsync(sessionId, "owner-1");

        var savedSession = await verifyDb.SigningSessions.SingleAsync();

        Assert.False(bad);
        Assert.True(good);
        Assert.NotNull(savedSession.RevokedAtUtc);
    }

    private sealed class StubSignedDocumentArtifactService : ISignedDocumentArtifactService
    {
        public string StorageKey { get; set; } = "documents/signed/default.pdf";

        public Guid ObservedDocumentId { get; private set; }

        public Guid ObservedSessionId { get; private set; }

        public string? ObservedSignedByName { get; private set; }

        public Task<SignedDocumentArtifactResult> CreateSignedArtifactAsync(Document document, SigningSession session, CancellationToken cancellationToken = default)
        {
            ObservedDocumentId = document.Id;
            ObservedSessionId = session.Id;
            ObservedSignedByName = session.SignedByName;
            return Task.FromResult(new SignedDocumentArtifactResult(StorageKey));
        }
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
