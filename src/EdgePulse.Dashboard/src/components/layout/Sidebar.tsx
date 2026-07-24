import { useEffect } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { setAlertCount } from '../../store/alertsSlice';
import { fetchAlertCount } from '../../api/alerts';
import keycloak from '../../keycloak';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import styles from './Sidebar.module.css';

interface SidebarProps {
  isOpen?: boolean;
  collapsed?: boolean;
  onClose?: () => void;
  onToggleCollapse?: () => void;
}

const NAV_ITEMS: { to: string; icon: string; labelKey: string; adminOnly?: boolean }[] = [
  { to: '/dashboard',     icon: '⬛', labelKey: 'nav.dashboard' },
  { to: '/devices',       icon: '🔌', labelKey: 'nav.devices'   },
  { to: '/alerts',        icon: '🔔', labelKey: 'nav.alerts'    },
  { to: '/mills',         icon: '🏭', labelKey: 'nav.mills'      },
  { to: '/areas',         icon: '📍', labelKey: 'nav.areas'      },
  { to: '/workorders',    icon: '🛠️', labelKey: 'nav.workorders' },
  { to: '/energy',        icon: '⚡', labelKey: 'nav.energy'     },
  { to: '/reports',       icon: '📊', labelKey: 'nav.reports'    },
  { to: '/users',         icon: '👥', labelKey: 'nav.users', adminOnly: true },
  { to: '/audit',         icon: '📜', labelKey: 'nav.audit', adminOnly: true },
  { to: '/configuration', icon: '⚙️', labelKey: 'nav.configuration' },
];

const BADGE_REFRESH_MS = 30_000;

export default function Sidebar({
  isOpen = false,
  collapsed = false,
  onClose,
  onToggleCollapse,
}: SidebarProps) {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const { openCount, criticalOpenCount } = useAppSelector(
    (s) => s.alerts.count
  );
  const location = useLocation();
  const user = useCurrentUser();
  const isAdmin = user?.role === 'SuperAdmin' || user?.role === 'CustomerAdmin';
  const navItems = NAV_ITEMS.filter(item => !item.adminOnly || isAdmin);

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

  const asideClass = [
    styles.sidebar,
    isOpen ? styles.sidebarOpen : '',
    collapsed ? styles.sidebarCollapsed : '',
  ].filter(Boolean).join(' ');

  return (
    <aside className={asideClass}>
      <div className={styles.logo}>
        <button
          className={styles.collapseBtn}
          onClick={onToggleCollapse}
          aria-label="Toggle menu"
          title="Toggle menu"
        >
          ☰
        </button>
        <div className={styles.logoBrand}>
          <div className={styles.logoText}>
            <span className={styles.logoFull}>EdgePulse</span>
            <span className={styles.logoMark}>EP</span>
          </div>
          <div className={styles.logoSub}>{t('nav.appSubtitle')}</div>
        </div>
      </div>

      <nav className={styles.nav}>
        {navItems.map(({ to, icon, labelKey }) => (
          <NavLink
            key={to}
            to={to}
            title={collapsed ? t(labelKey) : undefined}
            className={({ isActive }) =>
              isActive
                ? `${styles.navItem} ${styles.navItemActive}`
                : styles.navItem
            }
          >
            <span className={styles.navIcon}>{icon}</span>
            <span className={styles.navLabel}>{t(labelKey)}</span>

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
          title={t('nav.signOut')}
        >
          <span className={styles.logoutFull}>{t('nav.signOut')}</span>
          <span className={styles.logoutMark}>⏻</span>
        </button>
      </div>
    </aside>
  );
}
