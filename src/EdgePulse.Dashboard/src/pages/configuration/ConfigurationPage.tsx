import { useState } from 'react';
import LocationTypesTab from './tabs/LocationTypesTab';
import MaintenanceTypesTab from './tabs/MaintenanceTypesTab';
import MetricTypesTab from './tabs/MetricTypesTab';
import DeviceTypesTab from './tabs/DeviceTypesTab';
import DeviceStatusesTab from './tabs/DeviceStatusesTab';
import styles from './ConfigurationPage.module.css';

type TabKey = 'location-types' | 'maintenance-types' | 'metric-types' | 'device-types' | 'device-statuses';

const TABS: { key: TabKey; label: string; hint?: string }[] = [
  { key: 'location-types',    label: 'Location Types',    hint: 'Building, Floor, Production Line, Section…' },
  { key: 'device-types',      label: 'Device Types',      hint: 'Pump, Motor, Valve, Sensor…' },
  { key: 'device-statuses',   label: 'Device Statuses',   hint: 'Online, Offline, Maintenance, Decommissioned' },
  { key: 'maintenance-types', label: 'Maintenance Types', hint: 'Preventive, Corrective, Inspection…' },
  { key: 'metric-types',      label: 'Metric Types',      hint: 'Temperature, Pressure, Vibration, Flow rate…' },
];

export default function ConfigurationPage() {
  const [tab, setTab] = useState<TabKey>('location-types');
  const active = TABS.find(t => t.key === tab)!;

  return (
    <div>
      <div className={styles.tabs}>
        {TABS.map(t => (
          <button
            key={t.key}
            className={`${styles.tab} ${tab === t.key ? styles.tabActive : ''}`}
            onClick={() => setTab(t.key)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {active.hint && <p className={styles.hint}>{active.hint}</p>}

      <div className={styles.body}>
        {tab === 'location-types'    && <LocationTypesTab />}
        {tab === 'maintenance-types' && <MaintenanceTypesTab />}
        {tab === 'metric-types'      && <MetricTypesTab />}
        {tab === 'device-types'      && <DeviceTypesTab />}
        {tab === 'device-statuses'   && <DeviceStatusesTab />}
      </div>
    </div>
  );
}
