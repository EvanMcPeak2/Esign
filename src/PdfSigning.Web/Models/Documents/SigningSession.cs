namespace PdfSigning.Web.Models.Documents;

public class SigningSession
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Document Document { get; set; } = default!;

    public string RecipientEmail { get; set; } = string.Empty;

    public string AccessTokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public ICollection<SignatureField> SignatureFields { get; set; } = new List<SignatureField>();
}
