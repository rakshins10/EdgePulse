import apiClient from './client';
import type {
  DeviceTypeDto, DeviceStatusDto,
  LocationTypeDto, MetricTypeDto, MaintenanceTypeDto,
} from '../types/api';

const base = '/configuration';

// ── Device Types ────────────────────────────────────────────────────────────
export const getDeviceTypes = (): Promise<DeviceTypeDto[]> =>
  apiClient.get<DeviceTypeDto[]>(`${base}/device-types`).then(r => r.data);

export const createDeviceType = (body: {
  name: string; code: string; description?: string; icon?: string; sortOrder?: number;
}): Promise<string> =>
  apiClient.post<string>(`${base}/device-types`, body).then(r => r.data);

export const updateDeviceType = (id: string, body: {
  name: string; description?: string; icon?: string; sortOrder: number;
}): Promise<void> =>
  apiClient.put(`${base}/device-types/${id}`, body).then(() => undefined);

export const deleteDeviceType = (id: string): Promise<void> =>
  apiClient.delete(`${base}/device-types/${id}`).then(() => undefined);

// ── Device Statuses ─────────────────────────────────────────────────────────
export const getDeviceStatuses = (): Promise<DeviceStatusDto[]> =>
  apiClient.get<DeviceStatusDto[]>(`${base}/device-statuses`).then(r => r.data);

export const createDeviceStatus = (body: {
  name: string; code: string; description?: string; color?: string; sortOrder?: number;
}): Promise<string> =>
  apiClient.post<string>(`${base}/device-statuses`, body).then(r => r.data);

export const updateDeviceStatus = (id: string, body: {
  name: string; description?: string; color?: string; sortOrder: number;
}): Promise<void> =>
  apiClient.put(`${base}/device-statuses/${id}`, body).then(() => undefined);

export const deleteDeviceStatus = (id: string): Promise<void> =>
  apiClient.delete(`${base}/device-statuses/${id}`).then(() => undefined);

// ── Location Types ──────────────────────────────────────────────────────────
export const getLocationTypes = (): Promise<LocationTypeDto[]> =>
  apiClient.get<LocationTypeDto[]>(`${base}/location-types`).then(r => r.data);

export const createLocationType = (body: {
  name: string; code: string; description?: string; sortOrder?: number;
}): Promise<string> =>
  apiClient.post<string>(`${base}/location-types`, body).then(r => r.data);

export const updateLocationType = (id: string, body: {
  name: string; description?: string; sortOrder: number;
}): Promise<void> =>
  apiClient.put(`${base}/location-types/${id}`, body).then(() => undefined);

export const deleteLocationType = (id: string): Promise<void> =>
  apiClient.delete(`${base}/location-types/${id}`).then(() => undefined);

// ── Metric Types ────────────────────────────────────────────────────────────
export const getMetricTypes = (): Promise<MetricTypeDto[]> =>
  apiClient.get<MetricTypeDto[]>(`${base}/metric-types`).then(r => r.data);

export const createMetricType = (body: {
  name: string; code: string; defaultUnit: string; description?: string; sortOrder?: number;
}): Promise<string> =>
  apiClient.post<string>(`${base}/metric-types`, body).then(r => r.data);

export const updateMetricType = (id: string, body: {
  name: string; defaultUnit: string; description?: string; sortOrder: number;
}): Promise<void> =>
  apiClient.put(`${base}/metric-types/${id}`, body).then(() => undefined);

export const deleteMetricType = (id: string): Promise<void> =>
  apiClient.delete(`${base}/metric-types/${id}`).then(() => undefined);

// ── Maintenance Types ───────────────────────────────────────────────────────
export const getMaintenanceTypes = (): Promise<MaintenanceTypeDto[]> =>
  apiClient.get<MaintenanceTypeDto[]>(`${base}/maintenance-types`).then(r => r.data);

export const createMaintenanceType = (body: {
  name: string; code: string; description?: string; color?: string; sortOrder?: number;
}): Promise<string> =>
  apiClient.post<string>(`${base}/maintenance-types`, body).then(r => r.data);

export const updateMaintenanceType = (id: string, body: {
  name: string; description?: string; color?: string; sortOrder: number;
}): Promise<void> =>
  apiClient.put(`${base}/maintenance-types/${id}`, body).then(() => undefined);

export const deleteMaintenanceType = (id: string): Promise<void> =>
  apiClient.delete(`${base}/maintenance-types/${id}`).then(() => undefined);
