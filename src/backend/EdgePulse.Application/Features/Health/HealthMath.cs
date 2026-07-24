namespace EdgePulse.Application.Features.Health;

/// <summary>
/// Statistical device-health scoring — deliberately transparent arithmetic,
/// not opaque ML. A health score starts at 100 and loses points for:
///   • open alerts (severity-weighted),
///   • metrics running close to their alert threshold (utilization),
///   • metrics trending towards their threshold (degradation).
/// A naive remaining-useful-life figure is the linear extrapolation of the
/// worst degrading metric until it crosses its threshold.
/// </summary>
public static class HealthMath
{
    public static int AlertPenalty(int critical, int high, int medium, int low)
        => Math.Min(60, critical * 30 + high * 20 + medium * 10 + low * 5);

    /// <summary>How close a metric runs to its limit, 0–100+ (%).</summary>
    public static double UtilizationPercent(double average, double? min, double? max)
    {
        if (max is not null && max.Value != 0)
            return Math.Round(average / max.Value * 100, 1);
        if (min is not null && min.Value != 0)
            // For lower-bound thresholds utilization grows as value falls toward min
            return Math.Round(min.Value / Math.Max(average, 0.0001) * 100, 1);
        return 0;
    }

    /// <summary>Penalty for running hot: kicks in above 70 % of the limit.</summary>
    public static int UtilizationPenalty(double utilizationPercent)
        => utilizationPercent switch
        {
            >= 100 => 30,
            >= 90 => 20,
            >= 80 => 12,
            >= 70 => 6,
            _ => 0,
        };

    /// <summary>
    /// Least-squares slope of (day-index, value) pairs — units per day.
    /// Needs at least 3 points; otherwise 0 (no trend claim).
    /// </summary>
    public static double SlopePerDay(IReadOnlyList<double> dailyAverages)
    {
        var n = dailyAverages.Count;
        if (n < 3) return 0;

        double sumX = 0, sumY = 0, sumXy = 0, sumXx = 0;
        for (var i = 0; i < n; i++)
        {
            sumX += i;
            sumY += dailyAverages[i];
            sumXy += i * dailyAverages[i];
            sumXx += (double)i * i;
        }
        var denominator = n * sumXx - sumX * sumX;
        if (denominator == 0) return 0;
        return Math.Round((n * sumXy - sumX * sumY) / denominator, 4);
    }

    /// <summary>
    /// Days until the current average crosses the max threshold at the given
    /// slope. Null when not degrading or no upper threshold; capped at 90.
    /// </summary>
    public static double? DaysToThreshold(double average, double? max, double slopePerDay)
    {
        if (max is null || slopePerDay <= 0) return null;
        var headroom = max.Value - average;
        if (headroom <= 0) return 0;
        var days = headroom / slopePerDay;
        return days > 90 ? null : Math.Round(days, 1);
    }

    public static int TrendPenalty(double? daysToThreshold)
        => daysToThreshold switch
        {
            null => 0,
            <= 7 => 20,
            <= 30 => 10,
            _ => 4,
        };

    public static int Score(int alertPenalty, int utilizationPenalty, int trendPenalty)
        => Math.Clamp(100 - alertPenalty - utilizationPenalty - trendPenalty, 0, 100);

    public static string Grade(int score) => score switch
    {
        >= 85 => "GOOD",
        >= 65 => "WATCH",
        >= 40 => "DEGRADED",
        _ => "CRITICAL",
    };
}
