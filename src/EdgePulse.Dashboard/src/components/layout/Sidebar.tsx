import { useEffect } from 'react';
import { NavLink } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { setAlertCount } from '../../store/alertsSlice';
import { fetchAlertCount } from '../../api/alerts';
import keycloak from '../../keycloak';
import styles from './Sidebar.module.css';

const NAV_ITEMS = [
  { to: '/dashboard', icon: '⬛', label: 'Dashboard' },
  { to: '/devices',   icon: '🔌', label: 'Devices'   },
  { to: '/alerts',    icon: '🔔', label: 'Alerts'    },
  { to: '/mills',     icon: '🏭', label: 'Mills'      },
  { to: '/areas',     icon: '📍', label: 'Areas'      },
];

const BADGE_REFRESH_MS = 30_000; // 30 seconds

export default function Sidebar() {
  const dispatch = useAppDispatch();
  const { openCount, criticalOpenCount } = useAppSelector(
    (s) => s.alerts.count
  );

  // Poll alert count every 30s
  useEffect(() => {
    let mounted = true;

    async function refresh() {
      try {
        const count = await fetchAlertCount();
        if (mounted) dispatch(setAlertCount(count));
      } catch {
        // silently fail — badge shows stale count
      }
    }

    refresh();
    const id = setInterval(refresh, BADGE_REFRESH_MS);
    return () => {
      mounted = false;
      clearInterval(id);
    };
  }, [dispatch]);

  const userEmail = keycloak.tokenParsed?.email as string | undefined;

  return (
    <aside className={styles.sidebar}>
      <div className={styles.logo}>
        <div className={styles.logoText}>EdgePulse</div>
        <div className={styles.logoSub}>Industrial IoT Platform</div>
      </div>

      <nav className={styles.nav}>
        {NAV_ITEMS.map(({ to, icon, label }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              isActive
                ? `${styles.navItem} ${styles.navItemActive}`
                : styles.navItem
            }
          >
            <span className={styles.navIcon}>{icon}</span>
            <span>{label}</span>

            {label === 'Alerts' && openCount > 0 && (
              <span className={styles.badgeWrapper}>
                {criticalOpenCount > 0 && (
                  <span className={`${styles.badge} ${styles.badgeCritical}`}>
                    {criticalOpenCount > 99 ? '99+' : criticalOpenCount}
                  </span>
                )}
                {openCount !== criticalOpenCount && (
                  <span className={`${styles.badge} ${styles.badgeOpen}`}>
                    {openCount > 99 ? '99+' : openCount}
                  </span>
                )}
              </span>
            )}
          </NavLink>
        ))}
      </nav>

      <div className={styles.footer}>
        {userEmail && (
          <div className={styles.userEmail} title={userEmail}>
            {userEmail}
          </div>
        )}
        <button
          className={styles.logoutBtn}
          onClick={() => keycloak.logout()}
        >
          Sign out
        </button>
      </div>
    </aside>
  );
}
