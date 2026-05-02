using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Pages.Documents;

[Authorize]
public class SignedPdfModel : PageModel
{
    private readonly IDocumentReadService _documentReadService;
    private readonly IDocumentFileStore _documentFileStore;

    public SignedPdfModel(IDocumentReadService documentReadService, IDocumentFileStore documentFileStore)
    {
        _documentReadService = documentReadService;
        _documentFileStore = documentFileStore;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var ownerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Signed-in user ID was not available.");

        var document = await _documentReadService.GetDocumentDetailsAsync(id, ownerUserId);
        if (document is null || string.IsNullOrWhiteSpace(document.SignedArtifactStorageKey))
        {
            return NotFound();
        }

        var stream = await _documentFileStore.OpenReadAsync(document.SignedArtifactStorageKey);
        if (stream is null)
        {
            return NotFound();
        }

        var downloadName = $"{Path.GetFileNameWithoutExtension(document.OriginalFileName)}-signed.pdf";
        return File(stream, document.ContentType ?? "application/pdf", downloadName);
    }
}
