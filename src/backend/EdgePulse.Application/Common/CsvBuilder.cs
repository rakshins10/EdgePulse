using System.Text;

namespace EdgePulse.Application.Common;

/// <summary>
/// Minimal RFC-4180 CSV writer: quotes fields containing commas, quotes or
/// newlines and doubles embedded quotes. UTF-8 output with a BOM so Excel
/// opens files correctly.
/// </summary>
public static class CsvBuilder
{
    public static string Build(
        IEnumerable<string> header,
        IEnumerable<IEnumerable<object?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', header.Select(Escape)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(',',
                row.Select(v => Escape(Format(v)))));
        return sb.ToString();
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
        double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string Escape(string value)
        => value.Contains(',') || value.Contains('"') ||
           value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
