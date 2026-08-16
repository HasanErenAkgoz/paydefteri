namespace PayDefteri.Application.Common.Interfaces;

public interface IReceiptStorage
{
    Task<string> SaveAsync(
        Guid planId,
        Guid paymentId,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
