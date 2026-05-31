import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  getDeviceTypes, createDeviceType, updateDeviceType, deleteDeviceType,
} from '../../../api/configuration';
import type { DeviceTypeDto } from '../../../types/api';
import LoadingSpinner from '../../../components/common/LoadingSpinner';
import Modal from '../../../components/common/Modal';
import styles from './LookupTable.module.css';
import f from '../../../components/common/FormField.module.css';

export default function DeviceTypesTab() {
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
      setError(editing ? 'Failed to update.' : 'Failed to create. Code may already exist.');
    } finally { setSaving(false); }
  }

  async function handleDelete(row: DeviceTypeDto) {
    if (!confirm(`Delete device type "${row.name}"?\n\nDevices already of this type keep their assignment.`)) return;
    try {
      await deleteDeviceType(row.id);
      await qc.invalidateQueries({ queryKey: ['device-types'] });
    } catch { alert('Failed to delete. It may be in use or be a system type.'); }
  }

  if (isLoading) return <LoadingSpinner message="Loading…" />;

  return (
    <>
      <div className={styles.toolbar}>
        <span className={styles.count}>{data.length} device types</span>
        <button className={styles.addBtn} onClick={openAdd}>+ Add Device Type</button>
      </div>

      {data.length === 0 ? (
        <div className={styles.empty}>No device types defined.</div>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr><th>Name</th><th>Code</th><th>Icon</th><th>Description</th><th>Sort</th><th className={styles.actionsCol} /></tr>
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
                  <button className={styles.editBtn} onClick={() => openEdit(row)}>Edit</button>
                  <button className={styles.deleteBtn} onClick={() => handleDelete(row)}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <Modal
        open={open}
        title={editing ? 'Edit Device Type' : 'Add Device Type'}
        onClose={() => setOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setOpen(false)}>Cancel</button>
            <button className={f.btnPrimary} form="dt-form" disabled={saving}>
              {saving ? 'Saving…' : editing ? 'Save Changes' : 'Create'}
            </button>
          </>
        }
      >
        <form id="dt-form" className={f.formGrid} onSubmit={handleSubmit}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>Name</label>
              <input className={f.input} required value={name}
                onChange={e => setName(e.target.value)} placeholder="e.g. Pump" />
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>Code</label>
              <input className={f.input} required value={code} disabled={!!editing}
                onChange={e => setCode(e.target.value.toUpperCase())} placeholder="e.g. PUMP" />
            </div>
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={f.label}>Icon</label>
              <input className={f.input} value={icon}
                onChange={e => setIcon(e.target.value)} placeholder="e.g. 💧 or pump.svg" />
            </div>
            <div className={f.field}>
              <label className={f.label}>Sort Order</label>
              <input className={f.input} type="number" value={sortOrder}
                onChange={e => setSortOrder(parseInt(e.target.value, 10) || 0)} />
            </div>
          </div>
          <div className={f.field}>
            <label className={f.label}>Description</label>
            <textarea className={f.textarea} value={desc}
              onChange={e => setDesc(e.target.value)} placeholder="Optional" />
          </div>
        </form>
      </Modal>
    </>
  );
}
