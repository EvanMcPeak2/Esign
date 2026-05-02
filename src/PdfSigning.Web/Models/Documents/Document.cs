using PdfSigning.Web.Models;

namespace PdfSigning.Web.Models.Documents;

public class Document
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public string? StorageKey { get; set; }

    public string? SignedArtifactStorageKey { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string OwnerUserId { get; set; } = string.Empty;

    public ApplicationUser OwnerUser { get; set; } = default!;

    public ICollection<SignatureField> SignatureFields { get; set; } = new List<SignatureField>();

    public ICollection<SigningSession> SigningSessions { get; set; } = new List<SigningSession>();
}
