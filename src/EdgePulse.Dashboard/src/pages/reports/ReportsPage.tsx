import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getMillComparison, downloadMillComparisonCsv, downloadAlertsCsv,
} from '../../api/reports';
import { useToast } from '../../context/ToastContext';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import styles from './ReportsPage.module.css';

function isoDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}

export default function ReportsPage() {
  const { t, i18n } = useTranslation();
  const toast = useToast();

  const [from, setFrom] = useState(() =>
    isoDate(new Date(Date.now() - 30 * 24 * 3600 * 1000)));
  const [to, setTo] = useState(() => isoDate(new Date()));

  // Send the whole end day (inclusive) to the API
  const fromParam = `${from}T00:00:00Z`;
  const toParam = `${to}T23:59:59Z`;

  const { data, isLoading, isError } = useQuery({
    queryKey: ['reports', 'mill-comparison', fromParam, toParam],
    queryFn: () => getMillComparison(fromParam, toParam),
  });

  async function handleCsv(fn: (f: string, t: string) => Promise<void>) {
    try {
      await fn(fromParam, toParam);
    } catch {
      toast.error(t('reports.exportError'));
    }
  }

  const fmt = (v: number | null) =>
    v === null ? '—' : v.toLocaleString(i18n.language);

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <div className={styles.field}>
          <label className={styles.label}>{t('reports.from')}</label>
          <input
            type="date" className={styles.dateInput} value={from} max={to}
            onChange={e => setFrom(e.target.value)}
          />
        </div>
        <div className={styles.field}>
          <label className={styles.label}>{t('reports.to')}</label>
          <input
            type="date" className={styles.dateInput} value={to} min={from}
            onChange={e => setTo(e.target.value)}
          />
        </div>
        <div className={styles.spacer} />
        <button className={styles.csvBtn}
          onClick={() => handleCsv(downloadMillComparisonCsv)}>
          {t('reports.exportComparison')}
        </button>
        <button className={styles.csvBtn}
          onClick={() => handleCsv(downloadAlertsCsv)}>
          {t('reports.exportAlerts')}
        </button>
      </div>

      <section className={styles.card}>
        <h2 className={styles.cardTitle}>{t('reports.comparisonTitle')}</h2>
        <p className={styles.cardSub}>{t('reports.comparisonSub')}</p>

        {isLoading && <LoadingSpinner message={t('common.loading')} />}
        {isError && <div className={styles.empty}>{t('reports.loadError')}</div>}

        {data && data.mills.length === 0 && (
          <div className={styles.empty}>{t('reports.empty')}</div>
        )}

        {data && data.mills.length > 0 && (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t('reports.mill')}</th>
                <th>{t('reports.devices')}</th>
                <th>{t('reports.alerts')}</th>
                <th>{t('reports.open')}</th>
                <th>{t('reports.critical')}</th>
                <th>{t('reports.high')}</th>
                <th>{t('reports.mtta')}</th>
                <th>{t('reports.mttr')}</th>
              </tr>
            </thead>
            <tbody>
              {data.mills.map(m => (
                <tr key={m.millId}>
                  <td>
                    <span className={styles.millName}>{m.millName}</span>
                    <span className={styles.millLoc}>{m.location}</span>
                  </td>
                  <td>{m.deviceCount}</td>
                  <td>{m.totalAlerts}</td>
                  <td>{m.openAlerts}</td>
                  <td className={m.criticalAlerts > 0 ? styles.critical : styles.muted}>
                    {m.criticalAlerts}
                  </td>
                  <td className={m.highAlerts > 0 ? styles.high : styles.muted}>
                    {m.highAlerts}
                  </td>
                  <td className={styles.muted}>{fmt(m.avgAcknowledgeMinutes)}</td>
                  <td className={styles.muted}>{fmt(m.avgResolveMinutes)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  );
}
