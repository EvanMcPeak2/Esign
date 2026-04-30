namespace PdfSigning.Web.Services.Documents;

public interface IDocumentFieldService
{
    Task<SignatureFieldDto> AddSignatureFieldAsync(Guid documentId, AddSignatureFieldRequest request, CancellationToken cancellationToken = default);

    Task DeleteSignatureFieldAsync(Guid documentId, Guid signatureFieldId, CancellationToken cancellationToken = default);
}
