import styles from './LoadingSpinner.module.css';

export default function LoadingSpinner({ message = 'Loading…' }: { message?: string }) {
  return (
    <div className={styles.wrapper}>
      <div className={styles.spinner} />
      <p className={styles.message}>{message}</p>
    </div>
  );
}
