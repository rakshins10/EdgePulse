import styles from './KpiTile.module.css';

interface KpiTileProps {
  label: string;
  value: number | string;
  /** Optional sublabel below the value */
  sub?: string;
  /** Visual accent: "default" | "critical" | "warning" | "ok" */
  accent?: 'default' | 'critical' | 'warning' | 'ok';
}

export default function KpiTile({
  label,
  value,
  sub,
  accent = 'default',
}: KpiTileProps) {
  return (
    <div className={`${styles.tile} ${styles[accent]}`}>
      <span className={styles.label}>{label}</span>
      <span className={styles.value}>{value}</span>
      {sub && <span className={styles.sub}>{sub}</span>}
    </div>
  );
}
