import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  ResponsiveContainer, BarChart, Bar, XAxis, YAxis, Tooltip, CartesianGrid,
} from 'recharts';
import { getEnergyReport, downloadEnergyCsv } from '../../api/energy';
import { useToast } from '../../context/ToastContext';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import styles from './EnergyPage.module.css';

function isoDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}

export default function EnergyPage() {
  const { t, i18n } = useTranslation();
  const toast = useToast();

  const [from, setFrom] = useState(() =>
    isoDate(new Date(Date.now() - 30 * 24 * 3600 * 1000)));
  const [to, setTo] = useState(() => isoDate(new Date()));

  const fromParam = `${from}T00:00:00Z`;
  const toParam = `${to}T23:59:59Z`;

  const { data, isLoading, isError } = useQuery({
    queryKey: ['energy', fromParam, toParam],
    queryFn: () => getEnergyReport(fromParam, toParam),
  });

  const nf = (v: number) => v.toLocaleString(i18n.language);

  async function handleCsv() {
    try {
      await downloadEnergyCsv(fromParam, toParam);
    } catch {
      toast.error(t('energy.exportError'));
    }
  }

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <div className={styles.field}>
          <label className={styles.label}>{t('reports.from')}</label>
          <input type="date" className={styles.dateInput} value={from} max={to}
            onChange={e => setFrom(e.target.value)} />
        </div>
        <div className={styles.field}>
          <label className={styles.label}>{t('reports.to')}</label>
          <input type="date" className={styles.dateInput} value={to} min={from}
            onChange={e => setTo(e.target.value)} />
        </div>
        <div className={styles.spacer} />
        <button className={styles.csvBtn} onClick={handleCsv}>
          {t('energy.exportCsv')}
        </button>
      </div>

      {isLoading && <LoadingSpinner message={t('common.loading')} />}
      {isError && <div className={styles.empty}>{t('energy.loadError')}</div>}

      {data && (
        <>
          <div className={styles.kpis}>
            <div className={styles.kpi}>
              <div className={styles.kpiLabel}>{t('energy.totalEnergy')}</div>
              <div className={styles.kpiValue}>
                {nf(data.totalEnergyKwh)}<span className={styles.kpiUnit}>kWh</span>
              </div>
            </div>
            <div className={styles.kpi}>
              <div className={styles.kpiLabel}>{t('energy.totalCo2')}</div>
              <div className={styles.kpiValue}>
                {nf(data.totalCo2Kg)}<span className={styles.kpiUnit}>kg CO₂e</span>
              </div>
              <div className={styles.kpiSub}>
                {t('energy.co2Factor', { factor: data.co2FactorKgPerKwh })}
              </div>
            </div>
            <div className={styles.kpi}>
              <div className={styles.kpiLabel}>{t('energy.meteredDevices')}</div>
              <div className={styles.kpiValue}>{data.meteredDeviceCount}</div>
              <div className={styles.kpiSub}>{t('energy.meteredHint')}</div>
            </div>
          </div>

          <section className={styles.card}>
            <h2 className={styles.cardTitle}>{t('energy.dailyTitle')}</h2>
            {data.daily.length === 0 ? (
              <div className={styles.empty}>{t('energy.empty')}</div>
            ) : (
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={data.daily} margin={{ top: 4, right: 8, left: 8, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" />
                  <XAxis dataKey="date" tick={{ fontSize: 11, fill: 'var(--color-text-muted)' }}
                    tickFormatter={(d: string) => d.slice(5)} />
                  <YAxis tick={{ fontSize: 11, fill: 'var(--color-text-muted)' }} width={70}
                    tickFormatter={(v: number) => v.toLocaleString(i18n.language)} />
                  <Tooltip
                    formatter={(value: number | string) =>
                      [`${Number(value).toLocaleString(i18n.language)} kWh`, t('energy.energy')]}
                    contentStyle={{
                      background: 'var(--color-surface)',
                      border: '1px solid var(--color-border)',
                      borderRadius: 8, fontSize: 12,
                    }}
                    labelStyle={{ color: 'var(--color-text)' }}
                  />
                  <Bar dataKey="energyKwh" fill="var(--color-accent)" radius={[3, 3, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </section>

          <section className={styles.card}>
            <h2 className={styles.cardTitle}>{t('energy.deviceTitle')}</h2>
            {data.devices.length === 0 ? (
              <div className={styles.empty}>{t('energy.empty')}</div>
            ) : (
              <>
                <table className={styles.table}>
                  <thead>
                    <tr>
                      <th>{t('energy.device')}</th>
                      <th>{t('energy.avgPower')}</th>
                      <th>{t('energy.energyKwh')}</th>
                      <th>{t('energy.co2Kg')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.devices.map(d => (
                      <tr key={d.deviceId}>
                        <td>
                          <span className={styles.deviceName}>{d.deviceName}</span>
                          <span className={styles.deviceSub}>
                            {d.deviceCode} · {d.millName}
                          </span>
                        </td>
                        <td>{nf(d.avgPowerKw)} kW</td>
                        <td>{nf(d.energyKwh)}</td>
                        <td>{nf(d.co2Kg)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <p className={styles.note}>{t('energy.methodNote')}</p>
              </>
            )}
          </section>
        </>
      )}
    </div>
  );
}
