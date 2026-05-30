// ── Dashboard Summary ─────────────────────────────────────────────────────────
// Mirrors EdgePulse.Application.Features.Dashboard.Queries.DashboardSummaryDto

export interface AlertTrendDay {
  /** ISO date string, e.g. "2026-05-29" */
  date: string;
  count: number;
}

export interface SeverityCount {
  /** "CRITICAL" | "HIGH" | "MEDIUM" | "LOW" */
  severityCode: string;
  count: number;
}

export interface TopDevice {
  deviceId: string;
  deviceCode: string;
  deviceName: string;
  millName: string;
  alertCount: number;
}

export interface DashboardSummaryDto {
  totalDevices: number;
  openAlerts: number;
  criticalOpenAlerts: number;
  devicesWithAlerts: number;
  alertTrend: AlertTrendDay[];
  bySeverity: SeverityCount[];
  topDevices: TopDevice[];
}
