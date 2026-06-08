import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getDeviceTypes, createDeviceType, updateDeviceType, deleteDeviceType,
} from '../../../api/configuration';
import type { DeviceTypeDto } from '../../../types/api';
import LoadingSpinner from '../../../components/common/LoadingSpinner';
import Modal from '../../../components/common/Modal';
import styles from './LookupTable.module.css';
import f from '../../../components/common/FormField.module.css';

export default function DeviceTypesTab() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const { data = [], isLoading } = useQuery({ queryKey: ['device-types'], queryFn: getDeviceTypes });

  const [open, setOpen]       = useState(false);
  const [editing, setEditing] = useState<DeviceTypeDto | null>(null);
  const [name, setName]       = useState('');
  const [code, setCode]       = useState('');
  const [icon, setIcon]       = useState('');
  const [desc, setDesc]       = useState('');
  const [sortOrder, setSortOrder] = useState(0);
  const [saving, setSaving]   = useState(false);
  const [error, setError]     = useState<string | null>(null);

  function openAdd() {
    setEditing(null);
    setName(''); setCode(''); setIcon(''); setDesc(''); setSortOrder((data.length + 1) * 10);
    setError(null); setOpen(true);
  }

  function openEdit(row: DeviceTypeDto) {
    setEditing(row);
    setName(row.name); setCode(row.code); setIcon(row.icon ?? '');
    setDesc(row.description ?? ''); setSortOrder(row.sortOrder);
    setError(null); setOpen(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      if (editing) {
        await updateDeviceType(editing.id, { name, description: desc || undefined, icon: icon || undefined, sortOrder });
      } else {
        await createDeviceType({ name, code, description: desc || undefined, icon: icon || undefined, sortOrder });
      }
      await qc.invalidateQueries({ queryKey: ['device-types'] });
      setOpen(false);
    } catch {
      setError(editing ? t('configuration.lookup.errorUpdate') : t('configuration.lookup.errorCreate'));
    } finally { setSaving(false); }
  }

  async function handleDelete(row: DeviceTypeDto) {
    if (!confirm(t('configuration.deviceTypes.deleteConfirm', { name: row.name }))) return;
    try {
      await deleteDeviceType(row.id);
      await qc.invalidateQueries({ queryKey: ['device-types'] });
    } catch { alert(t('configuration.lookup.errorDelete')); }
  }

  if (isLoading) return <LoadingSpinner message={t('common.loading')} />;

  return (
    <>
      <div className={styles.toolbar}>
        <span className={styles.count}>{t('configuration.deviceTypes.count', { count: data.length })}</span>
        <button className={styles.addBtn} onClick={openAdd}>{t('configuration.deviceTypes.addBtn')}</button>
      </div>

      {data.length === 0 ? (
        <div className={styles.empty}>{t('configuration.lookup.empty')}</div>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>{t('configuration.lookup.name')}</th>
              <th>{t('configuration.lookup.code')}</th>
              <th>{t('configuration.lookup.icon')}</th>
              <th>{t('configuration.lookup.description')}</th>
              <th>{t('configuration.lookup.sort')}</th>
              <th className={styles.actionsCol} />
            </tr>
          </thead>
          <tbody>
            {data.slice().sort((a, b) => a.sortOrder - b.sortOrder).map(row => (
              <tr key={row.id}>
                <td>{row.name}</td>
                <td className={styles.code}>{row.code}</td>
                <td className={styles.muted}>{row.icon ?? '—'}</td>
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
        title={editing ? t('configuration.deviceTypes.editTitle') : t('configuration.deviceTypes.addTitle')}
        onClose={() => setOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setOpen(false)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="dt-form" disabled={saving}>
              {saving ? t('common.saving') : editing ? t('common.save') : t('common.create')}
            </button>
          </>
        }
      >
        <form id="dt-form" className={f.formGrid} onSubmit={handleSubmit}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('configuration.lookup.name')}</label>
              <input className={f.input} required value={name}
                onChange={e => setName(e.target.value)} placeholder={t('configuration.deviceTypes.namePlaceholder')} />
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('configuration.lookup.code')}</label>
              <input className={f.input} required value={code} disabled={!!editing}
                onChange={e => setCode(e.target.value.toUpperCase())} placeholder={t('configuration.deviceTypes.codePlaceholder')} />
            </div>
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={f.label}>{t('configuration.lookup.icon')}</label>
              <input className={f.input} value={icon}
                onChange={e => setIcon(e.target.value)} placeholder={t('configuration.deviceTypes.iconPlaceholder')} />
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
