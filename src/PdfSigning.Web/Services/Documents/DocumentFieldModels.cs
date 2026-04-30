namespace PdfSigning.Web.Services.Documents;

public sealed record AddSignatureFieldRequest(
    string Label,
    int PageNumber,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    bool IsRequired = true);
