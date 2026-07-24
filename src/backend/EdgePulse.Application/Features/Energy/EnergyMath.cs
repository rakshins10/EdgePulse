namespace EdgePulse.Application.Features.Energy;

/// <summary>
/// Pure energy/ESG arithmetic, kept free of I/O so it is trivially testable.
///
/// Telemetry gives instantaneous power samples (kW). For a bucket of samples
/// we approximate energy as: average power × observed duration, where the
/// observed duration is the span between the first and last sample in the
/// bucket. Devices that report a single sample in a bucket contribute no
/// measurable duration and therefore no energy.
/// </summary>
public static class EnergyMath
{
    /// <summary>Energy in kWh for one bucket of power samples.</summary>
    public static double EnergyKwh(
        double averagePowerKw, DateTime firstSample, DateTime lastSample)
    {
        var hours = (lastSample - firstSample).TotalHours;
        if (hours <= 0 || averagePowerKw <= 0) return 0;
        return Math.Round(averagePowerKw * hours, 2);
    }

    /// <summary>CO₂-equivalent mass for an energy amount at a grid intensity.</summary>
    public static double Co2Kg(double energyKwh, double factorKgPerKwh)
        => Math.Round(energyKwh * factorKgPerKwh, 1);
}
