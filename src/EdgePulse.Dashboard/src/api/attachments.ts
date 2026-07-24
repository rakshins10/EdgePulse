import apiClient from './client';
import type { AttachmentDto } from '../types/api';

const base = '/attachments';

export const getAttachments = (
  entityType: string, entityId: string,
): Promise<AttachmentDto[]> =>
  apiClient
    .get<AttachmentDto[]>(base, { params: { entityType, entityId } })
    .then(r => r.data);

export const uploadAttachment = (
  entityType: string, entityId: string, file: File, category?: string,
): Promise<AttachmentDto> => {
  const form = new FormData();
  form.append('entityType', entityType);
  form.append('entityId', entityId);
  if (category) form.append('category', category);
  form.append('file', file);
  return apiClient
    .post<AttachmentDto>(base, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    .then(r => r.data);
};

/** Fetches the file with auth and triggers a browser download. */
export const downloadAttachment = async (
  id: string, fileName: string,
): Promise<void> => {
  const res = await apiClient.get(`${base}/${id}/download`, {
    responseType: 'blob',
  });
  const url = URL.createObjectURL(res.data as Blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
};

export const deleteAttachment = (id: string): Promise<void> =>
  apiClient.delete(`${base}/${id}`).then(() => undefined);
