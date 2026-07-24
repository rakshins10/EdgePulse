export type DeploymentMode = 'Cloud' | 'OnPremise';

export type UserRole =
  | 'SuperAdmin'
  | 'CustomerAdmin'
  | 'MillManager'
  | 'Operator'
  | 'Executive';

// ── Organisation ────────────────────────────────────────────────────────────

export interface MillDto {
  id: string;
  name: string;
  code: string;
  location: string;
  timezone: string;
  hasInternet: boolean;
  deploymentMode: DeploymentMode;
  tenantId: string;
}

export interface AreaDto {
  id: string;
  millId: string;
  millName: string;
  name: string;
  code: string;
  description: string | null;
  locationTypeName: string | null;
}

// ── Devices ─────────────────────────────────────────────────────────────────

export interface DeviceListDto {
  id: string;
  name: string;
  code: string;
  serialNumber: string | null;
  typeName: string;
  statusName: string;
  statusColor: string | null;
  millId: string;
  millName: string;
  areaId: string;
  areaName: string;
  tenantId: string;
}

export interface RegisterDeviceResult {
  deviceId: string;
  apiKey: string;
}

// ── Configuration / lookup tables ───────────────────────────────────────────

export interface DeviceTypeDto {
  id: string;
  name: string;
  code: string;
  description: string | null;
  icon: string | null;
  sortOrder: number;
}

export interface DeviceStatusDto {
  id: string;
  name: string;
  code: string;
  description: string | null;
  color: string | null;
  sortOrder: number;
}

export interface LocationTypeDto {
  id: string;
  name: string;
  code: string;
  description: string | null;
  sortOrder: number;
}

export interface MaintenanceTypeDto {
  id: string;
  name: string;
  code: string;
  description: string | null;
  color: string | null;
  sortOrder: number;
}

export interface MetricTypeDto {
  id: string;
  name: string;
  code: string;
  defaultUnit: string;
  description: string | null;
  sortOrder: number;
}

// ── Current user (parsed from JWT) ──────────────────────────────────────────

export interface CurrentUser {
  userId: string;
  email: string;
  fullName: string;
  tenantId: string;
  role: UserRole;
  millId: string | null;
  areaIds: string[];
}

// ── Telemetry ───────────────────────────────────────────────────────────────

export interface MetricDto {
  key: string;
  value: number;
  unit?: string | null;
}

export interface TelemetryReadingDto {
  timestamp: string;
  metrics: MetricDto[];
}

export interface TelemetryResponse {
  deviceId: string;
  count: number;
  readings: TelemetryReadingDto[];
}

// ── Notifications ───────────────────────────────────────────────────────────

export interface NotificationDto {
  id: string;
  type: string;                       // "ALERT" | future types
  severityCode: string | null;        // CRITICAL / HIGH / MEDIUM / LOW
  title: string;
  message: string;
  linkEntityType: string | null;      // e.g. "Alert"
  linkEntityId: string | null;
  isRead: boolean;
  createdAt: string;
}

// ── Attachments ─────────────────────────────────────────────────────────────

export interface AttachmentDto {
  id: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  fileCategory: string;
  uploadedBy: string;
  uploadedAt: string;
}

// ── Reports ─────────────────────────────────────────────────────────────────

export interface MillReportRow {
  millId: string;
  millName: string;
  location: string;
  deviceCount: number;
  totalAlerts: number;
  openAlerts: number;
  criticalAlerts: number;
  highAlerts: number;
  avgAcknowledgeMinutes: number | null;
  avgResolveMinutes: number | null;
}

export interface MillComparisonReport {
  from: string;
  to: string;
  generatedAt: string;
  mills: MillReportRow[];
}
