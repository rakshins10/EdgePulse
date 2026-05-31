import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getDeviceTelemetry } from '../../api/telemetry';
import { useTimeRange } from '../../hooks/useTimeRange';
import TelemetryChart from './TelemetryChart';
import TimeRangeToolbar from './TimeRangeToolbar';
import LoadingSpinner from './LoadingSpinner';
import type { TelemetryReadingDto } from '../../types/api';
import styles from './TelemetryChartCard.module.css';

interface Props {
  metricKey:      string;
  deviceId:       string;
  color:          string;
  globalFrom:     Date;
  globalTo:       Date;
  globalReadings: TelemetryReadingDto[];
  globalLoading:  boolean;
}

export default function TelemetryChartCard({
  metricKey,
  deviceId,
  color,
  globalFrom,
  globalTo,
  globalReadings,
  globalLoading,
}: Props) {
  const [overrideActive, setOverrideActive] = useState(false);
  const [toolbarOpen,    setToolbarOpen]    = useState(false);

  const chartRange = useTimeRange();
  const isLive = chartRange.scale !== 'custom' && chartRange.to >= new Date();

  const { data: chartData, isLoading: chartLoading } = useQuery({
    queryKey: ['telemetry', deviceId, metricKey, chartRange.from.toISOString(), chartRange.to.toISOString()],
    queryFn: () =>
      getDeviceTelemetry(deviceId, {
        from:  chartRange.from.toISOString(),
        to:    chartRange.to.toISOString(),
        limit: 1000,
      }),
    enabled: overrideActive,
    refetchInterval: overrideActive && isLive ? 10_000 : false,
  });

  const activeFrom = overrideActive ? chartRange.from : globalFrom;
  const activeTo   = overrideActive ? chartRange.to   : globalTo;

  const readings = overrideActive ? (chartData?.readings ?? []) : globalReadings;
  const loading  = overrideActive ? chartLoading : globalLoading;

  function handleToggleOverride() {
    if (overrideActive) {
      setOverrideActive(false);
      setToolbarOpen(false);
    } else {
      setOverrideActive(true);
      setToolbarOpen(true);
    }
  }

  return (
    <div className={`${styles.card} ${overrideActive ? styles.overrideActive : ''}`}>
      <div className={styles.cardHeader}>
        <span className={styles.rangeTag}>
          {overrideActive ? 'Custom range' : 'Global range'}
        </span>

        <button
          className={`${styles.overrideBtn} ${overrideActive ? styles.overrideBtnActive : ''}`}
          onClick={handleToggleOverride}
          title={overrideActive ? 'Reset to global range' : 'Set a custom range for this chart'}
        >
          {overrideActive ? '↩ Reset to global' : '⊞ Custom range'}
        </button>
      </div>

      {overrideActive && toolbarOpen && (
        <div className={styles.toolbarWrap}>
          <TimeRangeToolbar range={chartRange} compact />
        </div>
      )}

      {loading
        ? <LoadingSpinner message="Loading…" />
        : <TelemetryChart
            metricKey={metricKey}
            readings={readings}
            color={color}
            from={activeFrom}
            to={activeTo}
          />
      }
    </div>
  );
}
