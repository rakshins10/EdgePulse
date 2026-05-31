import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getMills, getAreas } from '../../api/organisation';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import styles from './AreasPage.module.css';

export default function AreasPage() {
  const [millFilter, setMillFilter] = useState('');

  const { data: mills = [] } = useQuery({ queryKey: ['mills'], queryFn: getMills });
  const { data: areas = [], isLoading } = useQuery({
    queryKey: ['areas', millFilter],
    queryFn:  () => getAreas(millFilter || undefined),
  });

  if (isLoading) return <LoadingSpinner message="Loading areas…" />;

  return (
    <>
      <div className={styles.toolbar}>
        <div className={styles.filters}>
          <span className={styles.filterLabel}>Filter:</span>
          <select
            className={styles.select}
            value={millFilter}
            onChange={e => setMillFilter(e.target.value)}
          >
            <option value="">All mills</option>
            {mills.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
          </select>
        </div>

        <div className={styles.count}>
          {areas.length} {areas.length === 1 ? 'area' : 'areas'}
        </div>
      </div>

      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Area</th>
              <th>Code</th>
              <th>Mill</th>
              <th>Location Type</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody>
            {areas.length === 0 ? (
              <tr>
                <td colSpan={5} className={styles.empty}>No areas found.</td>
              </tr>
            ) : (
              areas.map(area => (
                <tr key={area.id}>
                  <td className={styles.areaName}>{area.name}</td>
                  <td className={styles.areaCode}>{area.code}</td>
                  <td className={styles.location}>{area.millName}</td>
                  <td className={styles.location}>{area.locationTypeName ?? '—'}</td>
                  <td className={styles.location}>{area.description ?? '—'}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
