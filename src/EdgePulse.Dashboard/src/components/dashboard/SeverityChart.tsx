import type { SeverityCount } from '../../types/dashboard';
import styles from './SeverityChart.module.css';

interface SeverityChartProps {
  data: SeverityCount[];
}

const SEVERITY_META: Record<string, { label: string; colorClass: string }> = {
  CRITICAL: { label: 'Critical', colorClass: styles.critical },
  HIGH:     { label: 'High',     colorClass: styles.high     },
  MEDIUM:   { label: 'Medium',   colorClass: styles.medium   },
  LOW:      { label: 'Low',      colorClass: styles.low      },
};

export default function SeverityChart({ data }: SeverityChartProps) {
  const total = data.reduce((sum, d) => sum + d.count, 0);

  if (total === 0) {
    return (
      <div className={styles.empty}>
        No active alerts — all clear.
      </div>
    );
  }

  return (
    <ul className={styles.list} role="list" aria-label="Active alerts by severity">
      {data.map(({ severityCode, count }) => {
        const meta = SEVERITY_META[severityCode];
        if (!meta) return null;

        const pct = total > 0 ? (count / total) * 100 : 0;

        return (
          <li key={severityCode} className={styles.row}>
            <span className={styles.severityLabel}>{meta.label}</span>
            <div className={styles.barTrack} role="presentation">
              <div
                className={`${styles.barFill} ${meta.colorClass}`}
                style={{ width: `${pct}%` }}
              />
            </div>
            <span className={styles.count}>{count}</span>
          </li>
        );
      })}
    </ul>
  );
}
