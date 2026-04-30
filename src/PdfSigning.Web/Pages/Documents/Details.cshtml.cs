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

    public DetailsModel(IDocumentReadService documentReadService, IDocumentFieldService documentFieldService)
    {
        _documentReadService = documentReadService;
        _documentFieldService = documentFieldService;
    }

    [BindProperty]
    public AddSignatureFieldRequest AddField { get; set; } = new("Signature", 1, 360m, 720m, 180m, 60m, true);

    public DocumentDetailsDto? Document { get; private set; }

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, string? message = null)
    {
        Document = await _documentReadService.GetDocumentDetailsAsync(id);

        if (Document is null)
        {
            StatusMessage = "Document not found.";
            return NotFound();
        }

        StatusMessage = message;
        return Page();
    }

    public async Task<IActionResult> OnPostAddFieldAsync(Guid id)
    {
        try
        {
            await _documentFieldService.AddSignatureFieldAsync(id, AddField);
            return RedirectToPage(new { id, message = "Signature field added." });
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            Document = await _documentReadService.GetDocumentDetailsAsync(id);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteFieldAsync(Guid id, Guid signatureFieldId)
    {
        try
        {
            await _documentFieldService.DeleteSignatureFieldAsync(id, signatureFieldId);
            return RedirectToPage(new { id, message = "Signature field deleted." });
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            Document = await _documentReadService.GetDocumentDetailsAsync(id);
            return Page();
        }
    }
}
