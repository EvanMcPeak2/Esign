using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Pages.Documents;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IDocumentReadService _documentReadService;
    private readonly IDocumentFieldService _documentFieldService;
    private readonly IDocumentStatusService _documentStatusService;

    public DetailsModel(IDocumentReadService documentReadService, IDocumentFieldService documentFieldService, IDocumentStatusService documentStatusService)
    {
        _documentReadService = documentReadService;
        _documentFieldService = documentFieldService;
        _documentStatusService = documentStatusService;
    }

    [BindProperty]
    public AddSignatureFieldRequest AddField { get; set; } = new("Signature", 1, 360m, 720m, 180m, 60m, true);

    public DocumentDetailsDto? Document { get; private set; }

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, string? message = null)
    {
        var ownerUserId = GetCurrentUserId();
        Document = await _documentReadService.GetDocumentDetailsAsync(id, ownerUserId);

        if (Document is null)
        {
            return NotFound();
        }

        StatusMessage = message;
        return Page();
    }

    public async Task<IActionResult> OnPostAddFieldAsync(Guid id)
    {
        var ownerUserId = GetCurrentUserId();
        var wantsJson = WantsJsonResponse();

        try
        {
            var addedField = await _documentFieldService.AddSignatureFieldAsync(id, ownerUserId, AddField);

            if (addedField is null)
            {
                return wantsJson
                    ? NotFound(new { message = "Document not found." })
                    : NotFound();
            }

            if (wantsJson)
            {
                return new JsonResult(new
                {
                    message = "Signature field added.",
                    field = addedField,
                });
            }

            return RedirectToPage(new { id, message = "Signature field added." });
        }
        catch (ArgumentException ex)
        {
            if (wantsJson)
            {
                return BadRequest(new { message = ex.Message });
            }

            StatusMessage = ex.Message;
            Document = await _documentReadService.GetDocumentDetailsAsync(id, ownerUserId);
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            if (wantsJson)
            {
                return BadRequest(new { message = ex.Message });
            }

            StatusMessage = ex.Message;
            Document = await _documentReadService.GetDocumentDetailsAsync(id, ownerUserId);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteFieldAsync(Guid id, Guid signatureFieldId)
    {
        var ownerUserId = GetCurrentUserId();
        var wantsJson = WantsJsonResponse();
        var deleted = await _documentFieldService.DeleteSignatureFieldAsync(id, ownerUserId, signatureFieldId);

        if (!deleted)
        {
            return wantsJson
                ? NotFound(new { message = "Signature field not found." })
                : NotFound();
        }

        if (wantsJson)
        {
            return new JsonResult(new
            {
                message = "Signature field deleted.",
                deletedFieldId = signatureFieldId,
            });
        }

        return RedirectToPage(new { id, message = "Signature field deleted." });
    }

    public async Task<IActionResult> OnPostMarkReadyAsync(Guid id)
    {
        var ownerUserId = GetCurrentUserId();

        try
        {
            var updated = await _documentStatusService.MarkReadyForSigningAsync(id, ownerUserId);

            if (!updated)
            {
                return NotFound();
            }

            return RedirectToPage(new { id, message = "Document marked ready for signing." });
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            Document = await _documentReadService.GetDocumentDetailsAsync(id, ownerUserId);
            return Page();
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Signed-in user ID was not available.");
    }

    private bool WantsJsonResponse()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
            || Request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}
