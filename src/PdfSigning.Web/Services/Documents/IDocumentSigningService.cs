namespace PdfSigning.Web.Services.Documents;

public interface IDocumentSigningService
{
    Task<CreateSigningSessionResult?> CreateSigningSessionAsync(Guid documentId, string ownerUserId, CreateSigningSessionRequest request, CancellationToken cancellationToken = default);

    Task<PendingSigningSessionDto?> GetPendingSigningSessionAsync(Guid sessionId, string accessToken, CancellationToken cancellationToken = default);

    Task<SigningSessionViewDto?> GetSigningSessionAsync(Guid sessionId, string accessToken, string recipientEmail, CancellationToken cancellationToken = default);

    Task<SigningCompletionResult?> CompleteSigningAsync(Guid sessionId, string accessToken, CompleteSigningRequest request, CancellationToken cancellationToken = default);

    Task<bool> RevokeSigningSessionAsync(Guid sessionId, string ownerUserId, CancellationToken cancellationToken = default);
}
