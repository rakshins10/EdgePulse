using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Domain.Tests.Entities;

/// <summary>
/// Tests for shared behaviour on LookupBaseEntity (via DeviceType as a concrete type).
/// </summary>
public class LookupBaseEntityTests
{
    private static readonly Guid _tenantId   = Guid.NewGuid();
    private static readonly Guid _templateId = Guid.NewGuid();

    // ── CreateCustomValue ──────────────────────────────────────────────────────

    [Fact]
    public void CreateCustomValue_SetsProperties_Correctly()
    {
        var dt = DeviceType.CreateCustomValue(_tenantId, "Pump", "pump", "A pump device", null, 5);

        dt.Name.Should().Be("Pump");
        dt.Code.Should().Be("PUMP");          // toUpperInvariant
        dt.Description.Should().Be("A pump device");
        dt.TenantId.Should().Be(_tenantId);
        dt.TemplateId.Should().BeNull();
        dt.IsSystem.Should().BeFalse();
        dt.IsActive.Should().BeTrue();
        dt.SortOrder.Should().Be(5);
        dt.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void CreateCustomValue_Code_IsUppercased()
    {
        var dt = DeviceType.CreateCustomValue(_tenantId, "Sensor", "sensor_temp");
        dt.Code.Should().Be("SENSOR_TEMP");
    }

    // ── CreateSystemValue ──────────────────────────────────────────────────────

    [Fact]
    public void CreateSystemValue_SetsIsSystem_True()
    {
        var id = Guid.NewGuid();
        var dt = DeviceType.CreateSystemValue(id, _templateId, "Fan", "FAN");

        dt.Id.Should().Be(id);
        dt.IsSystem.Should().BeTrue();
        dt.TenantId.Should().BeNull();
        dt.TemplateId.Should().Be(_templateId);
    }

    // ── Deactivate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_CustomValue_SetsIsActive_False()
    {
        var dt = DeviceType.CreateCustomValue(_tenantId, "Pump", "pump");
        dt.Deactivate();
        dt.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_SystemValue_Throws()
    {
        var id = Guid.NewGuid();
        var dt = DeviceType.CreateSystemValue(id, _templateId, "Fan", "FAN");

        var act = () => dt.Deactivate();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*System lookup values*");
    }

    // ── Activate ───────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActive_True()
    {
        var dt = DeviceType.CreateCustomValue(_tenantId, "Pump", "pump");
        dt.Deactivate();
        dt.Activate();
        dt.IsActive.Should().BeTrue();
    }

    // ── UpdateDetails ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateDetails_CustomValue_UpdatesNameAndDescription()
    {
        var dt = DeviceType.CreateCustomValue(_tenantId, "Pump", "pump", "Old desc");
        var before = dt.UpdatedAt;

        dt.UpdateDetails("Motor", "Updated desc");

        dt.Name.Should().Be("Motor");
        dt.Description.Should().Be("Updated desc");
        dt.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void UpdateDetails_SystemValue_Throws()
    {
        var id = Guid.NewGuid();
        var dt = DeviceType.CreateSystemValue(id, _templateId, "Fan", "FAN");

        var act = () => dt.UpdateDetails("Wind", null);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*System lookup values*");
    }

    // ── MarkAsDeleted ──────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsDeleted_SetsIsDeleted_And_DeletedAt()
    {
        var dt = DeviceType.CreateCustomValue(_tenantId, "Pump", "pump");
        dt.MarkAsDeleted();

        dt.IsDeleted.Should().BeTrue();
        dt.DeletedAt.Should().NotBeNull();
        dt.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
