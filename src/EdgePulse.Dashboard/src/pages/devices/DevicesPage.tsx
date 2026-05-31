import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getDevices } from '../../api/devices';
import { getMills, getAreas } from '../../api/organisation';
import Badge from '../../components/common/Badge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import styles from './DevicesPage.module.css';

export default function DevicesPage() {
  const navigate = useNavigate();
  const [millFilter, setMillFilter] = useState('');
  const [areaFilter, setAreaFilter] = useState('');

  const { data: mills = [] } = useQuery({ queryKey: ['mills'], queryFn: getMills });
  const { data: areas = [] } = useQuery({
    queryKey: ['areas', millFilter],
    queryFn:  () => getAreas(millFilter || undefined),
  });

  const { data: devices = [], isLoading } = useQuery({
    queryKey: ['devices', millFilter, areaFilter],
    queryFn:  () => getDevices({
      millId: millFilter || undefined,
      areaId: areaFilter || undefined,
    }),
  });

  if (isLoading) return <LoadingSpinner message="Loading devices…" />;

  return (
    <>
      <div className={styles.toolbar}>
        <div className={styles.filters}>
          <span className={styles.filterLabel}>Filter:</span>
          <select
            className={styles.select}
            value={millFilter}
            onChange={e => { setMillFilter(e.target.value); setAreaFilter(''); }}
          >
            <option value="">All mills</option>
            {mills.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
          </select>
          <select
            className={styles.select}
            value={areaFilter}
            onChange={e => setAreaFilter(e.target.value)}
            disabled={!millFilter}
          >
            <option value="">All areas</option>
            {areas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
          </select>
        </div>

        <div className={styles.count}>
          {devices.length} {devices.length === 1 ? 'device' : 'devices'}
        </div>
      </div>

      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Device</th>
              <th>Type</th>
              <th>Status</th>
              <th>Mill</th>
              <th>Area</th>
            </tr>
          </thead>
          <tbody>
            {devices.length === 0 ? (
              <tr>
                <td colSpan={5} className={styles.empty}>No devices found.</td>
              </tr>
            ) : (
              devices.map(device => (
                <tr
                  key={device.id}
                  className={styles.clickableRow}
                  onClick={() => navigate(`/devices/${device.id}`)}
                  title="Click to view telemetry"
                >
                  <td>
                    <div className={styles.deviceName}>{device.name}</div>
                    <div className={styles.deviceCode}>{device.code}</div>
                  </td>
                  <td>{device.typeName}</td>
                  <td>
                    <Badge label={device.statusName} color={device.statusColor} variant="status" />
                  </td>
                  <td className={styles.location}>{device.millName}</td>
                  <td className={styles.location}>{device.areaName}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
