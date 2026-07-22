import { useEffect } from 'react';
import type { ReactElement } from 'react';
import styles from './ConfirmDialog.module.css';

export type ConfirmVariant = 'danger' | 'warning' | 'info';

const ICONS: Record<ConfirmVariant, ReactElement> = {
  danger: (
    <>
      <path d="M3 6h18M8 6V4h8v2M6 6l1 14h10l1-14" fill="none" stroke="currentColor"
        strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </>
  ),
  warning: (
    <>
      <path d="M12 3 1.8 20.5h20.4L12 3Z" fill="none" stroke="currentColor" strokeWidth="2"
        strokeLinejoin="round" />
      <path d="M12 9.5v4.5M12 17.5v.5" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" />
    </>
  ),
  info: (
    <>
      <circle cx="12" cy="12" r="9.5" fill="none" stroke="currentColor" strokeWidth="2" />
      <path d="M12 11v5.5M12 7.5v.5" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" />
    </>
  ),
};

export interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message?: string;
  confirmLabel: string;
  cancelLabel: string;
  variant: ConfirmVariant;
  busy?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export default function ConfirmDialog({
  open, title, message, confirmLabel, cancelLabel, variant, busy, onConfirm, onCancel,
}: ConfirmDialogProps) {
  useEffect(() => {
    if (!open) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !busy) onCancel();
      if (e.key === 'Enter' && !busy) onConfirm();
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [open, busy, onCancel, onConfirm]);

  if (!open) return null;

  return (
    <div className={styles.overlay} onMouseDown={() => !busy && onCancel()}>
      <div
        className={`${styles.dialog} ${styles[variant]}`}
        role="alertdialog"
        aria-modal="true"
        aria-label={title}
        onMouseDown={e => e.stopPropagation()}
      >
        <div className={styles.body}>
          <div className={styles.iconWrap}>
            <svg viewBox="0 0 24 24" aria-hidden="true">{ICONS[variant]}</svg>
          </div>
          <div className={styles.text}>
            <h3 className={styles.title}>{title}</h3>
            {message && <p className={styles.message}>{message}</p>}
          </div>
        </div>
        <div className={styles.footer}>
          <button className={`${styles.btn} ${styles.cancel}`} onClick={onCancel} disabled={busy}>
            {cancelLabel}
          </button>
          <button className={`${styles.btn} ${styles.confirm}`} onClick={onConfirm} disabled={busy} autoFocus>
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
