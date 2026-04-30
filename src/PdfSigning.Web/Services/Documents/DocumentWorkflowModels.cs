namespace PdfSigning.Web.Services.Documents;

public sealed record SignatureFieldRequest(
    string Label,
    int PageNumber,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    bool IsRequired = true);

public sealed record CreateDocumentRequest(
    string OwnerUserId,
    string Title,
    string OriginalFileName,
    string? ContentType,
    string StorageKey,
    IReadOnlyList<SignatureFieldRequest> SignatureFields);
