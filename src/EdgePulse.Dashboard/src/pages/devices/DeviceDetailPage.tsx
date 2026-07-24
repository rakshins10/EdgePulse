import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { getDevices } from '../../api/devices';
import { getDeviceTelemetry } from '../../api/telemetry';
import Badge from '../../components/common/Badge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import TelemetryChartCard from '../../components/common/TelemetryChartCard';
import TimeRangeToolbar from '../../components/common/TimeRangeToolbar';
import AttachmentsCard from '../../components/common/AttachmentsCard';
import { useTimeRange } from '../../hooks/useTimeRange';
import styles from './DeviceDetailPage.module.css';

const CHART_COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4'];

export default function DeviceDetailPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();

  const globalRange = useTimeRange();
  const { from, to, isLive } = globalRange;

  const { data: devices = [], isLoading: devicesLoading } = useQuery({
    queryKey: ['devices'],
    queryFn: () => getDevices({}),
  });

  const device = devices.find(d => d.id === id);

  const { data: telemetry, isLoading: telLoading, error: telError } = useQuery({
    queryKey: ['telemetry', id, from.toISOString(), to.toISOString()],
    queryFn: () => getDeviceTelemetry(id!, {
      from:  from.toISOString(),
      to:    to.toISOString(),
      limit: 1000,
    }),
    refetchInterval: isLive ? 10_000 : false,
    enabled: !!id,
  });

  if (devicesLoading) return <LoadingSpinner message={t('common.loading')} />;

  if (!device) {
    return (
      <div className={styles.noDevice}>
        <p>{t('devices.detail.notFound')}</p>
        <Link to="/devices">{t('devices.detail.backLink')}</Link>
      </div>
    );
  }

  const metricKeys = telemetry
    ? Array.from(new Set(telemetry.readings.flatMap(r => r.metrics.map(m => m.key))))
    : [];

  return (
    <div className={styles.page}>
      <nav className={styles.breadcrumb}>
        <Link to="/devices">{t('devices.detail.backToDevices')}</Link>
        <span>›</span>
        <span>{device.name}</span>
      </nav>

      <div className={styles.header}>
        <div className={styles.titleGroup}>
          <h1 className={styles.deviceName}>{device.name}</h1>
          <div className={styles.deviceCode}>{device.code}</div>
        </div>
        <Badge label={device.statusName} color={device.statusColor} variant="status" />
      </div>

      <div className={styles.infoGrid}>
        <div className={styles.infoItem}>
          <div className={styles.infoLabel}>{t('devices.detail.type')}</div>
          <div className={styles.infoValue}>{device.typeName}</div>
        </div>
        <div className={styles.infoItem}>
          <div className={styles.infoLabel}>{t('devices.detail.mill')}</div>
          <div className={styles.infoValue}>{device.millName}</div>
        </div>
        <div className={styles.infoItem}>
          <div className={styles.infoLabel}>{t('devices.detail.area')}</div>
          <div className={styles.infoValue}>{device.areaName}</div>
        </div>
        {device.serialNumber && (
          <div className={styles.infoItem}>
            <div className={styles.infoLabel}>{t('devices.detail.serial')}</div>
            <div className={styles.infoValue}>{device.serialNumber}</div>
          </div>
        )}
      </div>

      <AttachmentsCard entityType="Device" entityId={id!} />

      <div>
        <div className={styles.stickyToolbar}>
          <h2 className={styles.sectionTitle}>
            {t('devices.detail.liveTelemetry')}
            {isLive && <span className={styles.refreshNote}>{t('devices.detail.autoRefresh')}</span>}
          </h2>

          <div className={styles.globalToolbarWrap}>
            <div className={styles.globalToolbarLabel}>
              <span className={styles.globalIcon}>⊞</span>
              {t('devices.detail.globalRange')}
              <span className={styles.globalHint}>{t('devices.detail.globalRangeHint')}</span>
            </div>
            <TimeRangeToolbar range={globalRange} />
          </div>
        </div>

        {telLoading && <LoadingSpinner message={t('devices.detail.loadingTelemetry')} />}

        {telError && (
          <div className={styles.empty}>{t('devices.detail.telemetryError')}</div>
        )}

        {!telLoading && !telError && metricKeys.length === 0 && (
          <div className={styles.empty}>
            {t('devices.detail.noTelemetryPeriod')}<br />
            {t('devices.detail.noTelemetryHint')}
          </div>
        )}

        {metricKeys.map((key, i) => (
          <TelemetryChartCard
            key={key}
            metricKey={key}
            deviceId={id!}
            color={CHART_COLORS[i % CHART_COLORS.length]}
            globalFrom={from}
            globalTo={to}
            globalReadings={telemetry?.readings ?? []}
            globalLoading={telLoading}
          />
        ))}
      </div>
    </div>
  );
}
