import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getNotifications, getUnreadCount,
  markNotificationRead, markAllNotificationsRead,
} from '../../api/notifications';
import type { NotificationDto } from '../../types/api';
import styles from './NotificationBell.module.css';

/** "5 m ago" style relative time in the active UI language. */
function relativeTime(iso: string, locale: string): string {
  const rtf = new Intl.RelativeTimeFormat(locale, { numeric: 'auto', style: 'narrow' });
  const seconds = Math.round((new Date(iso + (iso.endsWith('Z') ? '' : 'Z')).getTime() - Date.now()) / 1000);
  const abs = Math.abs(seconds);
  if (abs < 60) return rtf.format(Math.trunc(seconds / 1), 'second');
  if (abs < 3600) return rtf.format(Math.trunc(seconds / 60), 'minute');
  if (abs < 86400) return rtf.format(Math.trunc(seconds / 3600), 'hour');
  return rtf.format(Math.trunc(seconds / 86400), 'day');
}

export default function NotificationBell() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [open, setOpen] = useState(false);
  const wrapRef = useRef<HTMLDivElement>(null);

  const { data: unreadCount = 0 } = useQuery({
    queryKey: ['notifications', 'unread-count'],
    queryFn: getUnreadCount,
    refetchInterval: 30_000,
  });

  const { data: items = [] } = useQuery({
    queryKey: ['notifications', 'list'],
    queryFn: () => getNotifications(false, 30),
    enabled: open,
    refetchInterval: open ? 30_000 : false,
  });

  // Close on outside click / Escape
  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') setOpen(false); };
    document.addEventListener('mousedown', onDown);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onDown);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  async function invalidate() {
    await qc.invalidateQueries({ queryKey: ['notifications'] });
  }

  async function handleItemClick(n: NotificationDto) {
    if (!n.isRead) {
      try { await markNotificationRead(n.id); } catch { /* non-fatal */ }
      await invalidate();
    }
    setOpen(false);
    if (n.linkEntityType === 'Alert') navigate('/alerts');
  }

  async function handleMarkAll() {
    try { await markAllNotificationsRead(); } catch { /* non-fatal */ }
    await invalidate();
  }

  const severityClass = (sev: string | null) =>
    sev && sev in { CRITICAL: 1, HIGH: 1, MEDIUM: 1, LOW: 1 }
      ? styles[`dot${sev}` as keyof typeof styles]
      : '';

  return (
    <div className={styles.wrap} ref={wrapRef}>
      <button
        className={styles.bellBtn}
        onClick={() => setOpen(v => !v)}
        aria-label={t('notifications.title')}
        title={t('notifications.title')}
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
          strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.7 21a2 2 0 0 1-3.4 0" />
        </svg>
        {unreadCount > 0 && (
          <span className={styles.badge}>{unreadCount > 99 ? '99+' : unreadCount}</span>
        )}
      </button>

      {open && (
        <div className={styles.panel} role="region" aria-label={t('notifications.title')}>
          <div className={styles.panelHeader}>
            <h3 className={styles.panelTitle}>{t('notifications.title')}</h3>
            {unreadCount > 0 && (
              <button className={styles.markAllBtn} onClick={handleMarkAll}>
                {t('notifications.markAllRead')}
              </button>
            )}
          </div>
          <div className={styles.list}>
            {items.length === 0 ? (
              <div className={styles.empty}>{t('notifications.empty')}</div>
            ) : (
              items.map(n => (
                <button
                  key={n.id}
                  className={`${styles.item} ${n.isRead ? '' : styles.itemUnread}`}
                  onClick={() => handleItemClick(n)}
                >
                  <span className={`${styles.dot} ${severityClass(n.severityCode)}`} />
                  <span className={styles.itemBody}>
                    <p className={styles.itemTitle}>{n.title}</p>
                    <p className={styles.itemMsg}>{n.message}</p>
                    <span className={styles.itemTime}>
                      {relativeTime(n.createdAt, i18n.language)}
                    </span>
                  </span>
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
