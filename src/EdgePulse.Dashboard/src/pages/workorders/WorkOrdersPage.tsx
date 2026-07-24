import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getWorkOrders, createWorkOrder, transitionWorkOrder, assignWorkOrder,
} from '../../api/workorders';
import { getDevices } from '../../api/devices';
import type { WorkOrderDto } from '../../types/api';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { useConfirm } from '../../context/ConfirmContext';
import { useToast } from '../../context/ToastContext';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Modal from '../../components/common/Modal';
import styles from './WorkOrdersPage.module.css';
import f from '../../components/common/FormField.module.css';

const STATUSES = ['', 'OPEN', 'INPROGRESS', 'ONHOLD', 'COMPLETED', 'CANCELLED'] as const;
const PRIORITIES = ['LOW', 'MEDIUM', 'HIGH', 'CRITICAL'];

export default function WorkOrdersPage() {
  const { t, i18n } = useTranslation();
  const qc = useQueryClient();
  const confirm = useConfirm();
  const toast = useToast();
  const me = useCurrentUser();
  const readOnly = me?.role === 'Executive';

  const [statusFilter, setStatusFilter] = useState<string>('');

  const { data: orders = [], isLoading } = useQuery({
    queryKey: ['workorders', statusFilter],
    queryFn: () => getWorkOrders(statusFilter ? { status: statusFilter } : undefined),
    refetchInterval: 60_000,
  });
  const { data: devices = [] } = useQuery({ queryKey: ['devices'], queryFn: () => getDevices() });

  // ── Create modal ──────────────────────────────────────────────────────────
  const [createOpen, setCreateOpen] = useState(false);
  const [deviceId, setDeviceId] = useState('');
  const [title, setTitle] = useState('');
  const [desc, setDesc] = useState('');
  const [priority, setPriority] = useState('MEDIUM');
  const [dueDate, setDueDate] = useState('');
  const [assignee, setAssignee] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // ── Complete modal ────────────────────────────────────────────────────────
  const [completing, setCompleting] = useState<WorkOrderDto | null>(null);
  const [notes, setNotes] = useState('');
  const [parts, setParts] = useState('');

  // ── Assign modal ──────────────────────────────────────────────────────────
  const [assigning, setAssigning] = useState<WorkOrderDto | null>(null);
  const [assignTo, setAssignTo] = useState('');

  async function invalidate() {
    await qc.invalidateQueries({ queryKey: ['workorders'] });
  }

  function openCreate() {
    setDeviceId(''); setTitle(''); setDesc(''); setPriority('MEDIUM');
    setDueDate(''); setAssignee(''); setError(null); setCreateOpen(true);
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      await createWorkOrder({
        deviceId, title, description: desc || undefined, priority,
        dueDate: dueDate ? `${dueDate}T12:00:00Z` : null,
        assignedTo: assignee || null,
      });
      await invalidate();
      toast.success(t('workorders.created'));
      setCreateOpen(false);
    } catch {
      setError(t('workorders.createError'));
    } finally { setSaving(false); }
  }

  async function doTransition(
    wo: WorkOrderDto, action: 'start' | 'hold' | 'cancel',
  ) {
    if (action === 'cancel') {
      const ok = await confirm({
        message: t('workorders.cancelConfirm', { number: wo.number }),
        variant: 'warning',
        confirmLabel: t('workorders.cancelBtn'),
      });
      if (!ok) return;
    }
    try {
      await transitionWorkOrder(wo.id, action);
      await invalidate();
      toast.success(t(`workorders.${action}Done`, { number: wo.number }));
    } catch {
      toast.error(t('workorders.transitionError'));
    }
  }

  async function handleComplete(e: FormEvent) {
    e.preventDefault();
    if (!completing) return;
    setSaving(true); setError(null);
    try {
      await transitionWorkOrder(completing.id, 'complete', notes || undefined, parts || undefined);
      await invalidate();
      toast.success(t('workorders.completeDone', { number: completing.number }));
      setCompleting(null);
    } catch {
      setError(t('workorders.transitionError'));
    } finally { setSaving(false); }
  }

  async function handleAssign(e: FormEvent) {
    e.preventDefault();
    if (!assigning) return;
    setSaving(true); setError(null);
    try {
      await assignWorkOrder(assigning.id, assignTo || null);
      await invalidate();
      toast.success(t('workorders.assignDone', { number: assigning.number }));
      setAssigning(null);
    } catch {
      setError(t('workorders.transitionError'));
    } finally { setSaving(false); }
  }

  const fmtDate = (iso: string | null) =>
    iso ? new Date(iso + (iso.endsWith('Z') ? '' : 'Z')).toLocaleDateString(i18n.language) : '—';

  if (isLoading) return <LoadingSpinner message={t('common.loading')} />;

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <div className={styles.tabs}>
          {STATUSES.map(s => (
            <button
              key={s || 'all'}
              className={`${styles.tab} ${statusFilter === s ? styles.tabActive : ''}`}
              onClick={() => setStatusFilter(s)}
            >
              {s === '' ? t('common.all') : t(`workorders.status.${s}`)}
            </button>
          ))}
        </div>
        {!readOnly && (
          <button className={styles.addBtn} onClick={openCreate}>
            {t('workorders.addBtn')}
          </button>
        )}
      </div>

      <div className={styles.card}>
        {orders.length === 0 ? (
          <div className={styles.empty}>{t('workorders.empty')}</div>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t('workorders.number')}</th>
                <th>{t('workorders.workOrder')}</th>
                <th>{t('workorders.priority')}</th>
                <th>{t('workorders.statusHeader')}</th>
                <th>{t('workorders.assignedTo')}</th>
                <th>{t('workorders.due')}</th>
                {!readOnly && <th />}
              </tr>
            </thead>
            <tbody>
              {orders.map(wo => (
                <tr key={wo.id}>
                  <td className={styles.number}>{wo.number}</td>
                  <td>
                    <span className={styles.woTitle}>{wo.title}</span>
                    <span className={styles.woDevice}>
                      {wo.deviceName} ({wo.deviceCode})
                    </span>
                    {wo.description && <span className={styles.woDesc}>{wo.description}</span>}
                    {wo.status === 'COMPLETED' && wo.completionNotes && (
                      <span className={styles.woNotes}>
                        ✓ {wo.completionNotes}{wo.partsUsed ? ` — ${wo.partsUsed}` : ''}
                      </span>
                    )}
                  </td>
                  <td>
                    <span className={`${styles.chip} ${styles[`prio${wo.priority}` as keyof typeof styles] ?? ''}`}>
                      {t(`workorders.prio.${wo.priority}`)}
                    </span>
                  </td>
                  <td>
                    <span className={`${styles.chip} ${styles[`st${wo.status}` as keyof typeof styles] ?? ''}`}>
                      {t(`workorders.status.${wo.status}`)}
                    </span>
                  </td>
                  <td className={styles.muted}>{wo.assignedTo ?? '—'}</td>
                  <td className={styles.muted}>{fmtDate(wo.dueDate)}</td>
                  {!readOnly && (
                    <td>
                      <div className={styles.actions}>
                        {(wo.status === 'OPEN' || wo.status === 'ONHOLD') && (
                          <button className={styles.actionBtn}
                            onClick={() => doTransition(wo, 'start')}>
                            {t('workorders.start')}
                          </button>
                        )}
                        {wo.status === 'INPROGRESS' && (
                          <>
                            <button className={styles.actionBtn}
                              onClick={() => doTransition(wo, 'hold')}>
                              {t('workorders.hold')}
                            </button>
                            <button className={`${styles.actionBtn} ${styles.completeBtn}`}
                              onClick={() => { setCompleting(wo); setNotes(''); setParts(''); setError(null); }}>
                              {t('workorders.complete')}
                            </button>
                          </>
                        )}
                        {(wo.status === 'OPEN' || wo.status === 'INPROGRESS' || wo.status === 'ONHOLD') && (
                          <>
                            <button className={styles.actionBtn}
                              onClick={() => { setAssigning(wo); setAssignTo(wo.assignedTo ?? ''); setError(null); }}>
                              {t('workorders.assign')}
                            </button>
                            <button className={`${styles.actionBtn} ${styles.cancelBtn}`}
                              onClick={() => doTransition(wo, 'cancel')}>
                              {t('workorders.cancelBtn')}
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Create */}
      <Modal open={createOpen} title={t('workorders.addTitle')} onClose={() => setCreateOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setCreateOpen(false)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="wo-create" disabled={saving}>
              {saving ? t('common.creating') : t('common.create')}
            </button>
          </>
        }>
        <form id="wo-create" className={f.formGrid} onSubmit={handleCreate}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>{t('workorders.device')}</label>
            <select className={f.select} required value={deviceId}
              onChange={e => setDeviceId(e.target.value)}>
              <option value="">{t('workorders.selectDevice')}</option>
              {devices.map(d => (
                <option key={d.id} value={d.id}>{d.name} ({d.code})</option>
              ))}
            </select>
          </div>
          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>{t('workorders.title')}</label>
            <input className={f.input} required maxLength={200} value={title}
              onChange={e => setTitle(e.target.value)}
              placeholder={t('workorders.titlePlaceholder')} />
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={f.label}>{t('workorders.priority')}</label>
              <select className={f.select} value={priority} onChange={e => setPriority(e.target.value)}>
                {PRIORITIES.map(p => <option key={p} value={p}>{t(`workorders.prio.${p}`)}</option>)}
              </select>
            </div>
            <div className={f.field}>
              <label className={f.label}>{t('workorders.due')}</label>
              <input className={f.input} type="date" value={dueDate}
                onChange={e => setDueDate(e.target.value)} />
            </div>
          </div>
          <div className={f.field}>
            <label className={f.label}>{t('workorders.assignedTo')}</label>
            <input className={f.input} value={assignee}
              onChange={e => setAssignee(e.target.value)}
              placeholder={t('workorders.assignPlaceholder')} />
          </div>
          <div className={f.field}>
            <label className={f.label}>{t('configuration.lookup.description')}</label>
            <textarea className={f.textarea} value={desc}
              onChange={e => setDesc(e.target.value)} placeholder={t('common.optional')} />
          </div>
        </form>
      </Modal>

      {/* Complete */}
      <Modal open={!!completing}
        title={t('workorders.completeTitle', { number: completing?.number ?? '' })}
        onClose={() => setCompleting(null)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setCompleting(null)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="wo-complete" disabled={saving}>
              {saving ? t('common.saving') : t('workorders.complete')}
            </button>
          </>
        }>
        <form id="wo-complete" className={f.formGrid} onSubmit={handleComplete}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.field}>
            <label className={f.label}>{t('workorders.completionNotes')}</label>
            <textarea className={f.textarea} value={notes}
              onChange={e => setNotes(e.target.value)}
              placeholder={t('workorders.notesPlaceholder')} />
          </div>
          <div className={f.field}>
            <label className={f.label}>{t('workorders.partsUsed')}</label>
            <input className={f.input} value={parts}
              onChange={e => setParts(e.target.value)}
              placeholder={t('workorders.partsPlaceholder')} />
          </div>
        </form>
      </Modal>

      {/* Assign */}
      <Modal open={!!assigning}
        title={t('workorders.assignTitle', { number: assigning?.number ?? '' })}
        onClose={() => setAssigning(null)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setAssigning(null)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="wo-assign" disabled={saving}>
              {saving ? t('common.saving') : t('workorders.assign')}
            </button>
          </>
        }>
        <form id="wo-assign" className={f.formGrid} onSubmit={handleAssign}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.field}>
            <label className={f.label}>{t('workorders.assignedTo')}</label>
            <input className={f.input} value={assignTo}
              onChange={e => setAssignTo(e.target.value)}
              placeholder={t('workorders.assignPlaceholder')} />
          </div>
        </form>
      </Modal>
    </div>
  );
}
