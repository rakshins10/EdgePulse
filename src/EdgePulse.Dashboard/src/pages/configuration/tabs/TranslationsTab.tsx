import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getLocales, getLookupTranslations, upsertLookupTranslation,
} from '../../../api/localization';
import LoadingSpinner from '../../../components/common/LoadingSpinner';
import styles from './LookupTable.module.css';
import f from '../../../components/common/FormField.module.css';

const LOOKUP_TYPES = [
  'DeviceType', 'DeviceStatus', 'LocationType',
  'MaintenanceType', 'MetricType', 'AlertSeverity',
] as const;

type SaveState = 'idle' | 'saving' | 'saved' | 'error';

export default function TranslationsTab() {
  const { t } = useTranslation();
  const qc = useQueryClient();

  const [lookupType, setLookupType] = useState<string>('DeviceType');
  const [locale, setLocale] = useState<string>('');

  // Locales for the selector — exclude the default (English source needs no translation).
  const { data: locales = [] } = useQuery({
    queryKey: ['locales', 'all'],
    queryFn: () => getLocales(false),
  });

  const translatableLocales = locales.filter(l => !l.isDefault);

  // Default the locale selector to the first non-default locale once loaded.
  const effectiveLocale = locale || translatableLocales[0]?.code || '';

  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['translations', lookupType, effectiveLocale],
    queryFn: () => getLookupTranslations(lookupType, effectiveLocale),
    enabled: !!effectiveLocale,
  });

  // Per-row local edit + save state
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [saveState, setSaveState] = useState<Record<string, SaveState>>({});

  function valueFor(itemId: string, translated: string | null): string {
    return drafts[itemId] !== undefined ? drafts[itemId] : (translated ?? '');
  }

  async function commit(itemId: string, original: string | null) {
    const value = drafts[itemId];
    if (value === undefined || value === (original ?? '')) return; // no change

    setSaveState(s => ({ ...s, [itemId]: 'saving' }));
    try {
      await upsertLookupTranslation({
        lookupType,
        itemId,
        localeCode: effectiveLocale,
        name: value.trim() || undefined,
      });
      setSaveState(s => ({ ...s, [itemId]: 'saved' }));
      // Refresh server-resolved names elsewhere in the app.
      await qc.invalidateQueries({ queryKey: ['translations', lookupType, effectiveLocale] });
      setTimeout(() => setSaveState(s => ({ ...s, [itemId]: 'idle' })), 1500);
    } catch {
      setSaveState(s => ({ ...s, [itemId]: 'error' }));
    }
  }

  return (
    <>
      <div className={styles.toolbar}>
        <div style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap' }}>
          <label className={f.label} style={{ margin: 0 }}>{t('configuration.translations.lookupType')}</label>
          <select className={f.select} style={{ width: 'auto' }} value={lookupType}
            onChange={e => { setLookupType(e.target.value); setDrafts({}); setSaveState({}); }}>
            {LOOKUP_TYPES.map(lt => (
              <option key={lt} value={lt}>{t(`configuration.translations.lookupTypes.${lt}`)}</option>
            ))}
          </select>

          <label className={f.label} style={{ margin: 0 }}>{t('configuration.translations.locale')}</label>
          <select className={f.select} style={{ width: 'auto' }} value={effectiveLocale}
            onChange={e => { setLocale(e.target.value); setDrafts({}); setSaveState({}); }}>
            {translatableLocales.map(l => (
              <option key={l.code} value={l.code}>{l.flag ? `${l.flag} ` : ''}{l.nativeName}</option>
            ))}
          </select>
        </div>
      </div>

      <p style={{ padding: '0 20px 12px', fontSize: 12, color: 'var(--color-text-muted)' }}>
        {t('configuration.translations.hint')}
      </p>

      {!effectiveLocale ? (
        <div className={styles.empty}>{t('configuration.translations.noItems')}</div>
      ) : isLoading ? (
        <LoadingSpinner message={t('common.loading')} />
      ) : rows.length === 0 ? (
        <div className={styles.empty}>{t('configuration.translations.noItems')}</div>
      ) : (
        <table className={styles.table}>
          <thead>
            <tr>
              <th style={{ width: '40%' }}>{t('configuration.translations.sourceName')}</th>
              <th>{t('configuration.translations.translation')}</th>
              <th style={{ width: 90 }} />
            </tr>
          </thead>
          <tbody>
            {rows.map(row => {
              const st = saveState[row.itemId] ?? 'idle';
              return (
                <tr key={row.itemId}>
                  <td>{row.sourceName}</td>
                  <td>
                    <input
                      className={f.input}
                      value={valueFor(row.itemId, row.translatedName)}
                      placeholder={t('configuration.translations.placeholder')}
                      onChange={e => setDrafts(d => ({ ...d, [row.itemId]: e.target.value }))}
                      onBlur={() => commit(row.itemId, row.translatedName)}
                      onKeyDown={e => { if (e.key === 'Enter') (e.target as HTMLInputElement).blur(); }}
                    />
                  </td>
                  <td className={styles.muted}>
                    {st === 'saving' && '…'}
                    {st === 'saved' && <span style={{ color: 'var(--color-low)' }}>✓ {t('configuration.translations.saved')}</span>}
                    {st === 'error' && <span style={{ color: 'var(--color-critical)' }}>{t('configuration.translations.saveError')}</span>}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </>
  );
}
