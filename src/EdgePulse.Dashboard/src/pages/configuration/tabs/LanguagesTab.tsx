import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getLocales, createLocale, updateLocale, deleteLocale, setDefaultLocale,
  type LocaleDto,
} from '../../../api/localization';
import LoadingSpinner from '../../../components/common/LoadingSpinner';
import Modal from '../../../components/common/Modal';
import styles from './LookupTable.module.css';
import f from '../../../components/common/FormField.module.css';

export default function LanguagesTab() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const { data = [], isLoading } = useQuery({ queryKey: ['locales', 'all'], queryFn: () => getLocales(false) });

  const [open, setOpen]       = useState(false);
  const [editing, setEditing] = useState<LocaleDto | null>(null);
  const [code, setCode]       = useState('');
  const [displayName, setDisplayName] = useState('');
  const [nativeName, setNativeName]   = useState('');
  const [flag, setFlag]       = useState('');
  const [enabled, setEnabled] = useState(true);
  const [sortOrder, setSortOrder] = useState(0);
  const [saving, setSaving]   = useState(false);
  const [error, setError]     = useState<string | null>(null);

  function refresh() {
    return Promise.all([
      qc.invalidateQueries({ queryKey: ['locales', 'all'] }),
      qc.invalidateQueries({ queryKey: ['locales', 'enabled'] }),
    ]);
  }

  function openAdd() {
    setEditing(null);
    setCode(''); setDisplayName(''); setNativeName(''); setFlag('');
    setEnabled(true); setSortOrder((data.length + 1) * 10);
    setError(null); setOpen(true);
  }

  function openEdit(row: LocaleDto) {
    setEditing(row);
    setCode(row.code); setDisplayName(row.displayName); setNativeName(row.nativeName);
    setFlag(row.flag ?? ''); setEnabled(row.isEnabled); setSortOrder(row.sortOrder);
    setError(null); setOpen(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      if (editing) {
        await updateLocale(editing.id, {
          displayName, nativeName, flag: flag || undefined, isEnabled: enabled, sortOrder,
        });
      } else {
        await createLocale({
          code, displayName, nativeName, flag: flag || undefined, isEnabled: enabled, sortOrder,
        });
      }
      await refresh();
      setOpen(false);
    } catch {
      setError(editing ? t('configuration.languages.errorUpdate') : t('configuration.languages.errorCreate'));
    } finally { setSaving(false); }
  }

  async function handleDelete(row: LocaleDto) {
    if (!confirm(t('configuration.languages.deleteConfirm', { name: row.displayName }))) return;
    try {
      await deleteLocale(row.id);
      await refresh();
    } catch (err) {
      const msg = (err as { response?: { data?: { title?: string } } })?.response?.data?.title
        ?? t('configuration.languages.errorDelete');
      alert(msg);
    }
  }

  async function handleSetDefault(row: LocaleDto) {
    try {
      await setDefaultLocale(row.id);
      await refresh();
    } catch {
      alert(t('configuration.languages.errorSetDefault'));
    }
  }

  if (isLoading) return <LoadingSpinner message={t('common.loading')} />;

  return (
    <>
      <div className={styles.toolbar}>
        <span className={styles.count}>{t('configuration.languages.count', { count: data.length })}</span>
        <button className={styles.addBtn} onClick={openAdd}>{t('configuration.languages.addBtn')}</button>
      </div>

      <p style={{ padding: '0 20px 12px', fontSize: 12, color: 'var(--color-text-muted)' }}>
        {t('configuration.languages.hint')}
      </p>

      <table className={styles.table}>
        <thead>
          <tr>
            <th>{t('configuration.languages.code')}</th>
            <th>{t('configuration.languages.displayName')}</th>
            <th>{t('configuration.languages.nativeName')}</th>
            <th>{t('configuration.languages.flag')}</th>
            <th>{t('configuration.languages.enabled')}</th>
            <th>{t('configuration.languages.default')}</th>
            <th className={styles.actionsCol} />
          </tr>
        </thead>
        <tbody>
          {data.map(row => (
            <tr key={row.id}>
              <td className={styles.code}>{row.code}</td>
              <td>{row.displayName}</td>
              <td>{row.nativeName}</td>
              <td>{row.flag ?? '—'}</td>
              <td className={styles.muted}>{row.isEnabled ? '✓' : '—'}</td>
              <td className={styles.muted}>
                {row.isDefault
                  ? '★'
                  : <button className={styles.editBtn} onClick={() => handleSetDefault(row)}>{t('configuration.languages.setDefault')}</button>}
              </td>
              <td className={styles.actionsCol}>
                <button className={styles.editBtn} onClick={() => openEdit(row)}>{t('common.edit')}</button>
                {!row.isDefault && (
                  <button className={styles.deleteBtn} onClick={() => handleDelete(row)}>{t('common.delete')}</button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <Modal
        open={open}
        title={editing ? t('configuration.languages.editTitle') : t('configuration.languages.addTitle')}
        onClose={() => setOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setOpen(false)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="lang-form" disabled={saving}>
              {saving ? t('common.saving') : editing ? t('common.save') : t('common.create')}
            </button>
          </>
        }
      >
        <form id="lang-form" className={f.formGrid} onSubmit={handleSubmit}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('configuration.languages.code')}</label>
              <input className={f.input} required value={code} disabled={!!editing}
                onChange={e => setCode(e.target.value.toLowerCase())} placeholder="de" />
              <span className={f.hint}>{t('configuration.languages.codeHint')}</span>
            </div>
            <div className={f.field}>
              <label className={f.label}>{t('configuration.languages.flag')}</label>
              <input className={f.input} value={flag}
                onChange={e => setFlag(e.target.value)} placeholder="🇩🇪" />
            </div>
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('configuration.languages.displayName')}</label>
              <input className={f.input} required value={displayName}
                onChange={e => setDisplayName(e.target.value)} placeholder="German" />
              <span className={f.hint}>{t('configuration.languages.displayNameHint')}</span>
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('configuration.languages.nativeName')}</label>
              <input className={f.input} required value={nativeName}
                onChange={e => setNativeName(e.target.value)} placeholder="Deutsch" />
              <span className={f.hint}>{t('configuration.languages.nativeNameHint')}</span>
            </div>
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={f.label}>{t('configuration.languages.sortOrder')}</label>
              <input className={f.input} type="number" value={sortOrder}
                onChange={e => setSortOrder(parseInt(e.target.value, 10) || 0)} />
            </div>
            <div className={f.field}>
              <label className={f.label} style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 22 }}>
                <input type="checkbox" checked={enabled} disabled={editing?.isDefault}
                  onChange={e => setEnabled(e.target.checked)} />
                {t('configuration.languages.enabled')}
              </label>
            </div>
          </div>
        </form>
      </Modal>
    </>
  );
}
