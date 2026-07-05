using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Domain.Tests.Entities;

public class DeviceTests
{
    private static readonly Guid _tenantId  = Guid.NewGuid();
    private static readonly Guid _millId    = Guid.NewGuid();
    private static readonly Guid _areaId    = Guid.NewGuid();
    private static readonly Guid _typeId    = Guid.NewGuid();
    private static readonly Guid _statusId  = Guid.NewGuid();

    private static Device CreateDevice(string name = "Pump A", string code = "PA") =>
        Device.Create(_tenantId, _millId, _areaId, _typeId, _statusId, name, code);

    // ── Create ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsAllProperties_Correctly()
    {
        var device = Device.Create(
            _tenantId, _millId, _areaId, _typeId, _statusId,
            "Feed Pump", "fp-001",
            serialNumber: "SN123", installDate: new DateOnly(2024, 1, 15));

        device.TenantId.Should().Be(_tenantId);
        device.MillId.Should().Be(_millId);
        device.AreaId.Should().Be(_areaId);
        device.TypeId.Should().Be(_typeId);
        device.StatusId.Should().Be(_statusId);
        device.Name.Should().Be("Feed Pump");
        device.Code.Should().Be("FP-001");         // uppercased
        device.SerialNumber.Should().Be("SN123");
        device.InstallDate.Should().Be(new DateOnly(2024, 1, 15));
        device.IsDeleted.Should().BeFalse();
        device.LastSeenAt.Should().BeNull();
    }

    [Fact]
    public void Create_Code_IsUppercased()
    {
        var device = CreateDevice(code: "lower-code");
        device.Code.Should().Be("LOWER-CODE");
    }

    // ── UpdateStatus ───────────────────────────────────────────────────────────

    [Fact]
    public void UpdateStatus_ChangesStatusId()
    {
        var device   = CreateDevice();
        var newStatus = Guid.NewGuid();

        device.UpdateStatus(newStatus);

        device.StatusId.Should().Be(newStatus);
    }

    // ── UpdateLastSeen ─────────────────────────────────────────────────────────

    [Fact]
    public void UpdateLastSeen_SetsLastSeenAt_ToUtcNow()
    {
        var device = CreateDevice();
        device.UpdateLastSeen();

        device.LastSeenAt.Should().NotBeNull();
        device.LastSeenAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // ── UpdateDetails ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateDetails_OverwritesAllFields()
    {
        var device   = CreateDevice("Old Name", "OLD");
        var newArea  = Guid.NewGuid();
        var newType  = Guid.NewGuid();

        device.UpdateDetails("New Name", newArea, newType, null, null, "SN999", null, "Updated desc");

        device.Name.Should().Be("New Name");
        device.AreaId.Should().Be(newArea);
        device.TypeId.Should().Be(newType);
        device.SerialNumber.Should().Be("SN999");
        device.Description.Should().Be("Updated desc");
        device.ManufacturerId.Should().BeNull();
    }

    // ── Decommission ───────────────────────────────────────────────────────────

    [Fact]
    public void Decommission_SetsIsDeleted_And_DecommissionedStatus()
    {
        var device        = CreateDevice();
        var decommStatus  = Guid.NewGuid();

        device.Decommission(decommStatus);

        device.IsDeleted.Should().BeTrue();
        device.DeletedAt.Should().NotBeNull();
        device.StatusId.Should().Be(decommStatus);
    }
}
