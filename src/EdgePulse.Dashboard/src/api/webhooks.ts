import apiClient from './client';
import type { WebhookDto } from '../types/api';

const base = '/webhooks';

export const getWebhooks = (): Promise<WebhookDto[]> =>
  apiClient.get<WebhookDto[]>(base).then(r => r.data);

export const getWebhookEvents = (): Promise<string[]> =>
  apiClient.get<string[]>(`${base}/events`).then(r => r.data);

export const createWebhook = (body: {
  name: string; url: string; secret: string; events: string[]; format?: string;
}): Promise<string> =>
  apiClient.post<string>(base, body).then(r => r.data);

export const updateWebhook = (id: string, body: {
  name: string; url: string; secret?: string | null; events: string[];
  format?: string; isActive: boolean;
}): Promise<void> =>
  apiClient.put(`${base}/${id}`, body).then(() => undefined);

export const deleteWebhook = (id: string): Promise<void> =>
  apiClient.delete(`${base}/${id}`).then(() => undefined);

export const testWebhook = (id: string): Promise<string> =>
  apiClient.post<string>(`${base}/${id}/test`).then(r => r.data);
