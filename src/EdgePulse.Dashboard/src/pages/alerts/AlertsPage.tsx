import { useState, useCallback, useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useAppDispatch } from '../../store/hooks';
import { setAlertCount } from '../../store/alertsSlice';
import { fetchAlerts, fetchAlertCount } from '../../api/alerts';
import AlertActionModal from '../../components/alerts/AlertActionModal';
import type { AlertDto, SeverityCode, StatusCode } from '../../types/alerts';
import { getAiStatus } from '../../api/ai';
import AiSummaryPanel from '../../components/alerts/AiSummaryPanel';
import aiStyles from '../../components/alerts/AiSummaryPanel.module.css';
import styles from './AlertsPage.module.css';

const SEVERITY_OPTIONS: Array<{ value: SeverityCode | ''; label: string }> = [
  { value: '',         label: 'All Severities' },
  { value: 'CRITICAL', label: 'Critical' },
  { value: 'HIGH',     label: 'High' },
  { value: 'MEDIUM',   label: 'Medium' },
  { value: 'LOW',      label: 'Low' },
];

const STATUS_OPTIONS: Array<{ value: StatusCode | ''; label: string }> = [
  { value: '',             label: 'All Statuses' },
  { value: 'OPEN',         label: 'Open' },
  { value: 'ACKNOWLEDGED', label: 'Acknowledged' },
  { value: 'RESOLVED',     label: 'Resolved' },
  { value: 'CLOSED',       label: 'Closed' },
];

const PAGE_SIZE = 50;

export default function AlertsPage() {
  const dispatch = useAppDispatch();
  const queryClient = useQueryClient();

  // AI: is a provider configured? (hides the Explain button otherwise)
  const { data: aiStatus } = useQuery({ queryKey: ['ai-status'], queryFn: getAiStatus, staleTime: 5 * 60_000 });
  const [explainedId, setExplainedId] = useState<string | null>(null);

  // Deep link from a notification: /alerts?highlight=<alertId>
  const [searchParams] = useSearchParams();
  const highlightId = searchParams.get('highlight');

  // Filters
  const [severityFilter, setSeverityFilter] = useState<SeverityCode | ''>('');
  const [statusFilter, setStatusFilter]     = useState<StatusCode | ''>('OPEN');
  const [page, setPage]                     = useState(1);

  // A linked alert may already be acknowledged/resolved, which the default
  // OPEN filter would hide — clear filters so the target is always findable.
  useEffect(() => {
    if (highlightId) {
      setStatusFilter('');
      setSeverityFilter('');
      setPage(1);
    }
  }, [highlightId]);

  // Modal state
  const [modalAlert, setModalAlert]  = useState<AlertDto | null>(null);
  const [modalAction, setModalAction] = useState<'acknowledge' | 'resolve'>('acknowledge');

  // Fetch alerts list
  const { data, isLoading, isError } = useQuery({
    queryKey: ['alerts', severityFilter, statusFilter, page],
    queryFn: () =>
      fetchAlerts({
        severityCode: severityFilter || undefined,
        statusCode:   statusFilter   || undefined,
        page,
        pageSize: PAGE_SIZE,
      }),
    staleTime: 15_000,
  });

  // Invalidate after modal action + refresh badge count
  const handleActionSuccess = useCallback(async () => {
    void queryClient.invalidateQueries({ queryKey: ['alerts'] });
    try {
      const count = await fetchAlertCount();
      dispatch(setAlertCount(count));
    } catch {
      // non-critical
    }
  }, [queryClient, dispatch]);

  function openModal(alert: AlertDto, action: 'acknowledge' | 'resolve') {
    setModalAlert(alert);
    setModalAction(action);
  }

  const totalPages = data
    ? Math.ceil(data.totalCount / PAGE_SIZE)
    : 1;

  // Summary counts from current query (all open, regardless of page)
  const items = data?.items ?? [];
  const criticalCount = items.filter((a) => a.severityCode === 'CRITICAL' && a.statusCode === 'OPEN').length;
  const highCount     = items.filter((a) => a.severityCode === 'HIGH'     && a.statusCode === 'OPEN').length;
  const mediumCount   = items.filter((a) => a.severityCode === 'MEDIUM'   && a.statusCode === 'OPEN').length;

  return (
    <>
      {/* Summary counts */}
      <div className={styles.summary}>
        <div className={`${styles.summaryCard} ${styles.summaryCardCritical}`}>
          <div className={styles.summaryCount}>{data?.totalCount ?? '–'}</div>
          <div className={styles.summaryLabel}>Total (filtered)</div>
        </div>
        {statusFilter === 'OPEN' || !statusFilter ? (
          <>
            <div className={`${styles.summaryCard} ${styles.summaryCardCritical}`}>
              <div className={styles.summaryCount}>{criticalCount}</div>
              <div className={styles.summaryLabel}>Critical Open</div>
            </div>
            <div className={`${styles.summaryCard} ${styles.summaryCardHigh}`}>
              <div className={styles.summaryCount}>{highCount}</div>
              <div className={styles.summaryLabel}>High Open</div>
            </div>
            <div className={`${styles.summaryCard} ${styles.summaryCardMedium}`}>
              <div className={styles.summaryCount}>{mediumCount}</div>
              <div className={styles.summaryLabel}>Medium Open</div>
            </div>
          </>
        ) : null}
      </div>

      {/* Filters */}
      <div className={styles.filters}>
        <select
          className={styles.select}
          value={severityFilter}
          onChange={(e) => {
            setSeverityFilter(e.target.value as SeverityCode | '');
            setPage(1);
          }}
        >
          {SEVERITY_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>

        <select
          className={styles.select}
          value={statusFilter}
          onChange={(e) => {
            setStatusFilter(e.target.value as StatusCode | '');
            setPage(1);
          }}
        >
          {STATUS_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
      </div>

      {/* Table */}
      <div className={styles.tableWrap}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Severity</th>
              <th>Device</th>
              <th>Mill</th>
              <th>Metric</th>
              <th>Value / Threshold</th>
              <th>Status</th>
              <th>Triggered</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan={8} className={styles.loading}>
                  Loading alerts…
                </td>
              </tr>
            ) : isError ? (
              <tr>
                <td colSpan={8} className={styles.empty}>
                  Failed to load alerts. Please refresh.
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={8} className={styles.empty}>
                  No alerts match the current filters.
                </td>
              </tr>
            ) : (
              items.map((alert) => (
                <AlertRow
                  key={alert.id}
                  alert={alert}
                  highlighted={alert.id === highlightId}
                  onAcknowledge={() => openModal(alert, 'acknowledge')}
                  onResolve={() => openModal(alert, 'resolve')}
                  aiEnabled={!!aiStatus?.enabled}
                  explained={explainedId === alert.id}
                  onExplain={() => setExplainedId(explainedId === alert.id ? null : alert.id)}
                />
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className={styles.pagination}>
          <span className={styles.pageInfo}>
            {data?.totalCount ?? 0} alerts — Page {page} of {totalPages}
          </span>
          <div className={styles.pageControls}>
            <button
              className={styles.btnPage}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              ← Prev
            </button>
            <button
              className={styles.btnPage}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
            >
              Next →
            </button>
          </div>
        </div>
      )}

      {/* Acknowledge / Resolve modal */}
      {modalAlert && (
        <AlertActionModal
          alert={modalAlert}
          action={modalAction}
          onClose={() => setModalAlert(null)}
          onSuccess={handleActionSuccess}
        />
      )}
    </>
  );
}

// ─── Alert row sub-component ──────────────────────────────────────────────────

interface AlertRowProps {
  alert: AlertDto;
  onAcknowledge: () => void;
  onResolve: () => void;
  highlighted?: boolean;
  aiEnabled?: boolean;
  explained?: boolean;
  onExplain?: () => void;
}

function AlertRow({ alert, onAcknowledge, onResolve, highlighted, aiEnabled, explained, onExplain }: AlertRowProps) {
  const canResolve =
    alert.statusCode === 'OPEN' || alert.statusCode === 'ACKNOWLEDGED';
  const rowRef = useRef<HTMLTableRowElement>(null);

  // Scroll the deep-linked row into view when arriving from a notification
  useEffect(() => {
    if (highlighted) {
      rowRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }, [highlighted]);

  return (
    <>
    <tr ref={rowRef} className={highlighted ? styles.rowHighlight : undefined}>
      <td>
        <SeverityPill code={alert.severityCode} />
      </td>
      <td>
        <strong>{alert.deviceCode}</strong>
        <br />
        <span style={{ color: '#475569', fontSize: '0.72rem' }}>
          {alert.deviceName}
        </span>
      </td>
      <td>{alert.millName}</td>
      <td>{alert.metricKey}</td>
      <td>
        <div className={styles.metricValue}>
          {alert.triggerValue.toFixed(2)}
          {alert.unit ? ` ${alert.unit}` : ''}
        </div>
        <div className={styles.thresholdNote}>
          threshold: {alert.thresholdValue.toFixed(2)}
          {alert.unit ? ` ${alert.unit}` : ''}
        </div>
      </td>
      <td>
        <StatusPill code={alert.statusCode} />
      </td>
      <td>
        <RelativeTime iso={alert.triggeredAt} />
      </td>
      <td>
        <div className={styles.actions}>
          {alert.statusCode === 'OPEN' && (
            <button className={`${styles.btnAction} ${styles.btnAck}`} onClick={onAcknowledge}>
              Ack
            </button>
          )}
          {canResolve && (
            <button className={`${styles.btnAction} ${styles.btnResolve}`} onClick={onResolve}>
              Resolve
            </button>
          )}
          {aiEnabled && (
            <button
              className={`${aiStyles.explainBtn} ${explained ? aiStyles.explainBtnActive : ''}`}
              onClick={onExplain}
              title="AI explanation"
            >
              ✦ Explain
            </button>
          )}
        </div>
      </td>
    </tr>
    {explained && <AiSummaryPanel alertId={alert.id} colSpan={8} />}
    </>
  );
}

// ─── Severity pill ────────────────────────────────────────────────────────────

function SeverityPill({ code }: { code: SeverityCode }) {
  const cls = {
    CRITICAL: styles.severityCritical,
    HIGH:     styles.severityHigh,
    MEDIUM:   styles.severityMedium,
    LOW:      styles.severityLow,
  }[code];
  return <span className={`${styles.severity} ${cls}`}>{code}</span>;
}

// ─── Status pill ──────────────────────────────────────────────────────────────

function StatusPill({ code }: { code: StatusCode }) {
  const cls = {
    OPEN:         styles.statusOpen,
    ACKNOWLEDGED: styles.statusAcknowledged,
    RESOLVED:     styles.statusResolved,
    CLOSED:       styles.statusClosed,
  }[code];
  return <span className={`${styles.status} ${cls}`}>{code}</span>;
}

// ─── Relative time ────────────────────────────────────────────────────────────

function RelativeTime({ iso }: { iso: string }) {
  const date = new Date(iso);
  const diffMs = Date.now() - date.getTime();
  const diffMin = Math.floor(diffMs / 60_000);

  let label: string;
  if (diffMin < 1)       label = 'just now';
  else if (diffMin < 60) label = `${diffMin}m ago`;
  else if (diffMin < 1440) {
    const h = Math.floor(diffMin / 60);
    label = `${h}h ago`;
  } else {
    label = date.toLocaleDateString();
  }

  return (
    <span title={date.toLocaleString()} style={{ whiteSpace: 'nowrap' }}>
      {label}
    </span>
  );
}
