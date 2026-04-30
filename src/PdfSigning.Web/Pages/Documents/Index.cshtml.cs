using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PdfSigning.Web.Pages.Documents;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public IndexModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [BindProperty]
    public IFormFile? PdfFile { get; set; }

    public string? StatusMessage { get; private set; }
    public string? UploadedFileUrl { get; private set; }
    public string? UploadedFileName { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (PdfFile is null || PdfFile.Length == 0)
        {
            StatusMessage = "Choose a PDF file to upload.";
            return Page();
        }

        if (!string.Equals(Path.GetExtension(PdfFile.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Only .pdf files are allowed.";
            return Page();
        }

        var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var safeFileName = $"{Guid.NewGuid():N}-{Path.GetFileName(PdfFile.FileName)}";
        var savePath = Path.Combine(uploadsDir, safeFileName);

        await using (var stream = System.IO.File.Create(savePath))
        {
            await PdfFile.CopyToAsync(stream);
        }

        UploadedFileName = PdfFile.FileName;
        UploadedFileUrl = $"/uploads/{safeFileName}";
        StatusMessage = "PDF uploaded successfully.";

        return Page();
    }
}
