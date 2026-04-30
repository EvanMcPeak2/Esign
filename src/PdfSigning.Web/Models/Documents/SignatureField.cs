namespace PdfSigning.Web.Models.Documents;

public class SignatureField
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Document Document { get; set; } = default!;

    public Guid? SigningSessionId { get; set; }

    public SigningSession? SigningSession { get; set; }

    public string Label { get; set; } = string.Empty;

    public int PageNumber { get; set; }

    public decimal X { get; set; }

    public decimal Y { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public bool IsRequired { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
