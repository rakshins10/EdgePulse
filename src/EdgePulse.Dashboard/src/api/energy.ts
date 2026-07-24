import apiClient from './client';
import type { EnergyReport } from '../types/api';

const base = '/energy';

export const getEnergyReport = (from: string, to: string): Promise<EnergyReport> =>
  apiClient
    .get<EnergyReport>(`${base}/report`, { params: { from, to } })
    .then(r => r.data);

export const downloadEnergyCsv = async (from: string, to: string): Promise<void> => {
  const res = await apiClient.get(`${base}/report/csv`, {
    params: { from, to }, responseType: 'blob',
  });
  const disposition = (res.headers['content-disposition'] as string | undefined) ?? '';
  const match = /filename="?([^\";]+)"?/.exec(disposition);
  const url = URL.createObjectURL(res.data as Blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = match?.[1] ?? 'esg-energy.csv';
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
};
