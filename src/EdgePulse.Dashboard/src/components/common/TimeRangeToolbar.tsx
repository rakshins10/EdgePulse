import type { TimeRangeResult, Scale } from '../../hooks/useTimeRange';
import { toInputDate } from '../../hooks/useTimeRange';
import styles from './TimeRangeToolbar.module.css';

interface Props {
  range: TimeRangeResult;
  compact?: boolean;
}

const SCALES: Scale[] = ['day', 'week', 'month', 'year', 'custom'];

export default function TimeRangeToolbar({ range, compact = false }: Props) {
  const {
    scale, periodLabel, canNext,
    customFromInput, customToInput,
    handleScaleChange, handleNav,
    setCustomFromInput, setCustomToInput,
    handleApplyCustom,
  } = range;

  return (
    <div className={`${styles.toolbar} ${compact ? styles.compact : ''}`}>
      <div className={styles.scaleGroup}>
        {SCALES.map(s => (
          <button
            key={s}
            className={`${styles.scaleBtn} ${scale === s ? styles.scaleBtnActive : ''}`}
            onClick={() => handleScaleChange(s)}
          >
            {s.charAt(0).toUpperCase() + s.slice(1)}
          </button>
        ))}
      </div>

      {scale !== 'custom' && (
        <div className={styles.navRow}>
          <button
            className={styles.navBtn}
            onClick={() => handleNav(-1)}
            title="Previous period"
          >
            ‹
          </button>
          <span className={styles.periodLabel}>{periodLabel}</span>
          <button
            className={styles.navBtn}
            onClick={() => handleNav(1)}
            disabled={!canNext}
            title="Next period"
          >
            ›
          </button>
        </div>
      )}

      {scale === 'custom' && (
        <div className={styles.customRow}>
          <label className={styles.dateLabel}>From</label>
          <input
            type="date"
            className={styles.dateInput}
            value={customFromInput}
            max={customToInput}
            onChange={e => setCustomFromInput(e.target.value)}
          />
          <label className={styles.dateLabel}>To</label>
          <input
            type="date"
            className={styles.dateInput}
            value={customToInput}
            min={customFromInput}
            max={toInputDate(new Date())}
            onChange={e => setCustomToInput(e.target.value)}
          />
          <button className={styles.applyBtn} onClick={handleApplyCustom}>
            Apply
          </button>
          <span className={styles.periodLabel}>{periodLabel}</span>
        </div>
      )}
    </div>
  );
}
