using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;
using PdfSigning.Web.Pages.Documents;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Tests.Pages.Documents;

public class DetailsModelTests
{
    [Fact]
    public async Task OnPostDeleteFieldAsync_returns_json_for_xhr_requests_and_deletes_the_field()
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
        var model = new DetailsModel(
            new StubDocumentReadService(),
            new DocumentFieldService(verifyDb, new FixedClock(DateTimeOffset.UtcNow)),
            new StubDocumentStatusService(),
            new StubDocumentSigningService());

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-123")],
            authenticationType: "TestAuth"));
        httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        var result = await model.OnPostDeleteFieldAsync(documentId, fieldId);

        var payload = Assert.IsType<JsonResult>(result);
        Assert.Contains("Signature field deleted", payload.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await verifyDb.SignatureFields.ToListAsync());
    }

    [Fact]
    public async Task OnPostCreateSigningLinkAsync_uses_the_recipient_email_and_returns_a_page_result()
    {
        var documentId = Guid.NewGuid();
        var signingService = new StubDocumentSigningService
        {
            CreateResult = new CreateSigningSessionResult(
                Guid.NewGuid(),
                documentId,
                "recipient@example.com",
                DateTimeOffset.UtcNow.AddDays(7),
                "raw-token")
        };

        var model = new DetailsModel(
            new StubDocumentReadService(),
            new StubDocumentFieldService(),
            new StubDocumentStatusService(),
            signingService)
        {
            SigningRecipientEmail = "Recipient@Example.com"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-123")],
            authenticationType: "TestAuth"));

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        var result = await model.OnPostCreateSigningLinkAsync(documentId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Recipient@Example.com", signingService.ObservedRecipientEmail);
        Assert.Equal("recipient@example.com", model.SigningRecipientEmail);
        Assert.Equal("Secure signing link created.", model.StatusMessage);
        Assert.NotNull(model.GeneratedSigningLink);
        Assert.Contains("/Sign/", model.GeneratedSigningLink, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(signingService.CreateResult.ExpiresAtUtc, model.GeneratedSigningLinkExpiresAtUtc);
    }

    private sealed class StubDocumentReadService : IDocumentReadService
    {
        public Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, string ownerUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DocumentDetailsDto?>(new DocumentDetailsDto(
                documentId,
                ownerUserId,
                "Contract",
                "contract.pdf",
                "application/pdf",
                "documents/contract.pdf",
                DocumentStatus.ReadyForSigning,
                DateTimeOffset.UtcNow,
                null,
                []));
        }

        public Task<IReadOnlyList<DocumentSummaryDto>> GetRecentDocumentsAsync(int take, string ownerUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DocumentSummaryDto>>(Array.Empty<DocumentSummaryDto>());
        }
    }

    private sealed class StubDocumentFieldService : IDocumentFieldService
    {
        public Task<SignatureFieldDto?> AddSignatureFieldAsync(Guid documentId, string ownerUserId, AddSignatureFieldRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SignatureFieldDto?>(null);
        }

        public Task<bool> DeleteSignatureFieldAsync(Guid documentId, string ownerUserId, Guid signatureFieldId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class StubDocumentStatusService : IDocumentStatusService
    {
        public Task<bool> MarkReadyForSigningAsync(Guid documentId, string ownerUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class StubDocumentSigningService : IDocumentSigningService
    {
        public string? ObservedRecipientEmail { get; private set; }

        public CreateSigningSessionResult? CreateResult { get; set; }

        public Task<CreateSigningSessionResult?> CreateSigningSessionAsync(Guid documentId, string ownerUserId, CreateSigningSessionRequest request, CancellationToken cancellationToken = default)
        {
            ObservedRecipientEmail = request.RecipientEmail;
            return Task.FromResult(CreateResult);
        }

        public Task<PendingSigningSessionDto?> GetPendingSigningSessionAsync(Guid sessionId, string accessToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PendingSigningSessionDto?>(null);
        }

        public Task<SigningSessionViewDto?> GetSigningSessionAsync(Guid sessionId, string accessToken, string recipientEmail, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SigningSessionViewDto?>(null);
        }

        public Task<SigningCompletionResult?> CompleteSigningAsync(Guid sessionId, string accessToken, CompleteSigningRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SigningCompletionResult?>(null);
        }

        public Task<bool> RevokeSigningSessionAsync(Guid sessionId, string ownerUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
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
