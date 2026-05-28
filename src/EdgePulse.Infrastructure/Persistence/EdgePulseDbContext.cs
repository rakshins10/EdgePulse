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
    IQueryable<Tenant> IApplicationDbContext.Tenants => Set<Tenant>();
    IQueryable<Mill> IApplicationDbContext.Mills => Set<Mill>();
    IQueryable<Area> IApplicationDbContext.Areas => Set<Area>();
    IQueryable<Device> IApplicationDbContext.Devices => Set<Device>();
    IQueryable<Attachment> IApplicationDbContext.Attachments => Set<Attachment>();
    IQueryable<DeviceApiKey> IApplicationDbContext.DeviceApiKeys => Set<DeviceApiKey>();
    IQueryable<AlertThreshold> IApplicationDbContext.AlertThresholds => Set<AlertThreshold>();
    IQueryable<Alert> IApplicationDbContext.Alerts => Set<Alert>();

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
        return await base.SaveChangesAsync(cancellationToken);
    }
}
