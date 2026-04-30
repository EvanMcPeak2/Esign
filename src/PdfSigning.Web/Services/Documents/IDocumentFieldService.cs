namespace PdfSigning.Web.Services.Documents;

public interface IDocumentFieldService
{
    Task<SignatureFieldDto?> AddSignatureFieldAsync(Guid documentId, string ownerUserId, AddSignatureFieldRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteSignatureFieldAsync(Guid documentId, string ownerUserId, Guid signatureFieldId, CancellationToken cancellationToken = default);
}
