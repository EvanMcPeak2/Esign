using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Pages.Documents;

[Authorize]
public class PdfModel : PageModel
{
    private readonly IDocumentReadService _documentReadService;
    private readonly IDocumentFileStore _documentFileStore;

    public PdfModel(IDocumentReadService documentReadService, IDocumentFileStore documentFileStore)
    {
        _documentReadService = documentReadService;
        _documentFileStore = documentFileStore;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var ownerUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Signed-in user ID was not available.");

        var document = await _documentReadService.GetDocumentDetailsAsync(id, ownerUserId);
        if (document is null || string.IsNullOrWhiteSpace(document.StorageKey))
        {
            return NotFound();
        }

        var stream = await _documentFileStore.OpenReadAsync(document.StorageKey);
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, document.ContentType ?? "application/pdf", document.OriginalFileName);
    }
}
