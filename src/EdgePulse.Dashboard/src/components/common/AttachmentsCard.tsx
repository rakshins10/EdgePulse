import { useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getAttachments, uploadAttachment, downloadAttachment, deleteAttachment,
} from '../../api/attachments';
import type { AttachmentDto } from '../../types/api';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { useConfirm } from '../../context/ConfirmContext';
import { useToast } from '../../context/ToastContext';
import LoadingSpinner from './LoadingSpinner';
import styles from './AttachmentsCard.module.css';

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

interface Props {
  entityType: 'Device' | 'Mill' | 'Area';
  entityId: string;
}

/**
 * Attachments panel for a Device / Mill / Area detail view.
 * Upload + delete are hidden for Operator and Executive roles (US-018).
 */
export default function AttachmentsCard({ entityType, entityId }: Props) {
  const { t, i18n } = useTranslation();
  const qc = useQueryClient();
  const confirm = useConfirm();
  const toast = useToast();
  const user = useCurrentUser();
  const fileRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);

  const canManage = user
    ? user.role !== 'Operator' && user.role !== 'Executive'
    : false;

  const queryKey = ['attachments', entityType, entityId];
  const { data = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getAttachments(entityType, entityId),
  });

  async function handleFileChosen(file: File | null) {
    if (!file) return;
    setUploading(true);
    try {
      await uploadAttachment(entityType, entityId, file);
      await qc.invalidateQueries({ queryKey });
      toast.success(t('attachments.uploaded', { name: file.name }));
    } catch {
      toast.error(t('attachments.uploadError'));
    } finally {
      setUploading(false);
      if (fileRef.current) fileRef.current.value = '';
    }
  }

  async function handleDelete(row: AttachmentDto) {
    const ok = await confirm({
      message: t('attachments.deleteConfirm', { name: row.fileName }),
      variant: 'danger',
      confirmLabel: t('common.delete'),
    });
    if (!ok) return;
    try {
      await deleteAttachment(row.id);
      await qc.invalidateQueries({ queryKey });
      toast.success(t('common.deleted', { name: row.fileName }));
    } catch {
      toast.error(t('attachments.deleteError'));
    }
  }

  async function handleDownload(row: AttachmentDto) {
    try {
      await downloadAttachment(row.id, row.fileName);
    } catch {
      toast.error(t('attachments.downloadError'));
    }
  }

  return (
    <section className={styles.card}>
      <div className={styles.header}>
        <h2 className={styles.title}>
          {t('attachments.title')}
          <span className={styles.count}>
            {t('attachments.count', { count: data.length })}
          </span>
        </h2>
        {canManage && (
          <>
            <button
              className={styles.uploadBtn}
              disabled={uploading}
              onClick={() => fileRef.current?.click()}
            >
              {uploading ? t('attachments.uploading') : t('attachments.uploadBtn')}
            </button>
            <input
              ref={fileRef}
              type="file"
              className={styles.hiddenInput}
              accept=".pdf,.png,.jpg,.jpeg,.gif,.webp,.xlsx,.xls,.csv,.docx,.doc,.txt,.md,.dwg,.dxf,.zip"
              onChange={e => handleFileChosen(e.target.files?.[0] ?? null)}
            />
          </>
        )}
      </div>

      {isLoading ? (
        <LoadingSpinner message={t('common.loading')} />
      ) : data.length === 0 ? (
        <div className={styles.empty}>{t('attachments.empty')}</div>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>{t('attachments.file')}</th>
              <th>{t('attachments.category')}</th>
              <th>{t('attachments.size')}</th>
              <th>{t('attachments.uploadedBy')}</th>
              <th>{t('attachments.uploadedAt')}</th>
              {canManage && <th />}
            </tr>
          </thead>
          <tbody>
            {data.map(row => (
              <tr key={row.id}>
                <td>
                  <button className={styles.fileBtn} onClick={() => handleDownload(row)}>
                    {row.fileName}
                  </button>
                </td>
                <td><span className={styles.category}>{row.fileCategory}</span></td>
                <td className={styles.muted}>{formatSize(row.fileSize)}</td>
                <td className={styles.muted}>{row.uploadedBy}</td>
                <td className={styles.muted}>
                  {new Date(row.uploadedAt + (row.uploadedAt.endsWith('Z') ? '' : 'Z'))
                    .toLocaleString(i18n.language)}
                </td>
                {canManage && (
                  <td>
                    <button className={styles.deleteBtn} onClick={() => handleDelete(row)}>
                      {t('common.delete')}
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <p className={styles.hint}>{t('attachments.hint')}</p>
    </section>
  );
}
