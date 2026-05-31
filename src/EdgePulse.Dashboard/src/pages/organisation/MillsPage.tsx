import { useQuery } from '@tanstack/react-query';
import { getMills, getAreas } from '../../api/organisation';
import Badge from '../../components/common/Badge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import styles from './MillsPage.module.css';

export default function MillsPage() {
  const { data: mills = [], isLoading: millsLoading } = useQuery({
    queryKey: ['mills'],
    queryFn: getMills,
  });

  const { data: areas = [], isLoading: areasLoading } = useQuery({
    queryKey: ['areas'],
    queryFn: () => getAreas(),
  });

  if (millsLoading || areasLoading) return <LoadingSpinner message="Loading mills…" />;

  if (mills.length === 0) {
    return (
      <div className={styles.empty}>
        No mills configured for your tenant.
      </div>
    );
  }

  return (
    <div className={styles.grid}>
      {mills.map(mill => {
        const millAreas = areas.filter(a => a.millId === mill.id);

        return (
          <div key={mill.id} className={styles.card}>
            <div className={styles.cardHeader}>
              <div>
                <div className={styles.millName}>{mill.name}</div>
                <div className={styles.millMeta}>
                  {mill.location} · {mill.timezone}
                </div>
              </div>
              <div className={styles.badges}>
                <Badge label={mill.deploymentMode} variant="deployment" />
                {!mill.hasInternet && (
                  <Badge label="Offline" color="#ef4444" variant="status" />
                )}
              </div>
            </div>

            <div className={styles.cardBody}>
              <div className={styles.areaHeader}>
                Areas ({millAreas.length})
              </div>
              {millAreas.length === 0 ? (
                <p className={styles.noAreas}>No areas configured</p>
              ) : (
                <ul className={styles.areaList}>
                  {millAreas.map(area => (
                    <li key={area.id} className={styles.areaItem}>
                      <span className={styles.areaName}>{area.name}</span>
                      <span className={styles.areaCode}>{area.code}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
