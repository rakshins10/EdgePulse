import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { getAuditLogs, downloadAuditCsv } from '../../api/audit';
import { useToast } from '../../context/ToastContext';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import styles from './AuditPage.module.css';

const ACTIONS = ['', 'CREATED', 'UPDATED', 'DELETED'];

interface Change { old: string | null; new: string | null }

function parseChanges(json: string | null): [string, Change][] {
  if (!json) return [];
  try {
    return Object.entries(JSON.parse(json) as Record<string, Change>);
  } catch {
    return [];
  }
}

export default function AuditPage() {
  const { t, i18n } = useTranslation();
  const toast = useToast();
  const [action, setAction] = useState('');
  const [entityType, setEntityType] = useState('');

  const { data: logs = [], isLoading } = useQuery({
    queryKey: ['audit', action, entityType],
    queryFn: () => getAuditLogs({
      action: action || undefined,
      entityType: entityType || undefined,
    }),
    refetchInterval: 60_000,
  });

  const entityTypes = [...new Set(logs.map(l => l.entityType))].sort();

  async function handleCsv() {
    try { await downloadAuditCsv(); }
    catch { toast.error(t('audit.exportError')); }
  }

  if (isLoading) return <LoadingSpinner message={t('common.loading')} />;

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <select className={styles.select} value={action} onChange={e => setAction(e.target.value)}>
          {ACTIONS.map(a => (
            <option key={a || 'all'} value={a}>
              {a === '' ? t('audit.allActions') : t(`audit.action.${a}`)}
            </option>
          ))}
        </select>
        <select className={styles.select} value={entityType} onChange={e => setEntityType(e.target.value)}>
          <option value="">{t('audit.allEntities')}</option>
          {entityTypes.map(et => <option key={et} value={et}>{et}</option>)}
        </select>
        <div className={styles.spacer} />
        <button className={styles.csvBtn} onClick={handleCsv}>{t('audit.exportCsv')}</button>
      </div>

      <div className={styles.card}>
        {logs.length === 0 ? (
          <div className={styles.empty}>{t('audit.empty')}</div>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t('audit.when')}</th>
                <th>{t('audit.user')}</th>
                <th>{t('audit.actionHeader')}</th>
                <th>{t('audit.entity')}</th>
                <th>{t('audit.changes')}</th>
              </tr>
            </thead>
            <tbody>
              {logs.map(l => {
                const changes = parseChanges(l.changesJson);
                return (
                  <tr key={l.id}>
                    <td className={styles.time}>
                      {new Date(l.timestamp + (l.timestamp.endsWith('Z') ? '' : 'Z'))
                        .toLocaleString(i18n.language)}
                    </td>
                    <td>{l.userName}</td>
                    <td>
                      <span className={`${styles.chip} ${styles[`a${l.action}` as keyof typeof styles] ?? ''}`}>
                        {t(`audit.action.${l.action}`)}
                      </span>
                    </td>
                    <td>
                      <span className={styles.entity}>{l.entityType}</span>
                      <span className={styles.entitySub}>
                        {l.entityDisplay ?? l.entityId.slice(0, 8)}
                      </span>
                    </td>
                    <td>
                      {changes.length === 0 ? (
                        <span className={styles.entitySub}>—</span>
                      ) : (
                        <ul className={styles.changes}>
                          {changes.map(([prop, c]) => (
                            <li key={prop}>
                              <span className={styles.prop}>{prop}: </span>
                              <span className={styles.old}>{c.old ?? '∅'}</span>
                              {' → '}
                              <span className={styles.new}>{c.new ?? '∅'}</span>
                            </li>
                          ))}
                        </ul>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
