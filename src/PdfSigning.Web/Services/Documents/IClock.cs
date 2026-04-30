namespace PdfSigning.Web.Services.Documents;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
