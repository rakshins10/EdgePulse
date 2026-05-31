import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  getLocationTypes,
  createLocationType,
  updateLocationType,
  deleteLocationType,
} from '../../../api/configuration';
import type { LocationTypeDto } from '../../../types/api';
import LoadingSpinner from '../../../components/common/LoadingSpinner';
import Modal from '../../../components/common/Modal';
import styles from './LookupTable.module.css';
import f from '../../../components/common/FormField.module.css';

export default function LocationTypesTab() {
  const qc = useQueryClient();
  const { data = [], isLoading } = useQuery({ queryKey: ['location-types'], queryFn: getLocationTypes });

  const [open, setOpen]       = useState(false);
  const [editing, setEditing] = useState<LocationTypeDto | null>(null);
  const [name, setName]       = useState('');
  const [code, setCode]       = useState('');
  const [desc, setDesc]       = useState('');
  const [sortOrder, setSortOrder] = useState(0);
  const [saving, setSaving]   = useState(false);
  const [error, setError]     = useState<string | null>(null);

  function openAdd() {
    setEditing(null);
    setName(''); setCode(''); setDesc(''); setSortOrder((data.length + 1) * 10);
    setError(null); setOpen(true);
  }

  function openEdit(row: LocationTypeDto) {
    setEditing(row);
    setName(row.name); setCode(row.code); setDesc(row.description ?? '');
    setSortOrder(row.sortOrder);
    setError(null); setOpen(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true); setError(null);
    try {
      if (editing) {
        await updateLocationType(editing.id, { name, description: desc || undefined, sortOrder });
      } else {
        await createLocationType({ name, code, description: desc || undefined, sortOrder });
      }
      await qc.invalidateQueries({ queryKey: ['location-types'] });
      setOpen(false);
    } catch {
      setError(editing ? 'Failed to update.' : 'Failed to create. Code may already exist.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(row: LocationTypeDto) {
    if (!confirm(`Delete location type "${row.name}"?\n\nAreas already using it keep their reference until reassigned.`)) return;
    try {
      await deleteLocationType(row.id);
      await qc.invalidateQueries({ queryKey: ['location-types'] });
    } catch {
      alert('Failed to delete. It may be in use or be a system type.');
    }
  }

  if (isLoading) return <LoadingSpinner message="Loading…" />;

  return (
    <>
      <div className={styles.toolbar}>
        <span className={styles.count}>{data.length} location types</span>
        <button className={styles.addBtn} onClick={openAdd}>+ Add Location Type</button>
      </div>

      {data.length === 0 ? (
        <div className={styles.empty}>No location types defined.</div>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Name</th>
              <th>Code</th>
              <th>Description</th>
              <th>Sort Order</th>
              <th className={styles.actionsCol} />
            </tr>
          </thead>
          <tbody>
            {data
              .slice()
              .sort((a, b) => a.sortOrder - b.sortOrder)
              .map(row => (
                <tr key={row.id}>
                  <td>{row.name}</td>
                  <td className={styles.code}>{row.code}</td>
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
        title={editing ? 'Edit Location Type' : 'Add Location Type'}
        onClose={() => setOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setOpen(false)}>Cancel</button>
            <button className={f.btnPrimary} form="loc-form" disabled={saving}>
              {saving ? 'Saving…' : editing ? 'Save Changes' : 'Create'}
            </button>
          </>
        }
      >
        <form id="loc-form" className={f.formGrid} onSubmit={handleSubmit}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>Name</label>
              <input className={f.input} required value={name}
                onChange={e => setName(e.target.value)} placeholder="e.g. Floor" />
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>Code</label>
              <input className={f.input} required value={code} disabled={!!editing}
                onChange={e => setCode(e.target.value.toUpperCase())} placeholder="e.g. FLOOR" />
            </div>
          </div>
          <div className={f.field}>
            <label className={f.label}>Description</label>
            <textarea className={f.textarea} value={desc}
              onChange={e => setDesc(e.target.value)} placeholder="Optional" />
          </div>
          <div className={f.field}>
            <label className={f.label}>Sort Order</label>
            <input className={f.input} type="number" value={sortOrder}
              onChange={e => setSortOrder(parseInt(e.target.value, 10) || 0)} />
          </div>
        </form>
      </Modal>
    </>
  );
}
