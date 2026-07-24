using EdgePulse.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EdgePulse.Infrastructure.Services;

/// <summary>
/// Stores attachment files on the local filesystem under
/// {Storage:AttachmentsRoot}/{tenantId}/{entityType}/{entityId}/{storedFileName}.
/// In Docker the root should be a mounted volume so files survive restarts.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IConfiguration configuration)
    {
        _root = configuration["Storage:AttachmentsRoot"] ?? "data/attachments";
    }

    public async Task<string> SaveAsync(
        Guid tenantId, string entityType, Guid entityId,
        string storedFileName, Stream content,
        CancellationToken cancellationToken)
    {
        var relative = Path.Combine(
            tenantId.ToString(), entityType, entityId.ToString(), storedFileName);
        var full = Path.Combine(_root, relative);

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        await using var target = File.Create(full);
        await content.CopyToAsync(target, cancellationToken);

        // Store with forward slashes so paths are portable across OSes
        return relative.Replace('\\', '/');
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var full = Path.Combine(_root, storagePath);
        if (!File.Exists(full))
            throw new FileNotFoundException("Stored attachment file not found.", full);
        return Task.FromResult<Stream>(File.OpenRead(full));
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        var full = Path.Combine(_root, storagePath);
        if (File.Exists(full))
            File.Delete(full);
        return Task.CompletedTask;
    }
}
