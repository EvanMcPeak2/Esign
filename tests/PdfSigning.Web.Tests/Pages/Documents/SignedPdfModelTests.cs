using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSigning.Web.Pages.Documents;
using PdfSigning.Web.Services.Documents;
using PdfSigning.Web.Models.Documents;

namespace PdfSigning.Web.Tests.Pages.Documents;

public class SignedPdfModelTests
{
    [Fact]
    public async Task OnGetAsync_returns_signed_artifact_for_the_owner()
    {
        var documentId = Guid.NewGuid();
        var model = new SignedPdfModel(
            new StubDocumentReadService(documentId, "owner-1", "documents/original.pdf", "documents/signed/final.pdf"),
            new StubDocumentFileStore());

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "owner-1")],
            authenticationType: "TestAuth"));

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        var result = await model.OnGetAsync(documentId);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("contract-signed.pdf", file.FileDownloadName);
    }

    private sealed class StubDocumentReadService : IDocumentReadService
    {
        private readonly Guid _documentId;
        private readonly string _ownerUserId;
        private readonly string _storageKey;
        private readonly string? _signedStorageKey;

        public StubDocumentReadService(Guid documentId, string ownerUserId, string storageKey, string? signedStorageKey)
        {
            _documentId = documentId;
            _ownerUserId = ownerUserId;
            _storageKey = storageKey;
            _signedStorageKey = signedStorageKey;
        }

        public Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, string ownerUserId, CancellationToken cancellationToken = default)
        {
            if (documentId != _documentId || ownerUserId != _ownerUserId)
            {
                return Task.FromResult<DocumentDetailsDto?>(null);
            }

            return Task.FromResult<DocumentDetailsDto?>(new DocumentDetailsDto(
                _documentId,
                _ownerUserId,
                "Contract",
                "contract.pdf",
                "application/pdf",
                _storageKey,
                _signedStorageKey,
                DocumentStatus.Signed,
                new DateTimeOffset(2026, 1, 2, 15, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 2, 16, 30, 0, TimeSpan.Zero),
                []));
        }

        public Task<IReadOnlyList<DocumentSummaryDto>> GetRecentDocumentsAsync(int take, string ownerUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DocumentSummaryDto>>(Array.Empty<DocumentSummaryDto>());
        }
    }

    private sealed class StubDocumentFileStore : IDocumentFileStore
    {
        public string CreateStorageKey(string originalFileName) => throw new NotSupportedException();

        public string CreateSignedStorageKey(string originalFileName) => throw new NotSupportedException();

        public Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream?>(new MemoryStream("signed pdf bytes"u8.ToArray()));
        }
    }
}
