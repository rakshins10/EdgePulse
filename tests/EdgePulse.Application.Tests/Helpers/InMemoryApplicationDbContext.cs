using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Tests.Helpers;

/// <summary>
/// Lightweight in-memory DbContext for application command handler tests.
/// Uses InMemory provider; no SQL-specific configurations.
/// </summary>
public class InMemoryApplicationDbContext : DbContext, IApplicationDbContext
{
    public InMemoryApplicationDbContext(DbContextOptions<InMemoryApplicationDbContext> options)
        : base(options) { }

    public DbSet<DeviceType>           DeviceTypeSet       => Set<DeviceType>();
    public DbSet<DeviceStatus>         DeviceStatusSet     => Set<DeviceStatus>();
    public DbSet<DeviceManufacturer>   ManufacturerSet     => Set<DeviceManufacturer>();
    public DbSet<LocationType>         LocationTypeSet     => Set<LocationType>();
    public DbSet<MetricType>           MetricTypeSet       => Set<MetricType>();
    public DbSet<MaintenanceType>      MaintenanceTypeSet  => Set<MaintenanceType>();
    public DbSet<AlertSeverity>        AlertSeveritySet    => Set<AlertSeverity>();
    public DbSet<AlertStatus>          AlertStatusSet      => Set<AlertStatus>();
    public DbSet<IndustryTemplate>     IndustryTemplateSet => Set<IndustryTemplate>();
    public DbSet<TenantTemplate>       TenantTemplateSet   => Set<TenantTemplate>();
    public DbSet<Unit>                 UnitSet             => Set<Unit>();
    public DbSet<DeviceModel>          DeviceModelSet      => Set<DeviceModel>();
    public DbSet<TenantLookupOverride> OverrideSet         => Set<TenantLookupOverride>();
    public DbSet<DeviceApiKey>         DeviceApiKeySet     => Set<DeviceApiKey>();
    public DbSet<Tenant>               TenantSet           => Set<Tenant>();
    public DbSet<Mill>                 MillSet             => Set<Mill>();
    public DbSet<Area>                 AreaSet             => Set<Area>();
    public DbSet<Device>               DeviceSet           => Set<Device>();
    public DbSet<Attachment>           AttachmentSet       => Set<Attachment>();
    public DbSet<Notification>         NotificationSet     => Set<Notification>();

    // IApplicationDbContext explicit interface implementations
    IQueryable<IndustryTemplate>     IApplicationDbContext.IndustryTemplates     => Set<IndustryTemplate>();
    IQueryable<TenantTemplate>       IApplicationDbContext.TenantTemplates       => Set<TenantTemplate>();
    IQueryable<DeviceType>           IApplicationDbContext.DeviceTypes           => Set<DeviceType>();
    IQueryable<DeviceStatus>         IApplicationDbContext.DeviceStatuses        => Set<DeviceStatus>();
    IQueryable<AlertSeverity>        IApplicationDbContext.AlertSeverities       => Set<AlertSeverity>();
    IQueryable<AlertStatus>          IApplicationDbContext.AlertStatuses         => Set<AlertStatus>();
    IQueryable<MetricType>           IApplicationDbContext.MetricTypes           => Set<MetricType>();
    IQueryable<Unit>                 IApplicationDbContext.Units                 => Set<Unit>();
    IQueryable<DeviceManufacturer>   IApplicationDbContext.DeviceManufacturers   => Set<DeviceManufacturer>();
    IQueryable<DeviceModel>          IApplicationDbContext.DeviceModels          => Set<DeviceModel>();
    IQueryable<MaintenanceType>      IApplicationDbContext.MaintenanceTypes      => Set<MaintenanceType>();
    IQueryable<LocationType>         IApplicationDbContext.LocationTypes         => Set<LocationType>();
    IQueryable<TenantLookupOverride> IApplicationDbContext.TenantLookupOverrides => Set<TenantLookupOverride>();
    IQueryable<Tenant>               IApplicationDbContext.Tenants               => Set<Tenant>();
    IQueryable<Mill>                 IApplicationDbContext.Mills                 => Set<Mill>();
    IQueryable<Area>                 IApplicationDbContext.Areas                 => Set<Area>();
    IQueryable<Device>               IApplicationDbContext.Devices               => Set<Device>();
    IQueryable<Attachment>           IApplicationDbContext.Attachments           => Set<Attachment>();
    IQueryable<DeviceApiKey>         IApplicationDbContext.DeviceApiKeys         => Set<DeviceApiKey>();
    IQueryable<Locale>               IApplicationDbContext.Locales               => Set<Locale>();
    IQueryable<LookupTranslation>    IApplicationDbContext.LookupTranslations    => Set<LookupTranslation>();
    IQueryable<UiStringTranslation>  IApplicationDbContext.UiStringTranslations  => Set<UiStringTranslation>();
    IQueryable<AlertThreshold>       IApplicationDbContext.AlertThresholds       => Set<AlertThreshold>();
    IQueryable<Alert>                IApplicationDbContext.Alerts                => Set<Alert>();
    IQueryable<Notification>         IApplicationDbContext.Notifications         => Set<Notification>();

    public new void Add<TEntity>(TEntity entity) where TEntity : class => base.Add(entity);
    public new void Update<TEntity>(TEntity entity) where TEntity : class => base.Update(entity);
    public new void Remove<TEntity>(TEntity entity) where TEntity : class => base.Remove(entity);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);
}
