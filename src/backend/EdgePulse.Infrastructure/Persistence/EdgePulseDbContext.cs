using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Infrastructure.Persistence;

public class EdgePulseDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUser;

    public EdgePulseDbContext(
        DbContextOptions<EdgePulseDbContext> options,
        ICurrentUserService currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    // Lookup Tables
    public DbSet<IndustryTemplate> IndustryTemplates => Set<IndustryTemplate>();
    public DbSet<TenantTemplate> TenantTemplates => Set<TenantTemplate>();
    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<DeviceStatus> DeviceStatuses => Set<DeviceStatus>();
    public DbSet<AlertSeverity> AlertSeverities => Set<AlertSeverity>();
    public DbSet<AlertStatus> AlertStatuses => Set<AlertStatus>();
    public DbSet<MetricType> MetricTypes => Set<MetricType>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<DeviceManufacturer> DeviceManufacturers => Set<DeviceManufacturer>();
    public DbSet<DeviceModel> DeviceModels => Set<DeviceModel>();
    public DbSet<MaintenanceType> MaintenanceTypes => Set<MaintenanceType>();
    public DbSet<LocationType> LocationTypes => Set<LocationType>();
    public DbSet<TenantLookupOverride> TenantLookupOverrides => Set<TenantLookupOverride>();

    // Localization
    public DbSet<Locale> Locales => Set<Locale>();
    public DbSet<LookupTranslation> LookupTranslations => Set<LookupTranslation>();
    public DbSet<UiStringTranslation> UiStringTranslations => Set<UiStringTranslation>();

    // Core Entities
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Mill> Mills => Set<Mill>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<DeviceApiKey> DeviceApiKeys => Set<DeviceApiKey>();

    // Alert Engine
    public DbSet<AlertThreshold> AlertThresholds => Set<AlertThreshold>();
    public DbSet<Alert> Alerts => Set<Alert>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    // Work Orders
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Webhooks
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();

    // IApplicationDbContext explicit implementations
    IQueryable<IndustryTemplate> IApplicationDbContext.IndustryTemplates => Set<IndustryTemplate>();
    IQueryable<TenantTemplate> IApplicationDbContext.TenantTemplates => Set<TenantTemplate>();
    IQueryable<DeviceType> IApplicationDbContext.DeviceTypes => Set<DeviceType>();
    IQueryable<DeviceStatus> IApplicationDbContext.DeviceStatuses => Set<DeviceStatus>();
    IQueryable<AlertSeverity> IApplicationDbContext.AlertSeverities => Set<AlertSeverity>();
    IQueryable<AlertStatus> IApplicationDbContext.AlertStatuses => Set<AlertStatus>();
    IQueryable<MetricType> IApplicationDbContext.MetricTypes => Set<MetricType>();
    IQueryable<Unit> IApplicationDbContext.Units => Set<Unit>();
    IQueryable<DeviceManufacturer> IApplicationDbContext.DeviceManufacturers => Set<DeviceManufacturer>();
    IQueryable<DeviceModel> IApplicationDbContext.DeviceModels => Set<DeviceModel>();
    IQueryable<MaintenanceType> IApplicationDbContext.MaintenanceTypes => Set<MaintenanceType>();
    IQueryable<LocationType> IApplicationDbContext.LocationTypes => Set<LocationType>();
    IQueryable<TenantLookupOverride> IApplicationDbContext.TenantLookupOverrides => Set<TenantLookupOverride>();
    IQueryable<Locale> IApplicationDbContext.Locales => Set<Locale>();
    IQueryable<LookupTranslation> IApplicationDbContext.LookupTranslations => Set<LookupTranslation>();
    IQueryable<UiStringTranslation> IApplicationDbContext.UiStringTranslations => Set<UiStringTranslation>();
    IQueryable<Tenant> IApplicationDbContext.Tenants => Set<Tenant>();
    IQueryable<Mill> IApplicationDbContext.Mills => Set<Mill>();
    IQueryable<Area> IApplicationDbContext.Areas => Set<Area>();
    IQueryable<Device> IApplicationDbContext.Devices => Set<Device>();
    IQueryable<Attachment> IApplicationDbContext.Attachments => Set<Attachment>();
    IQueryable<DeviceApiKey> IApplicationDbContext.DeviceApiKeys => Set<DeviceApiKey>();
    IQueryable<AlertThreshold> IApplicationDbContext.AlertThresholds => Set<AlertThreshold>();
    IQueryable<Alert> IApplicationDbContext.Alerts => Set<Alert>();
    IQueryable<Notification> IApplicationDbContext.Notifications => Set<Notification>();
    IQueryable<WorkOrder> IApplicationDbContext.WorkOrders => Set<WorkOrder>();
    IQueryable<AuditLog> IApplicationDbContext.AuditLogs => Set<AuditLog>();
    IQueryable<WebhookSubscription> IApplicationDbContext.WebhookSubscriptions => Set<WebhookSubscription>();

    // Use 'new' keyword -- these hide DbContext methods intentionally
    public new void Add<TEntity>(TEntity entity) where TEntity : class
        => base.Add(entity);

    public new void Update<TEntity>(TEntity entity) where TEntity : class
        => base.Update(entity);

    public new void Remove<TEntity>(TEntity entity) where TEntity : class
        => base.Remove(entity);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(
            typeof(EdgePulseDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        CaptureAuditTrail();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Writes an AuditLog row for every tracked create / update / delete.
    /// Soft deletes (IsDeleted flipping to true) are recorded as DELETED.
    /// AuditLog itself and system-generated Notifications are excluded.
    /// Entity ids are generated client-side (BaseEntity), so rows can be
    /// built before the actual save and persisted in the same transaction.
    /// </summary>
    private void CaptureAuditTrail()
    {
        // Properties that change on every write and would only add noise
        string[] noiseProps = ["UpdatedAt", "CreatedAt"];

        var entries = ChangeTracker.Entries()
            .Where(e =>
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                e.Entity is not AuditLog &&
                e.Entity is not Notification)
            .ToList();

        if (entries.Count == 0) return;

        var userName = new[] { _currentUser.FullName, _currentUser.Email, _currentUser.UserId }
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "system";

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType().Name;
            var entityId = entry.Property("Id").CurrentValue as Guid? ?? Guid.Empty;
            var tenantId = entry.Metadata.FindProperty("TenantId") is not null
                ? entry.Property("TenantId").CurrentValue as Guid? ?? _currentUser.TenantId
                : _currentUser.TenantId;
            var display = entry.Metadata.FindProperty("Name") is not null
                ? entry.Property("Name").CurrentValue?.ToString()
                : entry.Metadata.FindProperty("Title") is not null
                    ? entry.Property("Title").CurrentValue?.ToString()
                    : null;

            string action;
            string? changesJson = null;

            switch (entry.State)
            {
                case EntityState.Added:
                    action = "CREATED";
                    break;

                case EntityState.Deleted:
                    action = "DELETED";
                    break;

                default: // Modified
                    var changes = entry.Properties
                        .Where(p => p.IsModified &&
                                    !noiseProps.Contains(p.Metadata.Name) &&
                                    !Equals(p.OriginalValue, p.CurrentValue))
                        .ToDictionary(
                            p => p.Metadata.Name,
                            p => new
                            {
                                old = p.OriginalValue?.ToString(),
                                @new = p.CurrentValue?.ToString(),
                            });

                    if (changes.Count == 0) continue; // nothing meaningful changed

                    // Soft delete surfaces as Modified with IsDeleted false→true
                    action = changes.TryGetValue("IsDeleted", out var deleted) &&
                             deleted.@new == "True"
                        ? "DELETED"
                        : "UPDATED";
                    changesJson = System.Text.Json.JsonSerializer.Serialize(changes);
                    break;
            }

            AuditLogs.Add(AuditLog.Create(
                tenantId, userName, action, entityType, entityId, display, changesJson));
        }
    }
}
