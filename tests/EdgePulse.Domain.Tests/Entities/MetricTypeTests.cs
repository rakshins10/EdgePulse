using EdgePulse.Domain.Entities;
using FluentAssertions;

namespace EdgePulse.Domain.Tests.Entities;

public class MetricTypeTests
{
    private static readonly Guid _tenantId   = Guid.NewGuid();
    private static readonly Guid _templateId = Guid.NewGuid();

    [Fact]
    public void CreateCustomValue_SetsDefaultUnit_AndUppercasesCode()
    {
        var mt = MetricType.CreateCustomValue(_tenantId, "Temperature", "temp", "C");

        mt.DefaultUnit.Should().Be("C");
        mt.Code.Should().Be("TEMP");
        mt.IsSystem.Should().BeFalse();
        mt.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void UpdateDefaultUnit_CustomValue_UpdatesUnit()
    {
        var mt = MetricType.CreateCustomValue(_tenantId, "Vibration", "VIB", "mm/s");
        mt.UpdateDefaultUnit("um/s");
        mt.DefaultUnit.Should().Be("um/s");
    }

    [Fact]
    public void UpdateDefaultUnit_SystemValue_Throws()
    {
        var mt = MetricType.CreateSystemValue(
            Guid.NewGuid(), _templateId, "Pressure", "PRESSURE", "bar");

        var act = () => mt.UpdateDefaultUnit("psi");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*System metric types*");
    }
}
