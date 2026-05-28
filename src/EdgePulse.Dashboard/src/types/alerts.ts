export type SeverityCode = 'CRITICAL' | 'HIGH' | 'MEDIUM' | 'LOW';
export type StatusCode = 'OPEN' | 'ACKNOWLEDGED' | 'RESOLVED' | 'CLOSED';

export interface AlertDto {
  id: string;
  deviceId: string;
  deviceName: string;
  deviceCode: string;
  millId: string;
  millName: string;
  metricKey: string;
  triggerValue: number;
  thresholdValue: number;
  unit: string | null;
  severityCode: SeverityCode;
  statusCode: StatusCode;
  aiSummary: string | null;
  triggeredAt: string;      // ISO 8601
  acknowledgedAt: string | null;
  acknowledgedBy: string | null;
  resolvedAt: string | null;
  resolvedBy: string | null;
  notes: string | null;
}

export interface AlertListResult {
  items: AlertDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AlertCountDto {
  openCount: number;
  criticalOpenCount: number;
}

export interface AlertThresholdDto {
  id: string;
  deviceId: string;
  deviceName: string;
  deviceCode: string;
  metricKey: string;
  name: string;
  minValue: number | null;
  maxValue: number | null;
  unit: string | null;
  severityCode: SeverityCode;
  consecutiveCount: number;
  isActive: boolean;
  description: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface GetAlertsParams {
  millId?: string;
  deviceId?: string;
  severityCode?: SeverityCode;
  statusCode?: StatusCode;
  page?: number;
  pageSize?: number;
}
