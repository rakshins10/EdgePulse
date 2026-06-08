import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getDeviceStatuses, createDeviceStatus, updateDeviceStatus, deleteDeviceStatus,
} from '../../../api/configuration';
import type { DeviceStatusDto } from '../../../types/api';
import LoadingSpinner from '../../../components/common/LoadingSpinner';
import Modal from '../../../components/common/Modal';
import styles from './LookupTable.module.css';
import f from '../../../components/common/FormField.module.css';

export default function DeviceStatusesTab() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const { data = [], isLoading } = useQuery({ queryKey: ['device-statuses'], queryFn: getDeviceStatuses });

  const [open, setOpen]       = useState(false);
  const [editing, setEditing] = useState<DeviceStatusDto | null>(null);
  const [name, setName]       = useState('');
  const [code, setCode]       = useState('');
  const [desc, setDesc]       = useState('');
  const [color, setColor]     = useState('#22c55e');
  const [sortOrder, setSortOrder] = useState(0);
  const [saving, setSaving]   = useState(false);
  const [error, setError]     = useState<string | null>(null);

  function openAdd() {
    setEditing(null);
    setName(''); setCode(''); setDesc(''); setColor('#22c55e'); setSortOrder((data.length + 1) * 10);
    setError(null); setOpen(true);
  }

  function openEdit(row: DeviceStatusDto) {
    setEditing(row);
    setName(row.name); setCode(row.code); setDesc(row.description ?? '');
    setColor(row.color ?? '#22c55e'); setSortOrder(row.sortOrder);
    setError(null); setOpen(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      if (editing) {
        await updateDeviceStatus(editing.id, { name, description: desc || undefined, color, sortOrder });
      } else {
        await createDeviceStatus({ name, code, description: desc || undefined, color, sortOrder });
      }
      await qc.invalidateQueries({ queryKey: ['device-statuses'] });
      setOpen(false);
    } catch {
      setError(editing ? t('configuration.lookup.errorUpdate') : t('configuration.lookup.errorCreate'));
    } finally { setSaving(false); }
  }

  async function handleDelete(row: DeviceStatusDto) {
    if (!confirm(t('configuration.deviceStatuses.deleteConfirm', { name: row.name }))) return;
    try {
      await deleteDeviceStatus(row.id);
      await qc.invalidateQueries({ queryKey: ['device-statuses'] });
    } catch { alert(t('configuration.lookup.errorDelete')); }
  }

  if (isLoading) return <LoadingSpinner message={t('common.loading')} />;

  return (
    <>
      <div className={styles.toolbar}>
        <span className={styles.count}>{t('configuration.deviceStatuses.count', { count: data.length })}</span>
        <button className={styles.addBtn} onClick={openAdd}>{t('configuration.deviceStatuses.addBtn')}</button>
      </div>

      {data.length === 0 ? (
        <div className={styles.empty}>{t('configuration.lookup.empty')}</div>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>{t('configuration.lookup.name')}</th>
              <th>{t('configuration.lookup.code')}</th>
              <th>{t('configuration.lookup.color')}</th>
              <th>{t('configuration.lookup.description')}</th>
              <th>{t('configuration.lookup.sort')}</th>
              <th className={styles.actionsCol} />
            </tr>
          </thead>
          <tbody>
            {data.slice().sort((a, b) => a.sortOrder - b.sortOrder).map(row => (
              <tr key={row.id}>
                <td>
                  {row.color && <span className={styles.colorSwatch} style={{ background: row.color }} />}
                  {row.name}
                </td>
                <td className={styles.code}>{row.code}</td>
                <td className={styles.muted}>{row.color ?? '—'}</td>
                <td className={styles.muted}>{row.description ?? '—'}</td>
                <td className={styles.muted}>{row.sortOrder}</td>
                <td className={styles.actionsCol}>
                  <button className={styles.editBtn} onClick={() => openEdit(row)}>{t('common.edit')}</button>
                  <button className={styles.deleteBtn} onClick={() => handleDelete(row)}>{t('common.delete')}</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <Modal
        open={open}
        title={editing ? t('configuration.deviceStatuses.editTitle') : t('configuration.deviceStatuses.addTitle')}
        onClose={() => setOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setOpen(false)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="ds-form" disabled={saving}>
              {saving ? t('common.saving') : editing ? t('common.save') : t('common.create')}
            </button>
          </>
        }
      >
        <form id="ds-form" className={f.formGrid} onSubmit={handleSubmit}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('configuration.lookup.name')}</label>
              <input className={f.input} required value={name}
                onChange={e => setName(e.target.value)} placeholder={t('configuration.deviceStatuses.namePlaceholder')} />
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('configuration.lookup.code')}</label>
              <input className={f.input} required value={code} disabled={!!editing}
                onChange={e => setCode(e.target.value.toUpperCase())} placeholder={t('configuration.deviceStatuses.codePlaceholder')} />
            </div>
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={f.label}>{t('configuration.lookup.color')}</label>
              <input className={f.input} type="color" value={color}
                onChange={e => setColor(e.target.value)} style={{ padding: 2, height: 36 }} />
            </div>
            <div className={f.field}>
              <label className={f.label}>{t('configuration.lookup.sortOrder')}</label>
              <input className={f.input} type="number" value={sortOrder}
                onChange={e => setSortOrder(parseInt(e.target.value, 10) || 0)} />
            </div>
          </div>
          <div className={f.field}>
            <label className={f.label}>{t('configuration.lookup.description')}</label>
            <textarea className={f.textarea} value={desc}
              onChange={e => setDesc(e.target.value)} placeholder={t('common.optional')} />
          </div>
        </form>
      </Modal>
    </>
  );
}
