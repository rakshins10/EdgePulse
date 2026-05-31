import apiClient from './client';
import type { DeviceListDto } from '../types/api';

export const getDevices = (params?: {
  millId?: string;
  areaId?: string;
}): Promise<DeviceListDto[]> =>
  apiClient.get<DeviceListDto[]>('/devices', { params }).then(r => r.data);
