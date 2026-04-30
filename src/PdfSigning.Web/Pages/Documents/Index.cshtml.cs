using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Pages.Documents;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly IDocumentWorkflowService _documentWorkflowService;

    public IndexModel(IWebHostEnvironment environment, IDocumentWorkflowService documentWorkflowService)
    {
        _environment = environment;
        _documentWorkflowService = documentWorkflowService;
    }

    [BindProperty]
    public IFormFile? PdfFile { get; set; }

    public string? StatusMessage { get; private set; }
    public string? UploadedFileUrl { get; private set; }
    public string? UploadedFileName { get; private set; }
    public Guid? UploadedDocumentId { get; private set; }

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

        var ownerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            StatusMessage = "You must be signed in to upload a PDF.";
            return Page();
        }

        var title = Path.GetFileNameWithoutExtension(PdfFile.FileName);
        var request = new CreateDocumentRequest(
            OwnerUserId: ownerUserId,
            Title: string.IsNullOrWhiteSpace(title) ? PdfFile.FileName : title,
            OriginalFileName: PdfFile.FileName,
            ContentType: PdfFile.ContentType,
            StorageKey: $"uploads/{safeFileName}",
            SignatureFields:
            [
                new SignatureFieldRequest("Signature", 1, 360m, 720m, 180m, 60m, true)
            ]);

        var document = await _documentWorkflowService.CreateDocumentAsync(request);

        UploadedDocumentId = document.Id;
        UploadedFileName = PdfFile.FileName;
        UploadedFileUrl = $"/uploads/{safeFileName}";
        StatusMessage = "PDF uploaded successfully and saved to the database.";

        return Page();
    }
}
