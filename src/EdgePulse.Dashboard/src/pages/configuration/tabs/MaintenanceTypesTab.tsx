import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  getMaintenanceTypes, createMaintenanceType, updateMaintenanceType, deleteMaintenanceType,
} from '../../../api/configuration';
import type { MaintenanceTypeDto } from '../../../types/api';
import LoadingSpinner from '../../../components/common/LoadingSpinner';
import Modal from '../../../components/common/Modal';
import styles from './LookupTable.module.css';
import f from '../../../components/common/FormField.module.css';

export default function MaintenanceTypesTab() {
  const qc = useQueryClient();
  const { data = [], isLoading } = useQuery({ queryKey: ['maintenance-types'], queryFn: getMaintenanceTypes });

  const [open, setOpen]       = useState(false);
  const [editing, setEditing] = useState<MaintenanceTypeDto | null>(null);
  const [name, setName]       = useState('');
  const [code, setCode]       = useState('');
  const [desc, setDesc]       = useState('');
  const [color, setColor]     = useState('#3b82f6');
  const [sortOrder, setSortOrder] = useState(0);
  const [saving, setSaving]   = useState(false);
  const [error, setError]     = useState<string | null>(null);

  function openAdd() {
    setEditing(null);
    setName(''); setCode(''); setDesc(''); setColor('#3b82f6'); setSortOrder((data.length + 1) * 10);
    setError(null); setOpen(true);
  }

  function openEdit(row: MaintenanceTypeDto) {
    setEditing(row);
    setName(row.name); setCode(row.code); setDesc(row.description ?? '');
    setColor(row.color ?? '#3b82f6'); setSortOrder(row.sortOrder);
    setError(null); setOpen(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      if (editing) {
        await updateMaintenanceType(editing.id, { name, description: desc || undefined, color, sortOrder });
      } else {
        await createMaintenanceType({ name, code, description: desc || undefined, color, sortOrder });
      }
      await qc.invalidateQueries({ queryKey: ['maintenance-types'] });
      setOpen(false);
    } catch {
      setError(editing ? 'Failed to update.' : 'Failed to create.');
    } finally { setSaving(false); }
  }

  async function handleDelete(row: MaintenanceTypeDto) {
    if (!confirm(`Delete maintenance type "${row.name}"?`)) return;
    try {
      await deleteMaintenanceType(row.id);
      await qc.invalidateQueries({ queryKey: ['maintenance-types'] });
    } catch { alert('Failed to delete. It may be in use or be a system type.'); }
  }

  if (isLoading) return <LoadingSpinner message="Loading…" />;

  return (
    <>
      <div className={styles.toolbar}>
        <span className={styles.count}>{data.length} maintenance types</span>
        <button className={styles.addBtn} onClick={openAdd}>+ Add Maintenance Type</button>
      </div>

      {data.length === 0 ? (
        <div className={styles.empty}>No maintenance types defined.</div>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr><th>Name</th><th>Code</th><th>Color</th><th>Description</th><th>Sort</th><th className={styles.actionsCol} /></tr>
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
        title={editing ? 'Edit Maintenance Type' : 'Add Maintenance Type'}
        onClose={() => setOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setOpen(false)}>Cancel</button>
            <button className={f.btnPrimary} form="mt-form" disabled={saving}>
              {saving ? 'Saving…' : editing ? 'Save Changes' : 'Create'}
            </button>
          </>
        }
      >
        <form id="mt-form" className={f.formGrid} onSubmit={handleSubmit}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>Name</label>
              <input className={f.input} required value={name}
                onChange={e => setName(e.target.value)} placeholder="e.g. Preventive" />
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>Code</label>
              <input className={f.input} required value={code} disabled={!!editing}
                onChange={e => setCode(e.target.value.toUpperCase())} placeholder="e.g. PM" />
            </div>
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={f.label}>Color</label>
              <input className={f.input} type="color" value={color}
                onChange={e => setColor(e.target.value)} style={{ padding: 2, height: 36 }} />
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
