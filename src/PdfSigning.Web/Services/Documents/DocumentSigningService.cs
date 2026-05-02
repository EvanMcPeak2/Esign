using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using PdfSigning.Web.Data;
using PdfSigning.Web.Models.Documents;

namespace PdfSigning.Web.Services.Documents;

public sealed class DocumentSigningService : IDocumentSigningService
{
    private static readonly TimeSpan DefaultLinkLifetime = TimeSpan.FromHours(24);

    private readonly ApplicationDbContext _db;
    private readonly IClock _clock;
    private readonly ISignedDocumentArtifactService _signedDocumentArtifactService;

    public DocumentSigningService(ApplicationDbContext db, IClock clock, ISignedDocumentArtifactService signedDocumentArtifactService)
    {
        _db = db;
        _clock = clock;
        _signedDocumentArtifactService = signedDocumentArtifactService;
    }

    public async Task<CreateSigningSessionResult?> CreateSigningSessionAsync(Guid documentId, string ownerUserId, CreateSigningSessionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RecipientEmail);

        var document = await _db.Documents
            .Include(x => x.SignatureFields)
            .Include(x => x.SigningSessions)
            .SingleOrDefaultAsync(x => x.Id == documentId && x.OwnerUserId == ownerUserId, cancellationToken);

        if (document is null)
        {
            return null;
        }

        if (document.SignatureFields.Count == 0)
        {
            throw new InvalidOperationException("Add at least one signature field before creating a signing link.");
        }

        if (document.Status is not (DocumentStatus.ReadyForSigning or DocumentStatus.InSigning))
        {
            throw new InvalidOperationException("Mark the document ready for signing before sending a secure link.");
        }

        var now = _clock.UtcNow;
        foreach (var existingSession in document.SigningSessions.Where(x => x.CompletedAtUtc is null && x.RevokedAtUtc is null))
        {
            existingSession.RevokedAtUtc = now;
        }

        var accessToken = CreateAccessToken();
        var session = new SigningSession
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            RecipientEmail = NormalizeEmail(request.RecipientEmail),
            AccessTokenHash = HashAccessToken(accessToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(DefaultLinkLifetime),
        };

        _db.SigningSessions.Add(session);
        document.Status = DocumentStatus.InSigning;

        foreach (var field in document.SignatureFields)
        {
            field.SigningSessionId = session.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateSigningSessionResult(
            session.Id,
            document.Id,
            session.RecipientEmail,
            session.ExpiresAtUtc,
            accessToken);
    }

    public async Task<PendingSigningSessionDto?> GetPendingSigningSessionAsync(Guid sessionId, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var session = await _db.SigningSessions
            .AsNoTracking()
            .Include(x => x.Document)
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.AccessTokenHash == HashAccessToken(accessToken), cancellationToken);

        if (session is null || !IsSessionUsable(session))
        {
            return null;
        }

        return new PendingSigningSessionDto(
            session.Id,
            session.DocumentId,
            session.Document.Title,
            session.Document.OriginalFileName,
            session.Document.StorageKey ?? string.Empty,
            session.Document.ContentType,
            session.RecipientEmail,
            session.ExpiresAtUtc);
    }

    public async Task<SigningSessionViewDto?> GetSigningSessionAsync(Guid sessionId, string accessToken, string recipientEmail, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);

        var pendingSession = await GetPendingSigningSessionAsync(sessionId, accessToken, cancellationToken);
        if (pendingSession is null)
        {
            return null;
        }

        if (!string.Equals(NormalizeEmail(pendingSession.RecipientEmail), NormalizeEmail(recipientEmail), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var session = await _db.SigningSessions
            .AsNoTracking()
            .Include(x => x.Document)
            .ThenInclude(x => x.SignatureFields)
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.AccessTokenHash == HashAccessToken(accessToken), cancellationToken);

        if (session is null)
        {
            return null;
        }

        return new SigningSessionViewDto(
            session.Id,
            session.DocumentId,
            session.Document.Title,
            session.Document.OriginalFileName,
            session.Document.StorageKey ?? string.Empty,
            session.Document.ContentType,
            session.RecipientEmail,
            session.ExpiresAtUtc,
            session.Document.SignatureFields
                .Where(x => x.SigningSessionId == session.Id)
                .OrderBy(x => x.PageNumber)
                .ThenBy(x => x.CreatedAtUtc)
                .Select(x => new SignatureFieldDto(
                    x.Id,
                    x.Label,
                    x.PageNumber,
                    x.X,
                    x.Y,
                    x.Width,
                    x.Height,
                    x.IsRequired,
                    x.CreatedAtUtc))
                .ToList());
    }

    public async Task<SigningCompletionResult?> CompleteSigningAsync(Guid sessionId, string accessToken, CompleteSigningRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RecipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SignedByName);

        var pendingSession = await GetPendingSigningSessionAsync(sessionId, accessToken, cancellationToken);
        if (pendingSession is null || !string.Equals(NormalizeEmail(pendingSession.RecipientEmail), NormalizeEmail(request.RecipientEmail), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var session = await _db.SigningSessions
            .Include(x => x.Document)
            .ThenInclude(x => x.SignatureFields)
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.AccessTokenHash == HashAccessToken(accessToken), cancellationToken);

        if (session is null)
        {
            return null;
        }

        var now = _clock.UtcNow;
        session.CompletedAtUtc = now;
        session.SignedByName = request.SignedByName.Trim();

        var signedArtifact = await _signedDocumentArtifactService.CreateSignedArtifactAsync(session.Document, session, cancellationToken);
        session.Document.SignedArtifactStorageKey = signedArtifact.StorageKey;
        session.Document.Status = DocumentStatus.Signed;
        session.Document.CompletedAtUtc = now;

        await _db.SaveChangesAsync(cancellationToken);

        return new SigningCompletionResult(session.Id, session.DocumentId, now, session.SignedByName);
    }

    public async Task<bool> RevokeSigningSessionAsync(Guid sessionId, string ownerUserId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);

        var session = await _db.SigningSessions
            .Include(x => x.Document)
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.Document.OwnerUserId == ownerUserId, cancellationToken);

        if (session is null)
        {
            return false;
        }

        if (session.RevokedAtUtc is not null || session.CompletedAtUtc is not null)
        {
            return true;
        }

        session.RevokedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static string HashAccessToken(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(accessToken));
        return Convert.ToHexString(hash);
    }

    public static string CreateAccessToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private bool IsSessionUsable(SigningSession session)
    {
        if (session.RevokedAtUtc is not null || session.CompletedAtUtc is not null)
        {
            return false;
        }

        return session.ExpiresAtUtc > _clock.UtcNow;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
