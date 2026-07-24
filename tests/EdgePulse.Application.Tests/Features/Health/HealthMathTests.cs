using EdgePulse.Application.Features.Health;
using FluentAssertions;

namespace EdgePulse.Application.Tests.Features.Health;

public class HealthMathTests
{
    [Fact]
    public void AlertPenalty_WeightsBySeverity_AndCaps()
    {
        HealthMath.AlertPenalty(0, 0, 0, 0).Should().Be(0);
        HealthMath.AlertPenalty(1, 1, 0, 0).Should().Be(50);
        HealthMath.AlertPenalty(3, 3, 3, 3).Should().Be(60); // capped
    }

    [Fact]
    public void UtilizationPercent_AgainstUpperThreshold()
    {
        HealthMath.UtilizationPercent(60, null, 75).Should().Be(80);
    }

    [Fact]
    public void SlopePerDay_DetectsLinearRise()
    {
        // 70, 71, 72, 73 → +1/day
        HealthMath.SlopePerDay([70, 71, 72, 73]).Should().Be(1);
    }

    [Fact]
    public void SlopePerDay_TooFewPoints_IsZero()
    {
        HealthMath.SlopePerDay([70, 80]).Should().Be(0);
    }

    [Fact]
    public void DaysToThreshold_LinearExtrapolation()
    {
        // avg 70, limit 75, +1/day → 5 days
        HealthMath.DaysToThreshold(70, 75, 1).Should().Be(5);
        HealthMath.DaysToThreshold(70, 75, 0).Should().BeNull();   // no trend
        HealthMath.DaysToThreshold(70, null, 1).Should().BeNull(); // no limit
        HealthMath.DaysToThreshold(80, 75, 1).Should().Be(0);      // already over
        HealthMath.DaysToThreshold(70, 75, 0.01).Should().BeNull(); // >90 days out
    }

    [Fact]
    public void Score_AndGrade_Compose()
    {
        var score = HealthMath.Score(
            HealthMath.AlertPenalty(0, 1, 0, 0),   // 20
            HealthMath.UtilizationPenalty(85),      // 12
            HealthMath.TrendPenalty(20));           // 10
        score.Should().Be(58);
        HealthMath.Grade(score).Should().Be("DEGRADED");
        HealthMath.Grade(95).Should().Be("GOOD");
        HealthMath.Grade(20).Should().Be("CRITICAL");
    }
}
