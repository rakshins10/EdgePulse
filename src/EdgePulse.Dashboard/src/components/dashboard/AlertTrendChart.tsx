import type { AlertTrendDay } from '../../types/dashboard';
import styles from './AlertTrendChart.module.css';

interface AlertTrendChartProps {
  data: AlertTrendDay[];
}

// Chart layout constants
const SVG_W = 600;
const SVG_H = 180;
const PAD_L = 36;  // space for y-axis labels
const PAD_R = 12;
const PAD_T = 16;
const PAD_B = 32;  // space for x-axis labels

const CHART_W = SVG_W - PAD_L - PAD_R;
const CHART_H = SVG_H - PAD_T - PAD_B;

function shortDay(isoDate: string): string {
  const d = new Date(isoDate + 'T00:00:00');
  return d.toLocaleDateString('en-US', { weekday: 'short' });
}

function shortDate(isoDate: string): string {
  const d = new Date(isoDate + 'T00:00:00');
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

export default function AlertTrendChart({ data }: AlertTrendChartProps) {
  if (!data || data.length === 0) {
    return (
      <div className={styles.empty}>No trend data available.</div>
    );
  }

  const maxCount = Math.max(...data.map(d => d.count), 1);

  // Round up the y-axis ceiling to a clean number
  const yMax = Math.ceil(maxCount / 5) * 5 || 5;
  const yTicks = [0, Math.round(yMax / 2), yMax];

  const barCount = data.length;
  const gap      = 8;
  const barW     = (CHART_W - gap * (barCount - 1)) / barCount;

  return (
    <div className={styles.wrapper}>
      <svg
        viewBox={`0 0 ${SVG_W} ${SVG_H}`}
        className={styles.svg}
        role="img"
        aria-label="7-day alert trend bar chart"
      >
        {/* ── Y-axis grid lines & labels ───────────────────────────── */}
        {yTicks.map(tick => {
          const y = PAD_T + CHART_H - (tick / yMax) * CHART_H;
          return (
            <g key={tick}>
              <line
                x1={PAD_L}
                y1={y}
                x2={PAD_L + CHART_W}
                y2={y}
                className={styles.gridLine}
              />
              <text x={PAD_L - 6} y={y + 4} className={styles.yLabel}>
                {tick}
              </text>
            </g>
          );
        })}

        {/* ── Bars ────────────────────────────────────────────────── */}
        {data.map((day, i) => {
          const barH    = (day.count / yMax) * CHART_H;
          const x       = PAD_L + i * (barW + gap);
          const y       = PAD_T + CHART_H - barH;
          const isEmpty = day.count === 0;

          return (
            <g key={day.date}>
              {/* Bar */}
              <rect
                x={x}
                y={isEmpty ? y - 2 : y}
                width={barW}
                height={isEmpty ? 2 : barH}
                rx={3}
                className={isEmpty ? styles.barEmpty : styles.bar}
              />

              {/* Count label above bar */}
              {day.count > 0 && (
                <text
                  x={x + barW / 2}
                  y={y - 4}
                  className={styles.barLabel}
                >
                  {day.count}
                </text>
              )}

              {/* X-axis label */}
              <text
                x={x + barW / 2}
                y={PAD_T + CHART_H + 14}
                className={styles.xLabel}
              >
                {shortDay(day.date)}
              </text>
              <text
                x={x + barW / 2}
                y={PAD_T + CHART_H + 26}
                className={styles.xSubLabel}
              >
                {shortDate(day.date)}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
}
