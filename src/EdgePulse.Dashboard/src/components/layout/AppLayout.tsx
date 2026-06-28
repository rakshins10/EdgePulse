import { useState } from 'react';
import { Outlet, useMatches } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import Sidebar from './Sidebar';
import ThemeToggle from './ThemeToggle';
import LanguageSwitcher from './LanguageSwitcher';
import styles from './AppLayout.module.css';

interface RouteHandle {
  titleKey?: string;
  title?: string;
}

const COLLAPSE_KEY = 'edgepulse-sidebar-collapsed';

export default function AppLayout() {
  const { t } = useTranslation();
  const matches = useMatches();
  const lastMatch = matches[matches.length - 1];
  const handle = lastMatch?.handle as RouteHandle | undefined;
  const title = handle?.titleKey ? t(handle.titleKey) : (handle?.title ?? 'EdgePulse');

  // Mobile drawer (overlay) state
  const [sidebarOpen, setSidebarOpen] = useState(false);
  // Desktop collapse (icon-only rail) state, persisted
  const [collapsed, setCollapsed] = useState(
    () => localStorage.getItem(COLLAPSE_KEY) === '1',
  );

  function toggleCollapsed() {
    setCollapsed(prev => {
      const next = !prev;
      localStorage.setItem(COLLAPSE_KEY, next ? '1' : '0');
      return next;
    });
  }

  return (
    <div className={styles.shell}>
      {sidebarOpen && (
        <div
          className={styles.mobileOverlay}
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <Sidebar
        isOpen={sidebarOpen}
        collapsed={collapsed}
        onClose={() => setSidebarOpen(false)}
        onToggleCollapse={toggleCollapsed}
      />

      <div className={styles.main}>
        <header className={styles.topbar}>
          {/* Mobile-only: opens the drawer (the in-sidebar toggle is off-screen
              when the drawer is closed). Hidden on desktop. */}
          <button
            className={styles.menuBtn}
            onClick={() => setSidebarOpen(v => !v)}
            aria-label="Open navigation"
            title="Menu"
          >
            ☰
          </button>

          <h1 className={styles.pageTitle}>{title}</h1>

          <div className={styles.topbarActions}>
            <LanguageSwitcher />
            <ThemeToggle />
          </div>
        </header>

        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
