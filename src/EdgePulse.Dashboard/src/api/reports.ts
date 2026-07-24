import apiClient from './client';
import type { MillComparisonReport } from '../types/api';

const base = '/reports';

export const getMillComparison = (
  from: string, to: string,
): Promise<MillComparisonReport> =>
  apiClient
    .get<MillComparisonReport>(`${base}/mill-comparison`, { params: { from, to } })
    .then(r => r.data);

async function downloadCsv(url: string, params: Record<string, string>, fallbackName: string) {
  const res = await apiClient.get(url, { params, responseType: 'blob' });
  const disposition = (res.headers['content-disposition'] as string | undefined) ?? '';
  const match = /filename="?([^\";]+)"?/.exec(disposition);
  const objectUrl = URL.createObjectURL(res.data as Blob);
  const a = document.createElement('a');
  a.href = objectUrl;
  a.download = match?.[1] ?? fallbackName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(objectUrl);
}

export const downloadMillComparisonCsv = (from: string, to: string): Promise<void> =>
  downloadCsv(`${base}/mill-comparison/csv`, { from, to }, 'mill-comparison.csv');

export const downloadAlertsCsv = (from: string, to: string): Promise<void> =>
  downloadCsv(`${base}/alerts/csv`, { from, to }, 'alerts.csv');
