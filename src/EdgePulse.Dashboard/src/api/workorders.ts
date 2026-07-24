import apiClient from './client';
import type { WorkOrderDto } from '../types/api';

const base = '/workorders';

export const getWorkOrders = (params?: {
  status?: string; deviceId?: string; assignedTo?: string;
}): Promise<WorkOrderDto[]> =>
  apiClient.get<WorkOrderDto[]>(base, { params }).then(r => r.data);

export const createWorkOrder = (body: {
  deviceId: string; title: string; description?: string; priority?: string;
  maintenanceTypeId?: string | null; dueDate?: string | null; assignedTo?: string | null;
}): Promise<string> =>
  apiClient.post<string>(base, body).then(r => r.data);

export const transitionWorkOrder = (
  id: string, action: 'start' | 'hold' | 'complete' | 'cancel',
  notes?: string, partsUsed?: string,
): Promise<void> =>
  apiClient.post(`${base}/${id}/transition`, { action, notes, partsUsed }).then(() => undefined);

export const assignWorkOrder = (id: string, assignedTo: string | null): Promise<void> =>
  apiClient.put(`${base}/${id}/assign`, { assignedTo }).then(() => undefined);
