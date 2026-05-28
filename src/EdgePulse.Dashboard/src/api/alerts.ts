import apiClient from './client';
import type {
  AlertCountDto,
  AlertListResult,
  AlertThresholdDto,
  GetAlertsParams,
} from '../types/alerts';

// ─── Alert Queries ────────────────────────────────────────────────────────────

export async function fetchAlerts(
  params: GetAlertsParams = {}
): Promise<AlertListResult> {
  const { data } = await apiClient.get<AlertListResult>('/alerts', { params });
  return data;
}

export async function fetchAlertCount(): Promise<AlertCountDto> {
  const { data } = await apiClient.get<AlertCountDto>('/alerts/count');
  return data;
}

export async function fetchAlertThresholds(
  deviceId?: string,
  includeInactive = false
): Promise<AlertThresholdDto[]> {
  const { data } = await apiClient.get<AlertThresholdDto[]>(
    '/alerts/thresholds',
    { params: { deviceId, includeInactive } }
  );
  return data;
}

// ─── Alert Actions ────────────────────────────────────────────────────────────

export async function acknowledgeAlert(
  id: string,
  notes?: string
): Promise<void> {
  await apiClient.post(`/alerts/${id}/acknowledge`, { notes });
}

export async function resolveAlert(
  id: string,
  notes?: string
): Promise<void> {
  await apiClient.post(`/alerts/${id}/resolve`, { notes });
}
