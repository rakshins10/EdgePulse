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

export default function AppLayout() {
  const { t } = useTranslation();
  const matches = useMatches();
  const lastMatch = matches[matches.length - 1];
  const handle = lastMatch?.handle as RouteHandle | undefined;
  const title = handle?.titleKey ? t(handle.titleKey) : (handle?.title ?? 'EdgePulse');

  const [sidebarOpen, setSidebarOpen] = useState(false);

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
        onClose={() => setSidebarOpen(false)}
      />

      <div className={styles.main}>
        <header className={styles.topbar}>
          <button
            className={styles.menuBtn}
            onClick={() => setSidebarOpen((v) => !v)}
            aria-label="Toggle navigation"
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
