import type { TopDevice } from '../../types/dashboard';
import styles from './TopDevicesTable.module.css';

interface TopDevicesTableProps {
  devices: TopDevice[];
}

export default function TopDevicesTable({ devices }: TopDevicesTableProps) {
  if (!devices || devices.length === 0) {
    return (
      <div className={styles.empty}>No devices with active alerts.</div>
    );
  }

  const maxCount = Math.max(...devices.map(d => d.alertCount), 1);

  return (
    <table className={styles.table} aria-label="Top devices by active alert count">
      <thead>
        <tr>
          <th className={styles.thRank}>#</th>
          <th className={styles.thDevice}>Device</th>
          <th className={styles.thMill}>Mill</th>
          <th className={styles.thBar}></th>
          <th className={styles.thCount}>Alerts</th>
        </tr>
      </thead>
      <tbody>
        {devices.map((device, index) => {
          const pct = (device.alertCount / maxCount) * 100;

          return (
            <tr key={device.deviceId} className={styles.row}>
              <td className={styles.tdRank}>{index + 1}</td>
              <td className={styles.tdDevice}>
                <span className={styles.deviceCode}>{device.deviceCode}</span>
                <span className={styles.deviceName}>{device.deviceName}</span>
              </td>
              <td className={styles.tdMill}>{device.millName}</td>
              <td className={styles.tdBar}>
                <div className={styles.barTrack} role="presentation">
                  <div
                    className={styles.barFill}
                    style={{ width: `${pct}%` }}
                  />
                </div>
              </td>
              <td className={styles.tdCount}>{device.alertCount}</td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
