namespace EdgePulse.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        string tenantId,
        string entityType,
        string entityId,
        string fileName,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<string> GetSignedUrlAsync(
        string storagePath,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
}
