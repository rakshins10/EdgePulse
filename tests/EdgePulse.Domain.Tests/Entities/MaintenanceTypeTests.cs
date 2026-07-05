using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Domain.Tests.Entities;

public class MaintenanceTypeTests
{
    private static readonly Guid _tenantId   = Guid.NewGuid();
    private static readonly Guid _templateId = Guid.NewGuid();

    [Fact]
    public void CreateCustomValue_SetsColor_Correctly()
    {
        var mt = MaintenanceType.CreateCustomValue(_tenantId, "Preventive", "prev", "#3b82f6");

        mt.Color.Should().Be("#3b82f6");
        mt.Code.Should().Be("PREV");          // custom codes are uppercased
        mt.IsSystem.Should().BeFalse();
        mt.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void CreateSystemValue_SetsColorAndIsSystem()
    {
        var id = Guid.NewGuid();
        var mt = MaintenanceType.CreateSystemValue(id, _templateId, "Routine", "ROUTINE", "#22c55e");

        mt.Id.Should().Be(id);
        mt.Color.Should().Be("#22c55e");
        mt.IsSystem.Should().BeTrue();
        mt.TemplateId.Should().Be(_templateId);
        mt.TenantId.Should().BeNull();
    }

    [Fact]
    public void Deactivate_SystemValue_Throws()
    {
        var id = Guid.NewGuid();
        var mt = MaintenanceType.CreateSystemValue(id, _templateId, "Routine", "ROUTINE");

        var act = () => mt.Deactivate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateColor_CustomValue_UpdatesColor()
    {
        var mt = MaintenanceType.CreateCustomValue(_tenantId, "Preventive", "PREV", "#3b82f6");
        mt.UpdateColor("#ef4444");
        mt.Color.Should().Be("#ef4444");
    }

    [Fact]
    public void UpdateColor_ToNull_ClearsColor()
    {
        var mt = MaintenanceType.CreateCustomValue(_tenantId, "Preventive", "PREV", "#3b82f6");
        mt.UpdateColor(null);
        mt.Color.Should().BeNull();
    }

    [Fact]
    public void UpdateColor_SystemValue_Throws()
    {
        var id = Guid.NewGuid();
        var mt = MaintenanceType.CreateSystemValue(id, _templateId, "Routine", "ROUTINE", "#22c55e");

        var act = () => mt.UpdateColor("#ff0000");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*System maintenance types*");
    }
}
