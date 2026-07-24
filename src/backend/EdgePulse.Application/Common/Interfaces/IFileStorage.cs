namespace EdgePulse.Application.Common.Interfaces;

/// <summary>
/// Physical file storage for attachments. The local implementation writes
/// under a configured root directory; a cloud implementation (Azure Blob)
/// can replace it without touching the handlers.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Persist a file stream. Returns the storage path (relative, provider
    /// specific) that must be kept on the Attachment record for later reads.
    /// </summary>
    Task<string> SaveAsync(
        Guid tenantId,
        string entityType,
        Guid entityId,
        string storedFileName,
        Stream content,
        CancellationToken cancellationToken);

    /// <summary>Open a stored file for reading.</summary>
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken);

    /// <summary>Delete a stored file. Missing files are ignored.</summary>
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
}
