using EdgePulse.Application.Features.Energy;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Energy;

public class EnergyMathTests
{
    [Fact]
    public void EnergyKwh_AveragePowerTimesObservedHours()
    {
        var start = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(12);

        // 1500 kW for 12 h = 18 000 kWh
        EnergyMath.EnergyKwh(1500, start, end).Should().Be(18_000);
    }

    [Fact]
    public void EnergyKwh_SingleSample_NoObservedDuration_IsZero()
    {
        var ts = DateTime.UtcNow;
        EnergyMath.EnergyKwh(1500, ts, ts).Should().Be(0);
    }

    [Fact]
    public void EnergyKwh_NegativePower_IsZero()
    {
        var start = DateTime.UtcNow;
        EnergyMath.EnergyKwh(-5, start, start.AddHours(1)).Should().Be(0);
    }

    [Fact]
    public void Co2Kg_AppliesGridFactor()
    {
        // 18 000 kWh at EU-average 0.181 kg/kWh = 3 258 kg
        EnergyMath.Co2Kg(18_000, 0.181).Should().Be(3258);
    }
}
