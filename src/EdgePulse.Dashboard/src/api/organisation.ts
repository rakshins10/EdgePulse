import apiClient from './client';
import type { MillDto, AreaDto, DeploymentMode } from '../types/api';

// ── Read ────────────────────────────────────────────────────────────────────

export const getMills = (): Promise<MillDto[]> =>
  apiClient.get<MillDto[]>('/organisation/mills').then(r => r.data);

export const getAreas = (millId?: string): Promise<AreaDto[]> =>
  apiClient
    .get<AreaDto[]>('/organisation/areas', {
      params: millId ? { millId } : undefined,
    })
    .then(r => r.data);

// ── Create ──────────────────────────────────────────────────────────────────

export interface CreateMillBody {
  name: string;
  code: string;
  location: string;
  timezone: string;
  hasInternet: boolean;
  deploymentMode: DeploymentMode;
}

export const createMill = (body: CreateMillBody): Promise<string> =>
  apiClient.post<string>('/organisation/mills', body).then(r => r.data);

export interface CreateAreaBody {
  millId: string;
  name: string;
  code: string;
  locationTypeId?: string;
  description?: string;
}

export const createArea = (body: CreateAreaBody): Promise<string> =>
  apiClient.post<string>('/organisation/areas', body).then(r => r.data);

// ── Update + Delete ─────────────────────────────────────────────────────────

export interface UpdateMillBody {
  name: string;
  location: string;
  timezone: string;
  hasInternet: boolean;
  deploymentMode: DeploymentMode;
}

export const updateMill = (id: string, body: UpdateMillBody): Promise<void> =>
  apiClient.put(`/organisation/mills/${id}`, body).then(() => undefined);

export const deleteMill = (id: string): Promise<void> =>
  apiClient.delete(`/organisation/mills/${id}`).then(() => undefined);

export interface UpdateAreaBody {
  name: string;
  description?: string;
  locationTypeId?: string;
}

export const updateArea = (id: string, body: UpdateAreaBody): Promise<void> =>
  apiClient.put(`/organisation/areas/${id}`, body).then(() => undefined);

export const deleteArea = (id: string): Promise<void> =>
  apiClient.delete(`/organisation/areas/${id}`).then(() => undefined);
