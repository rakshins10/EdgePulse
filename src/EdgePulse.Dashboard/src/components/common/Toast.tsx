import type { ReactElement } from 'react';
import styles from './Toast.module.css';
import type { ToastItem, ToastVariant } from '../../context/ToastContext';

const ICONS: Record<ToastVariant, ReactElement> = {
  success: (
    <path d="M20 6 9 17l-5-5" fill="none" stroke="currentColor" strokeWidth="2.2"
      strokeLinecap="round" strokeLinejoin="round" />
  ),
  error: (
    <>
      <circle cx="12" cy="12" r="9.5" fill="none" stroke="currentColor" strokeWidth="2" />
      <path d="M12 7v6M12 16.5v.5" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" />
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

interface Props {
  items: ToastItem[];
  onDismiss: (id: number) => void;
}

export default function ToastContainer({ items, onDismiss }: Props) {
  if (items.length === 0) return null;

  return (
    <div className={styles.container} role="region" aria-label="Notifications">
      {items.map(item => (
        <div
          key={item.id}
          className={`${styles.toast} ${styles[item.variant]}`}
          role={item.variant === 'error' ? 'alert' : 'status'}
        >
          <svg className={styles.icon} viewBox="0 0 24 24" aria-hidden="true">
            {ICONS[item.variant]}
          </svg>
          <span className={styles.message}>{item.message}</span>
          <button className={styles.close} onClick={() => onDismiss(item.id)} aria-label="Dismiss">✕</button>
        </div>
      ))}
    </div>
  );
}
