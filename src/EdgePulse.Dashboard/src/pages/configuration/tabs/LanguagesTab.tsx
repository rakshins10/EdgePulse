import { useState, useRef, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getLocales, createLocale, updateLocale, deleteLocale, setDefaultLocale,
  getLookupSourceItems, bulkUpsertLookupTranslations, bulkUpsertUiStrings,
  type LocaleDto,
} from '../../../api/localization';
import {
  englishUiStrings, buildExportCsv, splitImportRows,
} from '../../../i18n/translationTools';
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
  const [prefill, setPrefill] = useState(true);

  // Import / Export panel state
  const [ioLocale, setIoLocale] = useState('');
  const [ioBusy, setIoBusy]     = useState(false);
  const [ioMsg, setIoMsg]       = useState<string | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  function refresh() {
    return Promise.all([
      qc.invalidateQueries({ queryKey: ['locales', 'all'] }),
      qc.invalidateQueries({ queryKey: ['locales', 'enabled'] }),
    ]);
  }

  // Copy English values (UI keys + lookup item names) into a locale as a
  // starting point. Used on create when "pre-fill" is checked.
  async function prefillFromEnglish(localeCode: string): Promise<number> {
    const ui = englishUiStrings();
    const uiEntries = Object.entries(ui).map(([key, value]) => ({ key, value }));
    const uiRes = await bulkUpsertUiStrings(localeCode, uiEntries);

    const items = await getLookupSourceItems();
    const lookupEntries = items.map(i => ({
      lookupType: i.lookupType, itemId: i.itemId, name: i.sourceName,
    }));
    const lookupRes = await bulkUpsertLookupTranslations(localeCode, lookupEntries);

    return uiRes.affected + lookupRes.affected;
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
        if (prefill) {
          setIoMsg(t('configuration.languages.prefilling'));
          const count = await prefillFromEnglish(code.trim().toLowerCase());
          setIoMsg(t('configuration.languages.prefillDone', { count }));
        }
      }
      await refresh();
      setOpen(false);
    } catch {
      setError(editing ? t('configuration.languages.errorUpdate') : t('configuration.languages.errorCreate'));
    } finally { setSaving(false); }
  }

  // ── Export / Import ────────────────────────────────────────────────────────
  async function handleExport() {
    if (!ioLocale) return;
    setIoBusy(true); setIoMsg(null);
    try {
      const csv = await buildExportCsv(ioLocale);
      const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `edgepulse-translations-${ioLocale}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      setIoMsg(t('configuration.languages.exportError'));
    } finally { setIoBusy(false); }
  }

  async function handleImportFile(file: File) {
    if (!ioLocale) return;
    setIoBusy(true); setIoMsg(null);
    try {
      const text = await file.text();
      const { ui, lookups, skipped } = splitImportRows(text);
      const uiRes = ui.length ? await bulkUpsertUiStrings(ioLocale, ui) : { affected: 0 };
      const lookupRes = lookups.length
        ? await bulkUpsertLookupTranslations(ioLocale, lookups) : { affected: 0 };
      setIoMsg(t('configuration.languages.importDone', {
        ui: uiRes.affected, lookup: lookupRes.affected, skipped,
      }));
      await qc.invalidateQueries();
    } catch {
      setIoMsg(t('configuration.languages.importError'));
    } finally {
      setIoBusy(false);
      if (fileRef.current) fileRef.current.value = '';
    }
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

      {/* Import / Export panel */}
      <div className={styles.ioPanel}>
        <div className={styles.ioHeader}>{t('configuration.languages.ioTitle')}</div>
        <p className={styles.ioHint}>{t('configuration.languages.ioHint')}</p>
        <div className={styles.ioRow}>
          <select
            className={f.select}
            style={{ width: 'auto' }}
            value={ioLocale}
            onChange={e => { setIoLocale(e.target.value); setIoMsg(null); }}
          >
            <option value="">—</option>
            {data.map(l => (
              <option key={l.code} value={l.code}>
                {l.flag ? `${l.flag} ` : ''}{l.nativeName} ({l.code})
              </option>
            ))}
          </select>
          <button className={styles.editBtn} disabled={!ioLocale || ioBusy} onClick={handleExport}>
            {ioBusy ? t('configuration.languages.exporting') : t('configuration.languages.exportBtn')}
          </button>
          <button className={styles.editBtn} disabled={!ioLocale || ioBusy} onClick={() => fileRef.current?.click()}>
            {ioBusy ? t('configuration.languages.importing') : t('configuration.languages.importBtn')}
          </button>
          <input
            ref={fileRef}
            type="file"
            accept=".csv,text/csv"
            style={{ display: 'none' }}
            onChange={e => { const file = e.target.files?.[0]; if (file) void handleImportFile(file); }}
          />
          {ioMsg && <span className={styles.ioMsg}>{ioMsg}</span>}
        </div>
      </div>

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
          {!editing && (
            <div className={f.field}>
              <label className={f.label} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <input type="checkbox" checked={prefill}
                  onChange={e => setPrefill(e.target.checked)} />
                {t('configuration.languages.prefill')}
              </label>
            </div>
          )}
        </form>
      </Modal>
    </>
  );
}
