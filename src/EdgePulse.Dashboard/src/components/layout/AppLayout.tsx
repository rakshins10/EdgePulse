import { Outlet, useMatches } from 'react-router-dom';
import Sidebar from './Sidebar';
import styles from './AppLayout.module.css';

interface RouteHandle {
  title?: string;
}

export default function AppLayout() {
  const matches = useMatches();
  const lastMatch = matches[matches.length - 1];
  const handle = lastMatch?.handle as RouteHandle | undefined;
  const title = handle?.title ?? 'EdgePulse';

  return (
    <div className={styles.shell}>
      <Sidebar />
      <div className={styles.main}>
        <header className={styles.topbar}>
          <h1 className={styles.pageTitle}>{title}</h1>
        </header>
        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
