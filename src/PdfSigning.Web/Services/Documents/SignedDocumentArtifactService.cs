using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Utils;
using PdfSigning.Web.Models.Documents;

namespace PdfSigning.Web.Services.Documents;

public sealed class SignedDocumentArtifactService : ISignedDocumentArtifactService
{
    private readonly IDocumentFileStore _documentFileStore;

    public SignedDocumentArtifactService(IDocumentFileStore documentFileStore)
    {
        _documentFileStore = documentFileStore;
    }

    public async Task<SignedDocumentArtifactResult> CreateSignedArtifactAsync(Document document, SigningSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(document.StorageKey))
        {
            throw new InvalidOperationException("The original document file is missing a storage key.");
        }

        if (string.IsNullOrWhiteSpace(document.OriginalFileName))
        {
            throw new InvalidOperationException("The original document file name is required to create a signed artifact.");
        }

        if (string.IsNullOrWhiteSpace(session.SignedByName))
        {
            throw new InvalidOperationException("The signer name is required before creating the signed artifact.");
        }

        var completedAtUtc = session.CompletedAtUtc ?? throw new InvalidOperationException("The signing completion timestamp is required before creating the signed artifact.");
        var signatureFields = document.SignatureFields
            .Where(x => x.SigningSessionId == session.Id)
            .OrderBy(x => x.PageNumber)
            .ThenBy(x => x.CreatedAtUtc)
            .ToList();

        if (signatureFields.Count == 0)
        {
            throw new InvalidOperationException("No signature fields are assigned to this signing session.");
        }

        await using var originalStream = await _documentFileStore.OpenReadAsync(document.StorageKey, cancellationToken)
            ?? throw new InvalidOperationException("The original document file could not be opened.");

        await using var originalCopy = new MemoryStream();
        await originalStream.CopyToAsync(originalCopy, cancellationToken);
        originalCopy.Position = 0;

        EnsureFontResolver();

        using var sourceDocument = PdfReader.Open(originalCopy, PdfDocumentOpenMode.Import);
        using var signedDocument = new PdfSharpCore.Pdf.PdfDocument();

        for (var pageIndex = 0; pageIndex < sourceDocument.PageCount; pageIndex++)
        {
            signedDocument.AddPage(sourceDocument.Pages[pageIndex]);
        }

        foreach (var field in signatureFields)
        {
            if (field.PageNumber < 1 || field.PageNumber > signedDocument.PageCount)
            {
                throw new InvalidOperationException($"Signature field '{field.Label}' points to invalid page {field.PageNumber}.");
            }

            var page = signedDocument.Pages[field.PageNumber - 1];
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            DrawSignedField(graphics, field, session.SignedByName.Trim(), completedAtUtc);
        }

        var signedStorageKey = _documentFileStore.CreateSignedStorageKey(document.OriginalFileName);
        await using var signedStream = new MemoryStream();
        signedDocument.Save(signedStream, false);
        signedStream.Position = 0;
        await _documentFileStore.SaveAsync(signedStorageKey, signedStream, cancellationToken);

        return new SignedDocumentArtifactResult(signedStorageKey);
    }

    private static void DrawSignedField(XGraphics graphics, SignatureField field, string signedByName, DateTimeOffset completedAtUtc)
    {
        var rect = new XRect((double)field.X, (double)field.Y, (double)field.Width, (double)field.Height);
        var outerBorder = new XPen(XColor.FromArgb(29, 78, 216), 1.4);
        var innerBorder = new XPen(XColor.FromArgb(147, 197, 253), 0.8);
        var labelBrush = new XSolidBrush(XColor.FromArgb(30, 41, 59));
        var valueBrush = new XSolidBrush(XColor.FromArgb(15, 23, 42));
        var timestampBrush = new XSolidBrush(XColor.FromArgb(71, 85, 105));
        var backgroundBrush = new XSolidBrush(XColor.FromArgb(245, 249, 255));

        graphics.DrawRectangle(backgroundBrush, rect);
        graphics.DrawRectangle(outerBorder, rect);

        var inset = 6d;
        var innerRect = new XRect(rect.X + inset, rect.Y + inset, Math.Max(10d, rect.Width - (inset * 2)), Math.Max(10d, rect.Height - (inset * 2)));
        graphics.DrawRectangle(innerBorder, innerRect);

        var labelFont = new XFont("Arial", 8, XFontStyle.Bold);
        var nameFont = new XFont("Arial", 13, XFontStyle.BoldItalic);
        var timestampFont = new XFont("Arial", 7, XFontStyle.Regular);

        graphics.DrawString(field.Label, labelFont, labelBrush, new XRect(innerRect.X + 4, innerRect.Y + 2, innerRect.Width - 8, 12), XStringFormats.TopLeft);
        graphics.DrawString(signedByName, nameFont, valueBrush, new XRect(innerRect.X + 4, innerRect.Y + 16, innerRect.Width - 8, Math.Max(16, innerRect.Height - 30)), XStringFormats.TopLeft);
        graphics.DrawString($"Signed {completedAtUtc:yyyy-MM-dd HH:mm} UTC", timestampFont, timestampBrush, new XRect(innerRect.X + 4, innerRect.Y + innerRect.Height - 12, innerRect.Width - 8, 10), XStringFormats.TopLeft);
    }

    private static void EnsureFontResolver()
    {
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new FontResolver();
        }
    }
}
