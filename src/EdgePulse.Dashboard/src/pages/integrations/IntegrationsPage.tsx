import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getWebhooks, getWebhookEvents, createWebhook, updateWebhook,
  deleteWebhook, testWebhook,
} from '../../api/webhooks';
import type { WebhookDto } from '../../types/api';
import { useConfirm } from '../../context/ConfirmContext';
import { useToast } from '../../context/ToastContext';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Modal from '../../components/common/Modal';
import styles from './IntegrationsPage.module.css';
import f from '../../components/common/FormField.module.css';

export default function IntegrationsPage() {
  const { t, i18n } = useTranslation();
  const qc = useQueryClient();
  const confirm = useConfirm();
  const toast = useToast();

  const { data: hooks = [], isLoading } = useQuery({
    queryKey: ['webhooks'], queryFn: getWebhooks,
  });
  const { data: availableEvents = [] } = useQuery({
    queryKey: ['webhook-events'], queryFn: getWebhookEvents,
  });

  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<WebhookDto | null>(null);
  const [name, setName] = useState('');
  const [url, setUrl] = useState('');
  const [secret, setSecret] = useState('');
  const [events, setEvents] = useState<string[]>([]);
  const [format, setFormat] = useState('json');
  const [active, setActive] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function invalidate() {
    await qc.invalidateQueries({ queryKey: ['webhooks'] });
  }

  function openAdd() {
    setEditing(null);
    setName(''); setUrl(''); setSecret('');
    setEvents([...availableEvents]); setFormat('json'); setActive(true);
    setError(null); setOpen(true);
  }

  function openEdit(w: WebhookDto) {
    setEditing(w);
    setName(w.name); setUrl(w.url); setSecret('');
    setEvents(w.events); setFormat(w.format); setActive(w.isActive);
    setError(null); setOpen(true);
  }

  function toggleEvent(e: string) {
    setEvents(prev => prev.includes(e) ? prev.filter(x => x !== e) : [...prev, e]);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      if (editing) {
        await updateWebhook(editing.id, {
          name, url, secret: secret || null, events, format, isActive: active,
        });
      } else {
        await createWebhook({ name, url, secret, events, format });
      }
      await invalidate();
      toast.success(t('integrations.saved', { name }));
      setOpen(false);
    } catch {
      setError(t('integrations.saveError'));
    } finally { setSaving(false); }
  }

  async function handleTest(w: WebhookDto) {
    try {
      const status = await testWebhook(w.id);
      await invalidate();
      if (status.startsWith('2')) toast.success(t('integrations.testOk', { status }));
      else toast.warning(t('integrations.testFailed', { status }));
    } catch {
      toast.error(t('integrations.testError'));
    }
  }

  async function handleDelete(w: WebhookDto) {
    const ok = await confirm({
      message: t('integrations.deleteConfirm', { name: w.name }),
      variant: 'danger',
      confirmLabel: t('common.delete'),
    });
    if (!ok) return;
    try {
      await deleteWebhook(w.id);
      await invalidate();
      toast.success(t('common.deleted', { name: w.name }));
    } catch {
      toast.error(t('integrations.saveError'));
    }
  }

  if (isLoading) return <LoadingSpinner message={t('common.loading')} />;

  const statusClass = (s: string | null) =>
    !s ? styles.statusNone : s.startsWith('2') ? styles.statusOk : styles.statusErr;

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <p className={styles.hint}>{t('integrations.hint')}</p>
        <button className={styles.addBtn} onClick={openAdd}>{t('integrations.addBtn')}</button>
      </div>

      <div className={styles.card}>
        {hooks.length === 0 ? (
          <div className={styles.empty}>{t('integrations.empty')}</div>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t('integrations.name')}</th>
                <th>{t('integrations.events')}</th>
                <th>{t('integrations.format')}</th>
                <th>{t('integrations.lastDelivery')}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {hooks.map(w => (
                <tr key={w.id}>
                  <td>
                    <span className={styles.whName}>
                      {w.name}{!w.isActive && <span className={styles.inactive}> — {t('integrations.inactive')}</span>}
                    </span>
                    <span className={styles.whUrl}>{w.url}</span>
                  </td>
                  <td>
                    {w.events.map(e => (
                      <span key={e} className={styles.eventChip}>{e}</span>
                    ))}
                  </td>
                  <td><span className={styles.formatChip}>{w.format}</span></td>
                  <td>
                    <span className={statusClass(w.lastStatus)}>
                      {w.lastStatus ?? t('integrations.never')}
                    </span>
                    {w.lastTriggeredAt && (
                      <span className={styles.whUrl}>
                        {new Date(w.lastTriggeredAt + (w.lastTriggeredAt.endsWith('Z') ? '' : 'Z'))
                          .toLocaleString(i18n.language)}
                      </span>
                    )}
                  </td>
                  <td>
                    <div className={styles.actions}>
                      <button className={styles.actionBtn} onClick={() => handleTest(w)}>
                        {t('integrations.test')}
                      </button>
                      <button className={styles.actionBtn} onClick={() => openEdit(w)}>
                        {t('common.edit')}
                      </button>
                      <button className={`${styles.actionBtn} ${styles.deleteBtn}`}
                        onClick={() => handleDelete(w)}>
                        {t('common.delete')}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <Modal open={open}
        title={editing ? t('integrations.editTitle') : t('integrations.addTitle')}
        onClose={() => setOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setOpen(false)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="wh-form" disabled={saving}>
              {saving ? t('common.saving') : editing ? t('common.save') : t('common.create')}
            </button>
          </>
        }>
        <form id="wh-form" className={f.formGrid} onSubmit={handleSubmit}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>{t('integrations.name')}</label>
            <input className={f.input} required maxLength={100} value={name}
              onChange={e => setName(e.target.value)}
              placeholder={t('integrations.namePlaceholder')} />
          </div>
          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>{t('integrations.url')}</label>
            <input className={f.input} required type="url" value={url}
              onChange={e => setUrl(e.target.value)}
              placeholder="https://hooks.slack.com/services/… or your endpoint" />
          </div>
          <div className={f.field}>
            <label className={`${f.label} ${editing ? '' : f.required}`}>
              {t('integrations.secret')}
            </label>
            <input className={f.input} required={!editing} minLength={8} value={secret}
              onChange={e => setSecret(e.target.value)}
              placeholder={editing ? t('integrations.secretKeep') : t('integrations.secretPlaceholder')} />
          </div>
          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>{t('integrations.events')}</label>
            <div className={styles.checkboxRow}>
              {availableEvents.map(e => (
                <label key={e} className={styles.checkboxLabel}>
                  <input type="checkbox" checked={events.includes(e)}
                    onChange={() => toggleEvent(e)} />
                  {e}
                </label>
              ))}
            </div>
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={f.label}>{t('integrations.format')}</label>
              <select className={f.select} value={format} onChange={e => setFormat(e.target.value)}>
                <option value="json">JSON (signed)</option>
                <option value="slack">Slack / Teams text</option>
              </select>
            </div>
            {editing && (
              <div className={f.field}>
                <label className={f.label}>{t('integrations.active')}</label>
                <select className={f.select} value={active ? '1' : '0'}
                  onChange={e => setActive(e.target.value === '1')}>
                  <option value="1">{t('integrations.activeYes')}</option>
                  <option value="0">{t('integrations.activeNo')}</option>
                </select>
              </div>
            )}
          </div>
        </form>
      </Modal>
    </div>
  );
}
