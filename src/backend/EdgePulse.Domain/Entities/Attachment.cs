using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class Attachment : TenantBaseEntity
{
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StoredFileName { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public string FileCategory { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public bool IsPublic { get; private set; }
    public int DisplayOrder { get; private set; }
    public string UploadedBy { get; private set; } = string.Empty;
    public DateTime UploadedAt { get; private set; }

    protected Attachment() { }

    public static Attachment Create(
        Guid tenantId,
        string entityType,
        Guid entityId,
        string fileName,
        string storedFileName,
        long fileSize,
        string contentType,
        string fileCategory,
        string storagePath,
        string uploadedBy,
        bool isPublic = false,
        int displayOrder = 0)
    {
        return new Attachment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = entityType,
            EntityId = entityId,
            FileName = fileName,
            StoredFileName = storedFileName,
            FileSize = fileSize,
            ContentType = contentType,
            FileCategory = fileCategory,
            StoragePath = storagePath,
            UploadedBy = uploadedBy,
            IsPublic = isPublic,
            DisplayOrder = displayOrder,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void SoftDelete()
    {
        MarkAsDeleted();
    }

    public void UpdateDisplayOrder(int order)
    {
        DisplayOrder = order;
        MarkAsUpdated();
    }
}
