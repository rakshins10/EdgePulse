import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import LocationTypesTab from './tabs/LocationTypesTab';
import MaintenanceTypesTab from './tabs/MaintenanceTypesTab';
import MetricTypesTab from './tabs/MetricTypesTab';
import DeviceTypesTab from './tabs/DeviceTypesTab';
import DeviceStatusesTab from './tabs/DeviceStatusesTab';
import styles from './ConfigurationPage.module.css';

type TabKey = 'location-types' | 'maintenance-types' | 'metric-types' | 'device-types' | 'device-statuses';

const TABS: { key: TabKey; labelKey: string; hintKey: string }[] = [
  { key: 'location-types',    labelKey: 'configuration.tabs.locationTypes',    hintKey: 'configuration.hints.locationTypes' },
  { key: 'device-types',      labelKey: 'configuration.tabs.deviceTypes',      hintKey: 'configuration.hints.deviceTypes' },
  { key: 'device-statuses',   labelKey: 'configuration.tabs.deviceStatuses',   hintKey: 'configuration.hints.deviceStatuses' },
  { key: 'maintenance-types', labelKey: 'configuration.tabs.maintenanceTypes', hintKey: 'configuration.hints.maintenanceTypes' },
  { key: 'metric-types',      labelKey: 'configuration.tabs.metricTypes',      hintKey: 'configuration.hints.metricTypes' },
];

export default function ConfigurationPage() {
  const { t } = useTranslation();
  const [tab, setTab] = useState<TabKey>('location-types');
  const active = TABS.find(tt => tt.key === tab)!;

  return (
    <div>
      <div className={styles.tabs}>
        {TABS.map(tt => (
          <button
            key={tt.key}
            className={`${styles.tab} ${tab === tt.key ? styles.tabActive : ''}`}
            onClick={() => setTab(tt.key)}
          >
            {t(tt.labelKey)}
          </button>
        ))}
      </div>

      <p className={styles.hint}>{t(active.hintKey)}</p>

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
