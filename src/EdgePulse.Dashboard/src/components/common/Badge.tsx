import styles from './Badge.module.css';
import type { UserRole } from '../../types/api';

type Variant = 'role' | 'status' | 'deployment';

interface BadgeProps {
  label: string;
  color?: string | null;
  variant?: Variant;
}

const ROLE_CLASS: Record<UserRole, string> = {
  SuperAdmin:    styles.roleSuperAdmin,
  CustomerAdmin: styles.roleCustomerAdmin,
  MillManager:   styles.roleMillManager,
  Operator:      styles.roleOperator,
  Executive:     styles.roleExecutive,
};

export default function Badge({ label, color, variant = 'status' }: BadgeProps) {
  if (variant === 'role') {
    const cls = ROLE_CLASS[label as UserRole] ?? '';
    return <span className={`${styles.badge} ${cls}`}>{label}</span>;
  }

  if (variant === 'deployment') {
    const cls = label === 'Cloud' ? styles.deploymentCloud : styles.deploymentOnPremise;
    return <span className={`${styles.badge} ${cls}`}>{label}</span>;
  }

  const style = color
    ? {
        backgroundColor: `${color}26`,
        color: color,
        borderColor: `${color}50`,
      }
    : {};

  return (
    <span className={`${styles.badge} ${styles.status}`} style={style}>
      {label}
    </span>
  );
}
