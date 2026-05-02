using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using PdfSharpCore.Utils;
using PdfSigning.Web.Models.Documents;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Tests.Services.Documents;

public class SignedDocumentArtifactServiceTests
{
    [Fact]
    public async Task CreateSignedArtifactAsync_creates_a_separate_private_pdf_from_the_original()
    {
        var root = CreateTempDirectory();
        var contentRoot = Path.Combine(root, "content");
        var webRoot = Path.Combine(root, "wwwroot");
        Directory.CreateDirectory(contentRoot);
        Directory.CreateDirectory(webRoot);

        var store = new DocumentFileStore(new FakeEnvironment(contentRoot, webRoot));
        var originalStorageKey = "documents/original-contract.pdf";

        await using (var originalStream = new MemoryStream(CreateSimplePdfBytes()))
        {
            await store.SaveAsync(originalStorageKey, originalStream);
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Contract",
            OriginalFileName = "contract.pdf",
            ContentType = "application/pdf",
            StorageKey = originalStorageKey,
            SignatureFields =
            [
                new SignatureField
                {
                    Id = Guid.NewGuid(),
                    Label = "Signature",
                    PageNumber = 1,
                    X = 72m,
                    Y = 144m,
                    Width = 240m,
                    Height = 72m,
                    IsRequired = true,
                    CreatedAtUtc = new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero)
                }
            ]
        };

        var session = new SigningSession
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Document = document,
            RecipientEmail = "signer@example.com",
            AccessTokenHash = "hash",
            CreatedAtUtc = new DateTimeOffset(2026, 1, 2, 10, 5, 0, TimeSpan.Zero),
            ExpiresAtUtc = new DateTimeOffset(2026, 1, 3, 10, 5, 0, TimeSpan.Zero),
            CompletedAtUtc = new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero),
            SignedByName = "Inspector Example"
        };

        document.SignatureFields.Single().SigningSessionId = session.Id;

        var service = new SignedDocumentArtifactService(store);

        var result = await service.CreateSignedArtifactAsync(document, session);

        Assert.NotEqual(originalStorageKey, result.StorageKey);
        Assert.StartsWith("documents/signed/", result.StorageKey);
        Assert.Equal(originalStorageKey, document.StorageKey);

        await using var originalRead = await store.OpenReadAsync(originalStorageKey);
        await using var signedRead = await store.OpenReadAsync(result.StorageKey);

        Assert.NotNull(originalRead);
        Assert.NotNull(signedRead);
        Assert.True(signedRead!.Length > 0);
        Assert.NotEqual(originalRead!.Length, signedRead.Length);
    }

    private static byte[] CreateSimplePdfBytes()
    {
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new FontResolver();
        }

        using var document = new PdfDocument();
        var page = document.AddPage();
        using var graphics = XGraphics.FromPdfPage(page);
        var font = new XFont("Arial", 18, XFontStyle.Regular);
        graphics.DrawString("Original PDF", font, XBrushes.Black, new XRect(72, 72, 300, 40), XStringFormats.TopLeft);

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "pdfsigning-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public FakeEnvironment(string contentRootPath, string webRootPath)
        {
            ContentRootPath = contentRootPath;
            WebRootPath = webRootPath;
        }

        public string ApplicationName { get; set; } = "PdfSigning.Web.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; }
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; } = Environments.Development;
    }
}
