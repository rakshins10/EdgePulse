import apiClient from './client';
import type { TelemetryResponse } from '../types/api';

export interface TelemetryQueryParams {
  from?: string;
  to?: string;
  limit?: number;
}

export async function getDeviceTelemetry(
  deviceId: string,
  params: TelemetryQueryParams = {},
): Promise<TelemetryResponse> {
  const res = await apiClient.get<TelemetryResponse>(
    `/telemetry/devices/${deviceId}`,
    { params },
  );
  return res.data;
}
