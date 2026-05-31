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
