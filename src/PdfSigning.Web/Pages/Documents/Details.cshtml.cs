using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Pages.Documents;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IDocumentReadService _documentReadService;

    public DetailsModel(IDocumentReadService documentReadService)
    {
        _documentReadService = documentReadService;
    }

    public DocumentDetailsDto? Document { get; private set; }

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Document = await _documentReadService.GetDocumentDetailsAsync(id);

        if (Document is null)
        {
            StatusMessage = "Document not found.";
            return NotFound();
        }

        return Page();
    }
}
