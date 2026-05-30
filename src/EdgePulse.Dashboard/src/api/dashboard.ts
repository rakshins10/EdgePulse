import apiClient from './client';
import type { DashboardSummaryDto } from '../types/dashboard';

export async function fetchDashboardSummary(): Promise<DashboardSummaryDto> {
  const { data } = await apiClient.get<DashboardSummaryDto>('/dashboard/summary');
  return data;
}
