import apiClient from './client';
import type { IdentityUserDto } from '../types/api';

const base = '/users';

export const getUsers = (): Promise<IdentityUserDto[]> =>
  apiClient.get<IdentityUserDto[]>(base).then(r => r.data);

export const createUser = (body: {
  email: string; firstName: string; lastName: string; role: string;
  millId?: string | null; areaIds?: string[]; temporaryPassword: string;
}): Promise<string> =>
  apiClient.post<string>(base, body).then(r => r.data);

export const updateUserRole = (id: string, body: {
  role: string; millId?: string | null; areaIds?: string[];
}): Promise<void> =>
  apiClient.put(`${base}/${id}/role`, body).then(() => undefined);

export const setUserEnabled = (id: string, enabled: boolean): Promise<void> =>
  apiClient.put(`${base}/${id}/enabled`, { enabled }).then(() => undefined);

export const resetUserPassword = (id: string, temporaryPassword: string): Promise<void> =>
  apiClient.post(`${base}/${id}/reset-password`, { temporaryPassword }).then(() => undefined);
