using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PdfSigning.Web.Pages.Documents;

[Authorize]
public class ViewModel : PageModel
{
    public IActionResult OnGet(Guid id)
    {
        return Redirect($"/Documents/Details/{id}");
    }
}
