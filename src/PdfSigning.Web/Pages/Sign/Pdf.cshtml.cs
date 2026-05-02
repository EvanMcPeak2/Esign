using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Pages.Sign;

[AllowAnonymous]
public class PdfModel : PageModel
{
    private readonly IDocumentSigningService _documentSigningService;
    private readonly IDocumentFileStore _documentFileStore;

    public PdfModel(IDocumentSigningService documentSigningService, IDocumentFileStore documentFileStore)
    {
        _documentSigningService = documentSigningService;
        _documentFileStore = documentFileStore;
    }

    public async Task<IActionResult> OnGetAsync(Guid sessionId, string token, string recipientEmail)
    {
        var signingSession = await _documentSigningService.GetSigningSessionAsync(sessionId, token, recipientEmail);
        if (signingSession is null || string.IsNullOrWhiteSpace(signingSession.StorageKey))
        {
            return NotFound();
        }

        var stream = await _documentFileStore.OpenReadAsync(signingSession.StorageKey);
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, signingSession.ContentType ?? "application/pdf", signingSession.OriginalFileName);
    }
}
