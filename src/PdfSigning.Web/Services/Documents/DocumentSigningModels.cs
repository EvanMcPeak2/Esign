namespace PdfSigning.Web.Services.Documents;

public sealed record CreateSigningSessionRequest(string RecipientEmail);

public sealed record CreateSigningSessionResult(
    Guid SessionId,
    Guid DocumentId,
    string RecipientEmail,
    DateTimeOffset ExpiresAtUtc,
    string AccessToken);

public sealed record SigningSessionViewDto(
    Guid SessionId,
    Guid DocumentId,
    string DocumentTitle,
    string OriginalFileName,
    string StorageKey,
    string? ContentType,
    string RecipientEmail,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<SignatureFieldDto> SignatureFields);

public sealed record PendingSigningSessionDto(
    Guid SessionId,
    Guid DocumentId,
    string DocumentTitle,
    string OriginalFileName,
    string StorageKey,
    string? ContentType,
    string RecipientEmail,
    DateTimeOffset ExpiresAtUtc);

public sealed record CompleteSigningRequest(
    string RecipientEmail,
    string SignedByName);

public sealed record SigningCompletionResult(
    Guid SessionId,
    Guid DocumentId,
    DateTimeOffset CompletedAtUtc,
    string SignedByName);
