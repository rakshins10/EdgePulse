import { useQuery } from '@tanstack/react-query';
import { fetchDashboardSummary } from '../api/dashboard';
import KpiTile from '../components/dashboard/KpiTile';
import AlertTrendChart from '../components/dashboard/AlertTrendChart';
import SeverityChart from '../components/dashboard/SeverityChart';
import TopDevicesTable from '../components/dashboard/TopDevicesTable';
import styles from './DashboardPage.module.css';

const REFETCH_INTERVAL = 60_000; // 1 minute

export default function DashboardPage() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: fetchDashboardSummary,
    staleTime: 30_000,
    refetchInterval: REFETCH_INTERVAL,
  });

  if (isLoading) {
    return (
      <div className={styles.state}>
        <span className={styles.spinner} aria-hidden="true" />
        Loading dashboard…
      </div>
    );
  }

  if (isError) {
    return (
      <div className={`${styles.state} ${styles.error}`}>
        Failed to load dashboard data.{' '}
        {error instanceof Error ? error.message : 'Unknown error.'}
      </div>
    );
  }

  if (!data) return null;

  const devicesAtRiskPct =
    data.totalDevices > 0
      ? Math.round((data.devicesWithAlerts / data.totalDevices) * 100)
      : 0;

  return (
    <div className={styles.page}>
      {/* ── KPI Row ─────────────────────────────────────────────────── */}
      <section className={styles.kpiRow} aria-label="Key performance indicators">
        <KpiTile
          label="Total Devices"
          value={data.totalDevices}
          sub="Registered in scope"
          accent="default"
        />
        <KpiTile
          label="Open Alerts"
          value={data.openAlerts}
          sub="Open + Acknowledged"
          accent={data.openAlerts > 0 ? 'warning' : 'ok'}
        />
        <KpiTile
          label="Critical Alerts"
          value={data.criticalOpenAlerts}
          sub="Requires immediate action"
          accent={data.criticalOpenAlerts > 0 ? 'critical' : 'ok'}
        />
        <KpiTile
          label="Devices at Risk"
          value={data.devicesWithAlerts}
          sub={`${devicesAtRiskPct}% of fleet`}
          accent={data.devicesWithAlerts > 0 ? 'warning' : 'ok'}
        />
      </section>

      {/* ── Charts Row ──────────────────────────────────────────────── */}
      <section className={styles.chartsRow}>
        {/* 7-day trend — wider panel */}
        <div className={styles.card}>
          <h2 className={styles.cardTitle}>Alert Trend — Last 7 Days</h2>
          <AlertTrendChart data={data.alertTrend} />
        </div>

        {/* Severity breakdown — narrower panel */}
        <div className={styles.card}>
          <h2 className={styles.cardTitle}>Active Alerts by Severity</h2>
          <SeverityChart data={data.bySeverity} />
        </div>
      </section>

      {/* ── Top Devices ─────────────────────────────────────────────── */}
      <section className={styles.card}>
        <h2 className={styles.cardTitle}>Top 5 Devices by Active Alert Count</h2>
        <TopDevicesTable devices={data.topDevices} />
      </section>
    </div>
  );
}
