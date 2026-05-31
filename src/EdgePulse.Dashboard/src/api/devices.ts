import apiClient from './client';
import type { DeviceListDto, RegisterDeviceResult } from '../types/api';

export const getDevices = (params?: {
  millId?: string;
  areaId?: string;
}): Promise<DeviceListDto[]> =>
  apiClient.get<DeviceListDto[]>('/devices', { params }).then(r => r.data);

export interface RegisterDeviceBody {
  areaId: string;
  typeId: string;
  statusId: string;
  name: string;
  code: string;
  manufacturerId?: string;
  modelId?: string;
  serialNumber?: string;
  installDate?: string;
  description?: string;
}

export const registerDevice = (body: RegisterDeviceBody): Promise<RegisterDeviceResult> =>
  apiClient.post<RegisterDeviceResult>('/devices', body).then(r => r.data);

export const decommissionDevice = (id: string): Promise<void> =>
  apiClient.delete(`/devices/${id}`).then(() => undefined);
