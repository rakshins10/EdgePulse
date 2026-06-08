import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { getMills, getAreas } from '../../api/organisation';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import styles from './AreasPage.module.css';

export default function AreasPage() {
  const { t } = useTranslation();
  const [millFilter, setMillFilter] = useState('');

  const { data: mills = [] } = useQuery({ queryKey: ['mills'], queryFn: getMills });
  const { data: areas = [], isLoading } = useQuery({
    queryKey: ['areas', millFilter],
    queryFn:  () => getAreas(millFilter || undefined),
  });

  if (isLoading) return <LoadingSpinner message={t('areas.loading')} />;

  return (
    <>
      <div className={styles.toolbar}>
        <div className={styles.filters}>
          <span className={styles.filterLabel}>{t('common.filter')}</span>
          <select
            className={styles.select}
            value={millFilter}
            onChange={e => setMillFilter(e.target.value)}
          >
            <option value="">{t('areas.allMills')}</option>
            {mills.map(m => (
              <option key={m.id} value={m.id}>
                {m.name}{m.location ? ` — ${m.location}` : ''}
              </option>
            ))}
          </select>
        </div>

        <div className={styles.count}>
          {t('areas.count', { count: areas.length })}
        </div>
      </div>

      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>{t('areas.table.area')}</th>
              <th>{t('areas.table.code')}</th>
              <th>{t('areas.table.mill')}</th>
              <th>{t('areas.table.locationType')}</th>
              <th>{t('areas.table.description')}</th>
            </tr>
          </thead>
          <tbody>
            {areas.length === 0 ? (
              <tr>
                <td colSpan={5} className={styles.empty}>{t('areas.empty')}</td>
              </tr>
            ) : (
              areas.map(area => {
                const mill = mills.find(m => m.id === area.millId);
                return (
                  <tr key={area.id}>
                    <td className={styles.areaName}>{area.name}</td>
                    <td className={styles.areaCode}>{area.code}</td>
                    <td className={styles.location}>
                      <div>{area.millName}</div>
                      {mill?.location && (
                        <div className={styles.locationSub}>{mill.location}</div>
                      )}
                    </td>
                    <td className={styles.location}>{area.locationTypeName ?? '—'}</td>
                    <td className={styles.location}>{area.description ?? '—'}</td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
