import apiClient from './client';
import type { FloorPlanDeviceDto } from '../types/api';

export const getFloorPlan = (millId: string): Promise<FloorPlanDeviceDto[]> =>
  apiClient.get<FloorPlanDeviceDto[]>(`/floorplan/${millId}`).then(r => r.data);

export const setDevicePosition = (
  deviceId: string, x: number | null, y: number | null,
): Promise<void> =>
  apiClient
    .put(`/floorplan/devices/${deviceId}/position`, { x, y })
    .then(() => undefined);
