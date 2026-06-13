using EdgePulse.Domain.Entities;

namespace EdgePulse.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Lookup Tables
    IQueryable<IndustryTemplate> IndustryTemplates { get; }
    IQueryable<TenantTemplate> TenantTemplates { get; }
    IQueryable<DeviceType> DeviceTypes { get; }
    IQueryable<DeviceStatus> DeviceStatuses { get; }
    IQueryable<AlertSeverity> AlertSeverities { get; }
    IQueryable<AlertStatus> AlertStatuses { get; }
    IQueryable<MetricType> MetricTypes { get; }
    IQueryable<Unit> Units { get; }
    IQueryable<DeviceManufacturer> DeviceManufacturers { get; }
    IQueryable<DeviceModel> DeviceModels { get; }
    IQueryable<MaintenanceType> MaintenanceTypes { get; }
    IQueryable<LocationType> LocationTypes { get; }
    IQueryable<TenantLookupOverride> TenantLookupOverrides { get; }
    IQueryable<DeviceApiKey> DeviceApiKeys { get; }

    // Localization
    IQueryable<Locale> Locales { get; }
    IQueryable<LookupTranslation> LookupTranslations { get; }

    // Core Entities
    IQueryable<Tenant> Tenants { get; }
    IQueryable<Mill> Mills { get; }
    IQueryable<Area> Areas { get; }
    IQueryable<Device> Devices { get; }
    IQueryable<Attachment> Attachments { get; }

    // Alert Engine
    IQueryable<AlertThreshold> AlertThresholds { get; }
    IQueryable<Alert> Alerts { get; }

    // Write operations
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
}
