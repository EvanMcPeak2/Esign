using PdfSigning.Web.Models.Documents;

namespace PdfSigning.Web.Services.Documents;

public sealed record SignatureFieldDto(
    Guid Id,
    string Label,
    int PageNumber,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    bool IsRequired,
    DateTimeOffset CreatedAtUtc);

public sealed record DocumentDetailsDto(
    Guid Id,
    string OwnerUserId,
    string Title,
    string OriginalFileName,
    string? ContentType,
    string? StorageKey,
    string? SignedArtifactStorageKey,
    DocumentStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<SignatureFieldDto> SignatureFields);

public sealed record DocumentSummaryDto(
    Guid Id,
    string Title,
    string OriginalFileName,
    string? StorageKey,
    DocumentStatus Status,
    DateTimeOffset CreatedAtUtc,
    int SignatureFieldCount);
