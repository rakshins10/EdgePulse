import apiClient from './client';
import type { MillDto, AreaDto } from '../types/api';

export const getMills = (): Promise<MillDto[]> =>
  apiClient.get<MillDto[]>('/organisation/mills').then(r => r.data);

export const getAreas = (millId?: string): Promise<AreaDto[]> =>
  apiClient
    .get<AreaDto[]>('/organisation/areas', {
      params: millId ? { millId } : undefined,
    })
    .then(r => r.data);
