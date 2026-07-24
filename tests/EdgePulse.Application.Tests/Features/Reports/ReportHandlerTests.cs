using EdgePulse.Application.Features.Reports.Queries;
using EdgePulse.Application.Tests.Helpers;
using EdgePulse.Domain.Entities;
using EdgePulse.Domain.Enums;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Reports;

public class ReportHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly DateTime _from = DateTime.UtcNow.AddDays(-7);
    private static readonly DateTime _to = DateTime.UtcNow.AddDays(1);

    private static (InMemoryApplicationDbContext ctx, TestCurrentUserService user) Setup()
        => (TestDbContextFactory.Create(), TestCurrentUserService.AsCustomerAdmin(_tenantId));

    private static Mill SeedMill(InMemoryApplicationDbContext ctx, string name = "Mill A")
    {
        var mill = Mill.Create(_tenantId, name, name.Replace(" ", ""), "Espoo", "Europe/Helsinki");
        ctx.Add(mill);
        ctx.SaveChanges();
        return mill;
    }

    private static Device SeedDevice(InMemoryApplicationDbContext ctx, Guid millId)
    {
        var device = Device.Create(
            _tenantId, millId, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), "Pump", $"P{Guid.NewGuid().ToString()[..4]}");
        ctx.Add(device);
        ctx.SaveChanges();
        return device;
    }

    private static Alert SeedAlert(
        InMemoryApplicationDbContext ctx, Guid millId, Guid deviceId,
        string severity = "HIGH", bool acknowledge = false, bool resolve = false)
    {
        var alert = Alert.Create(
            _tenantId, deviceId, millId, Guid.NewGuid(), Guid.NewGuid(),
            "bearing_temp", 92.5, 75, severity, "C");
        if (acknowledge) alert.Acknowledge("tester");
        if (resolve) { alert.Acknowledge("tester"); alert.Resolve("tester"); }
        ctx.Add(alert);
        ctx.SaveChanges();
        return alert;
    }

    // ── Mill comparison ──────────────────────────────────────────────────────

    [Fact]
    public async Task MillComparison_AggregatesPerMill()
    {
        var (ctx, user) = Setup();
        var millA = SeedMill(ctx, "Mill A");
        var millB = SeedMill(ctx, "Mill B");
        var devA = SeedDevice(ctx, millA.Id);
        SeedDevice(ctx, millA.Id);
        var devB = SeedDevice(ctx, millB.Id);

        SeedAlert(ctx, millA.Id, devA.Id, "CRITICAL");             // open
        SeedAlert(ctx, millA.Id, devA.Id, "HIGH", resolve: true);  // resolved
        SeedAlert(ctx, millB.Id, devB.Id, "LOW", acknowledge: true);

        var handler = new GetMillComparisonReportQueryHandler(ctx, user);
        var report = await handler.Handle(
            new GetMillComparisonReportQuery(_from, _to), CancellationToken.None);

        report.Mills.Should().HaveCount(2);
        var a = report.Mills.Single(m => m.MillName == "Mill A");
        a.DeviceCount.Should().Be(2);
        a.TotalAlerts.Should().Be(2);
        a.CriticalAlerts.Should().Be(1);
        a.OpenAlerts.Should().Be(1);              // the CRITICAL one is still open
        a.AvgResolveMinutes.Should().NotBeNull(); // one resolved alert

        var b = report.Mills.Single(m => m.MillName == "Mill B");
        b.DeviceCount.Should().Be(1);
        b.OpenAlerts.Should().Be(1);              // ACKNOWLEDGED still counts as open
        b.AvgAcknowledgeMinutes.Should().NotBeNull();
    }

    [Fact]
    public async Task MillComparison_MillManager_SeesOnlyTheirMill()
    {
        var (ctx, _) = Setup();
        var millA = SeedMill(ctx, "Mill A");
        SeedMill(ctx, "Mill B");

        var manager = new TestCurrentUserService
        {
            Role = UserRole.MillManager,
            TenantId = _tenantId,
            MillId = millA.Id,
        };

        var handler = new GetMillComparisonReportQueryHandler(ctx, manager);
        var report = await handler.Handle(
            new GetMillComparisonReportQuery(_from, _to), CancellationToken.None);

        report.Mills.Should().ContainSingle(m => m.MillId == millA.Id);
    }

    [Fact]
    public async Task MillComparison_ExcludesOtherTenants()
    {
        var (ctx, user) = Setup();
        SeedMill(ctx, "Mine");
        ctx.Add(Mill.Create(Guid.NewGuid(), "Foreign", "F", "Oslo", "Europe/Oslo"));
        await ctx.SaveChangesAsync(CancellationToken.None);

        var handler = new GetMillComparisonReportQueryHandler(ctx, user);
        var report = await handler.Handle(
            new GetMillComparisonReportQuery(_from, _to), CancellationToken.None);

        report.Mills.Should().ContainSingle(m => m.MillName == "Mine");
    }

    // ── Alerts CSV ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AlertsCsv_ContainsHeaderAndAlertRow()
    {
        var (ctx, user) = Setup();
        var mill = SeedMill(ctx);
        var device = SeedDevice(ctx, mill.Id);
        SeedAlert(ctx, mill.Id, device.Id, "CRITICAL");

        var handler = new ExportAlertsCsvQueryHandler(ctx, user);
        var csv = await handler.Handle(
            new ExportAlertsCsvQuery(_from, _to), CancellationToken.None);

        var lines = csv.TrimEnd().Split('\n');
        lines[0].Should().Contain("Device").And.Contain("Severity");
        lines.Should().HaveCount(2);
        lines[1].Should().Contain("bearing_temp").And.Contain("CRITICAL");
    }

    [Fact]
    public async Task AlertsCsv_EscapesCommasInFields()
    {
        // CsvBuilder unit behaviour via the handler: device name with comma
        var (ctx, user) = Setup();
        var mill = SeedMill(ctx);
        var device = Device.Create(
            _tenantId, mill.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Pump, big", "PB1");
        ctx.Add(device);
        ctx.SaveChanges();
        SeedAlert(ctx, mill.Id, device.Id);

        var handler = new ExportAlertsCsvQueryHandler(ctx, user);
        var csv = await handler.Handle(
            new ExportAlertsCsvQuery(_from, _to), CancellationToken.None);

        csv.Should().Contain("\"Pump, big\"");
    }
}
