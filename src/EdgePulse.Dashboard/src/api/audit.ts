import apiClient from './client';
import type { AuditLogDto } from '../types/api';

const base = '/audit';

export const getAuditLogs = (params?: {
  entityType?: string; action?: string; take?: number;
}): Promise<AuditLogDto[]> =>
  apiClient.get<AuditLogDto[]>(base, { params }).then(r => r.data);

export const downloadAuditCsv = async (): Promise<void> => {
  const res = await apiClient.get(`${base}/csv`, { responseType: 'blob' });
  const url = URL.createObjectURL(res.data as Blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'audit-trail.csv';
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
};
