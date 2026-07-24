import { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { getFloorPlan, setDevicePosition } from '../../api/floorplan';
import { getMills } from '../../api/organisation';
import type { FloorPlanDeviceDto } from '../../types/api';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { useToast } from '../../context/ToastContext';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import styles from './FloorPlanPage.module.css';

const VIEW_W = 100;
const VIEW_H = 62.5;

export default function FloorPlanPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const toast = useToast();
  const me = useCurrentUser();
  const canEdit = me?.role === 'SuperAdmin' || me?.role === 'CustomerAdmin' || me?.role === 'MillManager';

  const { data: mills = [] } = useQuery({ queryKey: ['mills'], queryFn: getMills });
  const [millId, setMillId] = useState('');
  const activeMill = millId || mills[0]?.id || '';

  const { data: devices = [], isLoading } = useQuery({
    queryKey: ['floorplan', activeMill],
    queryFn: () => getFloorPlan(activeMill),
    enabled: !!activeMill,
    refetchInterval: 10_000,
  });

  const [editMode, setEditMode] = useState(false);
  const [placing, setPlacing] = useState<FloorPlanDeviceDto | null>(null);
  const [dragging, setDragging] = useState<string | null>(null);
  const svgRef = useRef<SVGSVGElement>(null);

  const placed = devices.filter(d => d.floorX != null && d.floorY != null);
  const unplaced = devices.filter(d => d.floorX == null || d.floorY == null);

  function svgCoords(e: React.PointerEvent): { x: number; y: number } | null {
    const svg = svgRef.current;
    if (!svg) return null;
    const rect = svg.getBoundingClientRect();
    const x = ((e.clientX - rect.left) / rect.width) * VIEW_W;
    const y = ((e.clientY - rect.top) / rect.height) * VIEW_H;
    return {
      x: Math.min(Math.max(x, 1.5), VIEW_W - 1.5),
      y: Math.min(Math.max(y, 1.5), VIEW_H - 3),
    };
  }

  async function persistPosition(deviceId: string, x: number | null, y: number | null) {
    try {
      await setDevicePosition(deviceId, x, y === null ? null : (y / VIEW_H) * 100);
      await qc.invalidateQueries({ queryKey: ['floorplan', activeMill] });
    } catch {
      toast.error(t('floorplan.saveError'));
    }
  }

  function handleCanvasClick(e: React.PointerEvent) {
    if (!editMode || !placing) return;
    const pos = svgCoords(e);
    if (!pos) return;
    void persistPosition(placing.deviceId, pos.x, pos.y);
    toast.success(t('floorplan.placed', { name: placing.code }));
    setPlacing(null);
  }

  function handleDotPointerDown(e: React.PointerEvent, d: FloorPlanDeviceDto) {
    if (!editMode) return;
    e.stopPropagation();
    setDragging(d.deviceId);
    (e.target as Element).setPointerCapture(e.pointerId);
  }

  function handlePointerMove(_e: React.PointerEvent) {
    // Live-drag is visual only; the position persists on pointer-up
  }

  function handleDotPointerUp(e: React.PointerEvent) {
    if (!editMode || !dragging) return;
    const pos = svgCoords(e);
    if (pos) void persistPosition(dragging, pos.x, pos.y);
    setDragging(null);
  }

  function handleDotClick(d: FloorPlanDeviceDto) {
    if (editMode) return;
    navigate(`/devices/${d.deviceId}`);
  }

  function handleDotDoubleClick(d: FloorPlanDeviceDto) {
    if (!editMode) return;
    void persistPosition(d.deviceId, null, null);
    toast.success(t('floorplan.removed', { name: d.code }));
  }

  const dotColor = (d: FloorPlanDeviceDto) =>
    d.criticalAlerts > 0 ? 'var(--color-critical)'
    : d.openAlerts > 0 ? 'var(--color-high)'
    : d.statusColor || 'var(--color-low)';

  // stored Y is percent of height; convert back to view units
  const toViewY = (pct: number) => (pct / 100) * VIEW_H;

  if (isLoading && !devices.length) return <LoadingSpinner message={t('common.loading')} />;

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <select className={styles.select} value={activeMill}
          onChange={e => setMillId(e.target.value)}>
          {mills.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
        </select>
        <div className={styles.legend}>
          <span className={styles.legendItem}>
            <span className={styles.legendDot} style={{ background: 'var(--color-low)' }} />
            {t('floorplan.legendOk')}
          </span>
          <span className={styles.legendItem}>
            <span className={styles.legendDot} style={{ background: 'var(--color-high)' }} />
            {t('floorplan.legendOpen')}
          </span>
          <span className={styles.legendItem}>
            <span className={styles.legendDot} style={{ background: 'var(--color-critical)' }} />
            {t('floorplan.legendCritical')}
          </span>
        </div>
        <div className={styles.spacer} />
        {editMode && (
          <span className={styles.hint}>
            {placing
              ? t('floorplan.placeHint', { name: placing.code })
              : t('floorplan.editHint')}
          </span>
        )}
        {canEdit && (
          <button
            className={`${styles.editBtn} ${editMode ? styles.editActive : ''}`}
            onClick={() => { setEditMode(v => !v); setPlacing(null); }}
          >
            {editMode ? t('floorplan.doneEditing') : t('floorplan.editLayout')}
          </button>
        )}
      </div>

      <div className={styles.canvasCard}>
        <svg
          ref={svgRef}
          className={styles.svg}
          viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
          onPointerUp={editMode && placing ? handleCanvasClick : handleDotPointerUp}
          onPointerMove={handlePointerMove}
        >
          {placed.map(d => (
            <g
              key={d.deviceId}
              className={editMode ? styles.deviceDotEdit : styles.deviceDot}
              onPointerDown={e => handleDotPointerDown(e, d)}
              onPointerUp={handleDotPointerUp}
              onClick={() => handleDotClick(d)}
              onDoubleClick={() => handleDotDoubleClick(d)}
            >
              <title>
                {`${d.name} (${d.code}) — ${d.areaName}\n${d.statusName}` +
                  (d.openAlerts > 0 ? `\n${d.openAlerts} open alert(s)` : '')}
              </title>
              <circle
                className={d.criticalAlerts > 0 ? styles.pulse : undefined}
                cx={d.floorX!} cy={toViewY(d.floorY!)} r={2.1}
                fill={dotColor(d)}
                stroke="var(--color-surface)" strokeWidth={0.4}
              />
              <text className={styles.dotLabel} x={d.floorX!} y={toViewY(d.floorY!) + 4.2}>
                {d.code}
              </text>
            </g>
          ))}
        </svg>
      </div>

      {editMode && (
        <div className={styles.tray}>
          <h3 className={styles.trayTitle}>
            {t('floorplan.unplaced', { count: unplaced.length })}
          </h3>
          {unplaced.length === 0 ? (
            <span className={styles.hint}>{t('floorplan.allPlaced')}</span>
          ) : (
            <div className={styles.trayChips}>
              {unplaced.map(d => (
                <button
                  key={d.deviceId}
                  className={`${styles.trayChip} ${placing?.deviceId === d.deviceId ? styles.trayChipSelected : ''}`}
                  onClick={() => setPlacing(placing?.deviceId === d.deviceId ? null : d)}
                >
                  {d.name} ({d.code})
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
