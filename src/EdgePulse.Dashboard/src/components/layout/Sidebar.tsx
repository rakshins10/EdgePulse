import { useEffect } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { setAlertCount } from '../../store/alertsSlice';
import { fetchAlertCount } from '../../api/alerts';
import keycloak from '../../keycloak';
import styles from './Sidebar.module.css';

interface SidebarProps {
  isOpen?: boolean;
  onClose?: () => void;
}

const NAV_ITEMS = [
  { to: '/dashboard',     icon: '⬛', labelKey: 'nav.dashboard' },
  { to: '/devices',       icon: '🔌', labelKey: 'nav.devices'   },
  { to: '/alerts',        icon: '🔔', labelKey: 'nav.alerts'    },
  { to: '/mills',         icon: '🏭', labelKey: 'nav.mills'      },
  { to: '/areas',         icon: '📍', labelKey: 'nav.areas'      },
  { to: '/configuration', icon: '⚙️', labelKey: 'nav.configuration' },
];

const BADGE_REFRESH_MS = 30_000;

export default function Sidebar({ isOpen = false, onClose }: SidebarProps) {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const { openCount, criticalOpenCount } = useAppSelector(
    (s) => s.alerts.count
  );
  const location = useLocation();

  // Close sidebar on route change (mobile)
  useEffect(() => {
    onClose?.();
  }, [location.pathname]); // eslint-disable-line react-hooks/exhaustive-deps

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
    <aside
      className={`${styles.sidebar}${isOpen ? ` ${styles.sidebarOpen}` : ''}`}
    >
      <div className={styles.logo}>
        <div className={styles.logoText}>EdgePulse</div>
        <div className={styles.logoSub}>{t('nav.appSubtitle')}</div>
      </div>

      <nav className={styles.nav}>
        {NAV_ITEMS.map(({ to, icon, labelKey }) => (
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
            <span>{t(labelKey)}</span>

            {labelKey === 'nav.alerts' && openCount > 0 && (
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
          {t('nav.signOut')}
        </button>
      </div>
    </aside>
  );
}
