using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
            new StubDocumentStatusService());

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

    private sealed class StubDocumentReadService : IDocumentReadService
    {
        public Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, string ownerUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DocumentDetailsDto?>(null);
        }

        public Task<IReadOnlyList<DocumentSummaryDto>> GetRecentDocumentsAsync(int take, string ownerUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DocumentSummaryDto>>(Array.Empty<DocumentSummaryDto>());
        }
    }

    private sealed class StubDocumentStatusService : IDocumentStatusService
    {
        public Task<bool> MarkReadyForSigningAsync(Guid documentId, string ownerUserId, CancellationToken cancellationToken = default)
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
