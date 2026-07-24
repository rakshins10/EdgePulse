import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { getDeviceHealth } from '../../api/health';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import styles from './HealthPage.module.css';

export default function HealthPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const { data: devices = [], isLoading, isError } = useQuery({
    queryKey: ['device-health'],
    queryFn: getDeviceHealth,
    refetchInterval: 60_000,
  });

  if (isLoading) return <LoadingSpinner message={t('common.loading')} />;

  const rulClass = (days: number | null) =>
    days === null ? '' : days <= 7 ? styles.rulSoon : days <= 30 ? styles.rulWarn : '';

  return (
    <div className={styles.page}>
      <p className={styles.hint}>{t('health.hint')}</p>

      <div className={styles.card}>
        {isError ? (
          <div className={styles.empty}>{t('health.loadError')}</div>
        ) : devices.length === 0 ? (
          <div className={styles.empty}>{t('health.empty')}</div>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t('health.device')}</th>
                <th className={styles.scoreCell}>{t('health.score')}</th>
                <th>{t('health.grade')}</th>
                <th>{t('health.openAlerts')}</th>
                <th>{t('health.watchMetric')}</th>
                <th>{t('health.daysToThreshold')}</th>
              </tr>
            </thead>
            <tbody>
              {devices.map(d => (
                <tr key={d.deviceId} onClick={() => navigate(`/devices/${d.deviceId}`)}
                  title={t('health.rowTooltip')}>
                  <td>
                    <span className={styles.deviceName}>{d.deviceName}</span>
                    <span className={styles.deviceSub}>{d.deviceCode} · {d.millName}</span>
                  </td>
                  <td className={styles.scoreCell}>
                    <div className={styles.scoreRow}>
                      <span className={styles.scoreValue}>{d.score}</span>
                      <div className={styles.scoreBar}>
                        <div
                          className={`${styles.scoreFill} ${styles[`fill${d.grade}` as keyof typeof styles] ?? ''}`}
                          style={{ width: `${d.score}%` }}
                        />
                      </div>
                    </div>
                  </td>
                  <td>
                    <span className={`${styles.chip} ${styles[`g${d.grade}` as keyof typeof styles] ?? ''}`}>
                      {t(`health.grades.${d.grade}`)}
                    </span>
                  </td>
                  <td className={styles.muted}>{d.openAlerts || '—'}</td>
                  <td>
                    {d.worstMetric ? (
                      <>
                        <span className={styles.metricKey}>{d.worstMetric.metricKey}</span>
                        <span className={styles.deviceSub}>
                          {t('health.utilization', {
                            avg: d.worstMetric.recentAverage,
                            pct: d.worstMetric.utilizationPercent,
                          })}
                        </span>
                      </>
                    ) : <span className={styles.muted}>—</span>}
                  </td>
                  <td className={rulClass(d.worstMetric?.daysToThreshold ?? null)}>
                    {d.worstMetric?.daysToThreshold != null
                      ? t('health.days', { count: Math.ceil(d.worstMetric.daysToThreshold) })
                      : <span className={styles.muted}>{t('health.stable')}</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
      <p className={styles.hint}>{t('health.methodNote')}</p>
    </div>
  );
}
