using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Pages.Sign;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly IDocumentSigningService _documentSigningService;

    public IndexModel(IDocumentSigningService documentSigningService)
    {
        _documentSigningService = documentSigningService;
    }

    [BindProperty]
    public string RecipientEmail { get; set; } = string.Empty;

    [BindProperty]
    public string SignedByName { get; set; } = string.Empty;

    [BindProperty]
    public string AccessToken { get; set; } = string.Empty;

    public Guid SessionId { get; private set; }

    public PendingSigningSessionDto? PendingSession { get; private set; }

    public SigningSessionViewDto? SigningSession { get; private set; }

    public bool IsComplete { get; private set; }

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid sessionId, string token)
    {
        SessionId = sessionId;
        AccessToken = token;
        PendingSession = await _documentSigningService.GetPendingSigningSessionAsync(sessionId, token);

        if (PendingSession is null)
        {
            return NotFound();
        }

        RecipientEmail = PendingSession.RecipientEmail;
        return Page();
    }

    public async Task<IActionResult> OnPostVerifyAsync(Guid sessionId)
    {
        SessionId = sessionId;
        PendingSession = await _documentSigningService.GetPendingSigningSessionAsync(sessionId, AccessToken);

        if (PendingSession is null)
        {
            return NotFound();
        }

        var signingSession = await _documentSigningService.GetSigningSessionAsync(sessionId, AccessToken, RecipientEmail);
        if (signingSession is null)
        {
            StatusMessage = "That email address does not match this secure signing link.";
            RecipientEmail = PendingSession.RecipientEmail;
            return Page();
        }

        SigningSession = signingSession;
        RecipientEmail = signingSession.RecipientEmail;
        StatusMessage = "Email verified. Review the document and complete your signature when ready.";
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid sessionId)
    {
        SessionId = sessionId;
        PendingSession = await _documentSigningService.GetPendingSigningSessionAsync(sessionId, AccessToken);

        if (PendingSession is null)
        {
            return NotFound();
        }

        var result = await _documentSigningService.CompleteSigningAsync(sessionId, AccessToken, new CompleteSigningRequest(
            RecipientEmail: RecipientEmail,
            SignedByName: SignedByName));

        if (result is null)
        {
            StatusMessage = "That signing link is no longer valid.";
            RecipientEmail = PendingSession.RecipientEmail;
            return Page();
        }

        IsComplete = true;
        StatusMessage = $"Signed by {result.SignedByName} at {result.CompletedAtUtc.ToLocalTime():g}.";
        RecipientEmail = PendingSession.RecipientEmail;
        return Page();
    }
}
