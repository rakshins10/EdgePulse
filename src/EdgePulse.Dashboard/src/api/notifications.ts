import apiClient from './client';
import type { NotificationDto } from '../types/api';

const base = '/notifications';

export const getNotifications = (unreadOnly = false, take = 30): Promise<NotificationDto[]> =>
  apiClient
    .get<NotificationDto[]>(base, { params: { unreadOnly, take } })
    .then(r => r.data);

export const getUnreadCount = (): Promise<number> =>
  apiClient.get<number>(`${base}/unread-count`).then(r => r.data);

export const markNotificationRead = (id: string): Promise<void> =>
  apiClient.post(`${base}/${id}/read`).then(() => undefined);

export const markAllNotificationsRead = (): Promise<number> =>
  apiClient.post<number>(`${base}/read-all`).then(r => r.data);
