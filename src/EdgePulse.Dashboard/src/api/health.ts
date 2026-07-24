import apiClient from './client';
import type { DeviceHealthDto } from '../types/api';

export const getDeviceHealth = (): Promise<DeviceHealthDto[]> =>
  apiClient.get<DeviceHealthDto[]>('/healthscore/devices').then(r => r.data);
