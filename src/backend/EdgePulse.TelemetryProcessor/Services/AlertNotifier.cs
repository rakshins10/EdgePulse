using MailKit.Net.Smtp;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Collections.Concurrent;

namespace EdgePulse.TelemetryProcessor.Services;

/// <summary>
/// SMTP settings bound from the "Smtp" configuration section.
/// Local dev uses the MailHog container (host localhost, port 1025,
/// no auth, no SSL — browse mail at http://localhost:8025).
/// </summary>
public class SmtpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; }
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "alerts@edgepulse.local";
    public string[] Recipients { get; set; } = [];
}

/// <summary>
/// Bound from "WorkOrders" config. When enabled, alerts of the listed
/// severities automatically open a maintenance work order.
/// </summary>
public class WorkOrderOptions
{
    public bool AutoCreateFromAlerts { get; set; } = true;
    public string[] AutoCreateSeverities { get; set; } = ["CRITICAL", "HIGH"];
}

/// <summary>
/// Fires the two delivery channels when an alert is created:
///   1. In-app — inserts a row into the Notifications table (the dashboard
///      bell polls it). Raw SQL, consistent with the rest of this worker.
///   2. Email  — sends a summary mail via SMTP when Smtp:Enabled.
/// Both channels are best-effort: failures are logged and never prevent
/// the alert itself from being recorded.
/// </summary>
public class AlertNotifier
{
    private readonly string _sqlConnectionString;
    private readonly SmtpOptions _smtp;
    private readonly WorkOrderOptions _workOrders;
    private readonly ILogger<AlertNotifier> _logger;

    // DeviceId -> "Name (CODE)" — devices rarely rename; cache for friendliness
    private readonly ConcurrentDictionary<Guid, string> _deviceLabels = new();

    public AlertNotifier(
        string sqlConnectionString,
        SmtpOptions smtp,
        WorkOrderOptions workOrders,
        ILogger<AlertNotifier> logger)
    {
        _sqlConnectionString = sqlConnectionString;
        _smtp = smtp;
        _workOrders = workOrders;
        _logger = logger;
    }

    public async Task NotifyAlertCreatedAsync(
        Guid alertId,
        Guid tenantId,
        Guid deviceId,
        string metricKey,
        double triggerValue,
        double thresholdValue,
        string? unit,
        string severityCode,
        CancellationToken ct = default)
    {
        var deviceLabel = await GetDeviceLabelAsync(deviceId, ct);
        var unitSuffix = string.IsNullOrEmpty(unit) ? "" : $" {unit}";

        var title = $"[{severityCode}] {deviceLabel}: {metricKey} alert";
        var message =
            $"{metricKey} reached {triggerValue}{unitSuffix} " +
            $"(threshold {thresholdValue}{unitSuffix}) on {deviceLabel}.";

        await InsertInAppNotificationAsync(
            tenantId, severityCode, title, message, alertId, ct);

        if (_smtp.Enabled && _smtp.Recipients.Length > 0)
            await SendEmailAsync(title, message, severityCode, deviceLabel, ct);

        if (_workOrders.AutoCreateFromAlerts &&
            _workOrders.AutoCreateSeverities.Contains(severityCode))
            await AutoCreateWorkOrderAsync(
                alertId, tenantId, deviceId, deviceLabel, metricKey, severityCode, ct);
    }

    /// <summary>
    /// Opens a maintenance work order for a fired alert (severity-gated).
    /// One per alert — skipped if this alert already has a work order.
    /// </summary>
    private async Task AutoCreateWorkOrderAsync(
        Guid alertId, Guid tenantId, Guid deviceId, string deviceLabel,
        string metricKey, string severityCode, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_sqlConnectionString);
            await conn.OpenAsync(ct);

            // Dedup: one work order per alert
            const string checkSql =
                "SELECT COUNT(1) FROM WorkOrders WHERE AlertId = @AlertId AND IsDeleted = 0";
            await using (var check = new SqlCommand(checkSql, conn))
            {
                check.Parameters.AddWithValue("@AlertId", alertId);
                if ((int)(await check.ExecuteScalarAsync(ct) ?? 0) > 0) return;
            }

            var workOrderId = Guid.NewGuid();
            var number = $"WO-{workOrderId.ToString("N")[..8].ToUpperInvariant()}";
            var millId = await GetDeviceMillIdAsync(conn, deviceId, ct);
            var now = DateTime.UtcNow;
            var title = $"Investigate {metricKey} alert on {deviceLabel}";

            const string sql = """
                INSERT INTO WorkOrders (
                    Id, TenantId, Number, Title, Description,
                    DeviceId, MillId, AlertId, MaintenanceTypeId,
                    Priority, Status, AssignedTo, DueDate, PartsUsed,
                    CreatedBy, CompletedAt, CompletedBy, CompletionNotes,
                    CreatedAt, UpdatedAt, IsDeleted, DeletedAt
                ) VALUES (
                    @Id, @TenantId, @Number, @Title, @Description,
                    @DeviceId, @MillId, @AlertId, NULL,
                    @Priority, 'OPEN', NULL, NULL, NULL,
                    'alert-engine', NULL, NULL, NULL,
                    @Now, @Now, 0, NULL
                )
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", workOrderId);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            cmd.Parameters.AddWithValue("@Number", number);
            cmd.Parameters.AddWithValue("@Title", Truncate(title, 200));
            cmd.Parameters.AddWithValue("@Description",
                $"Auto-created from a {severityCode} alert. " +
                "Review the alert, inspect the device and record the resolution here.");
            cmd.Parameters.AddWithValue("@DeviceId", deviceId);
            cmd.Parameters.AddWithValue("@MillId", millId);
            cmd.Parameters.AddWithValue("@AlertId", alertId);
            cmd.Parameters.AddWithValue("@Priority", severityCode);
            cmd.Parameters.AddWithValue("@Now", now);
            await cmd.ExecuteNonQueryAsync(ct);

            // Companion in-app notification
            const string notifSql = """
                INSERT INTO Notifications (
                    Id, TenantId, Type, SeverityCode, Title, Message,
                    LinkEntityType, LinkEntityId, IsRead, ReadAt,
                    CreatedAt, UpdatedAt, IsDeleted, DeletedAt
                ) VALUES (
                    @Id, @TenantId, 'WORKORDER', @Severity, @Title, @Message,
                    'WorkOrder', @WorkOrderId, 0, NULL,
                    @Now, @Now, 0, NULL
                )
                """;
            await using var notif = new SqlCommand(notifSql, conn);
            notif.Parameters.AddWithValue("@Id", Guid.NewGuid());
            notif.Parameters.AddWithValue("@TenantId", tenantId);
            notif.Parameters.AddWithValue("@Severity", severityCode);
            notif.Parameters.AddWithValue("@Title", $"Work order {number} opened");
            notif.Parameters.AddWithValue("@Message",
                Truncate($"{title}. Assign a technician on the Work Orders page.", 1000));
            notif.Parameters.AddWithValue("@WorkOrderId", workOrderId);
            notif.Parameters.AddWithValue("@Now", now);
            await notif.ExecuteNonQueryAsync(ct);

            _logger.LogInformation(
                "Auto-created work order {Number} for alert {AlertId} ({Severity})",
                number, alertId, severityCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to auto-create work order for alert {AlertId}", alertId);
        }
    }

    private static async Task<Guid> GetDeviceMillIdAsync(
        SqlConnection conn, Guid deviceId, CancellationToken ct)
    {
        const string sql = "SELECT MillId FROM Devices WHERE Id = @Id";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", deviceId);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is Guid g ? g : Guid.Empty;
    }

    private async Task InsertInAppNotificationAsync(
        Guid tenantId, string severityCode, string title, string message,
        Guid alertId, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_sqlConnectionString);
            await conn.OpenAsync(ct);

            const string sql = """
                INSERT INTO Notifications (
                    Id, TenantId, Type, SeverityCode, Title, Message,
                    LinkEntityType, LinkEntityId,
                    IsRead, ReadAt,
                    CreatedAt, UpdatedAt, IsDeleted, DeletedAt
                ) VALUES (
                    @Id, @TenantId, 'ALERT', @SeverityCode, @Title, @Message,
                    'Alert', @AlertId,
                    0, NULL,
                    @Now, @Now, 0, NULL
                )
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            cmd.Parameters.AddWithValue("@SeverityCode", severityCode);
            cmd.Parameters.AddWithValue("@Title", Truncate(title, 200));
            cmd.Parameters.AddWithValue("@Message", Truncate(message, 1000));
            cmd.Parameters.AddWithValue("@AlertId", alertId);
            cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to insert in-app notification for alert {AlertId}", alertId);
        }
    }

    private async Task SendEmailAsync(
        string title, string message, string severityCode, string deviceLabel,
        CancellationToken ct)
    {
        try
        {
            var mail = new MimeMessage();
            mail.From.Add(MailboxAddress.Parse(_smtp.From));
            foreach (var recipient in _smtp.Recipients)
                mail.To.Add(MailboxAddress.Parse(recipient));
            mail.Subject = $"EdgePulse {title}";
            mail.Body = new BodyBuilder
            {
                TextBody =
                    $"""
                    EdgePulse alert notification

                    Severity : {severityCode}
                    Device   : {deviceLabel}
                    Detail   : {message}
                    Time     : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

                    Open the dashboard to acknowledge or resolve this alert.
                    """
            }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, _smtp.UseSsl, ct);
            if (!string.IsNullOrEmpty(_smtp.User))
                await client.AuthenticateAsync(_smtp.User, _smtp.Password, ct);
            await client.SendAsync(mail, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation(
                "Alert email sent to {Count} recipient(s): {Title}",
                _smtp.Recipients.Length, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert email: {Title}", title);
        }
    }

    private async Task<string> GetDeviceLabelAsync(Guid deviceId, CancellationToken ct)
    {
        if (_deviceLabels.TryGetValue(deviceId, out var cached))
            return cached;

        try
        {
            await using var conn = new SqlConnection(_sqlConnectionString);
            await conn.OpenAsync(ct);

            const string sql = "SELECT Name, Code FROM Devices WHERE Id = @Id";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", deviceId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                var label = $"{reader.GetString(0)} ({reader.GetString(1)})";
                _deviceLabels[deviceId] = label;
                return label;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not resolve device label for {DeviceId}", deviceId);
        }

        return deviceId.ToString();
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
