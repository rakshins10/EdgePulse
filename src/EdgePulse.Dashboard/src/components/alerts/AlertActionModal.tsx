import { useState } from 'react';
import { acknowledgeAlert, resolveAlert } from '../../api/alerts';
import type { AlertDto } from '../../types/alerts';
import styles from './AlertActionModal.module.css';

type ActionType = 'acknowledge' | 'resolve';

interface Props {
  alert: AlertDto;
  action: ActionType;
  onClose: () => void;
  onSuccess: () => void;
}

export default function AlertActionModal({
  alert,
  action,
  onClose,
  onSuccess,
}: Props) {
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isAcknowledge = action === 'acknowledge';
  const title = isAcknowledge ? 'Acknowledge Alert' : 'Resolve Alert';
  const subtitle = isAcknowledge
    ? 'Confirm you have seen and are handling this alert.'
    : 'Confirm the issue has been fixed and the alert can be closed.';

  async function handleSubmit() {
    setSubmitting(true);
    setError(null);
    try {
      if (isAcknowledge) {
        await acknowledgeAlert(alert.id, notes || undefined);
      } else {
        await resolveAlert(alert.id, notes || undefined);
      }
      onSuccess();
      onClose();
    } catch {
      setError('Failed to update alert. Please try again.');
      setSubmitting(false);
    }
  }

  // Close on overlay click
  function handleOverlayClick(e: React.MouseEvent<HTMLDivElement>) {
    if (e.target === e.currentTarget) onClose();
  }

  return (
    <div className={styles.overlay} onClick={handleOverlayClick}>
      <div className={styles.dialog} role="dialog" aria-modal="true">
        <h2 className={styles.title}>{title}</h2>
        <p className={styles.subtitle}>{subtitle}</p>

        <div className={styles.alertInfo}>
          <div className={styles.infoRow}>
            <span className={styles.infoLabel}>Device </span>
            <span className={styles.infoValue}>{alert.deviceCode}</span>
          </div>
          <div className={styles.infoRow}>
            <span className={styles.infoLabel}>Metric </span>
            <span className={styles.infoValue}>{alert.metricKey}</span>
          </div>
          <div className={styles.infoRow}>
            <span className={styles.infoLabel}>Value </span>
            <span className={styles.infoValue}>
              {alert.triggerValue.toFixed(2)}
              {alert.unit ? ` ${alert.unit}` : ''}
            </span>
          </div>
          <div className={styles.infoRow}>
            <span className={styles.infoLabel}>Severity </span>
            <span className={styles.infoValue}>{alert.severityCode}</span>
          </div>
        </div>

        <label className={styles.label} htmlFor="notes">
          Notes (optional)
        </label>
        <textarea
          id="notes"
          className={styles.textarea}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Describe the issue or remediation steps..."
          maxLength={1000}
        />

        {error && (
          <p style={{ color: '#ef4444', fontSize: '0.8rem', marginTop: 8 }}>
            {error}
          </p>
        )}

        <div className={styles.actions}>
          <button className={styles.btnCancel} onClick={onClose}>
            Cancel
          </button>
          <button
            className={`${styles.btnConfirm} ${
              isAcknowledge ? styles.btnAcknowledge : styles.btnResolve
            }`}
            onClick={handleSubmit}
            disabled={submitting}
          >
            {submitting
              ? 'Saving…'
              : isAcknowledge
              ? 'Acknowledge'
              : 'Mark Resolved'}
          </button>
        </div>
      </div>
    </div>
  );
}
