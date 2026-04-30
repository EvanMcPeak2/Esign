namespace PdfSigning.Web.Models.Documents;

public enum DocumentStatus
{
    Draft = 0,
    ReadyForSigning = 1,
    InSigning = 2,
    Signed = 3,
    Archived = 4
}
