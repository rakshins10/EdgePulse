import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import type { TelemetryReadingDto } from '../../types/api';
import styles from './TelemetryChart.module.css';

interface Props {
  metricKey: string;
  readings:  TelemetryReadingDto[];
  color?:    string;
  unit?:     string | null;
  from:      Date;
  to:        Date;
}

interface DataPoint {
  ts:    number;
  value: number;
  unit:  string;
}

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4'];

const HOUR_MS  =          3_600_000;
const DAY_MS   =         86_400_000;
const WEEK_MS  =  7 * DAY_MS;
const MONTH_MS = 31 * DAY_MS;

function generateTicks(fromMs: number, toMs: number, spanMs: number): number[] {
  const ticks: number[] = [];

  if (spanMs <= 2 * DAY_MS) {
    const d = new Date(fromMs);
    d.setHours(0, 0, 0, 0);
    while (d.getTime() <= toMs) {
      if (d.getTime() >= fromMs) ticks.push(d.getTime());
      d.setTime(d.getTime() + 4 * HOUR_MS);
    }
  } else if (spanMs <= WEEK_MS) {
    const d = new Date(fromMs);
    d.setHours(0, 0, 0, 0);
    while (d.getTime() <= toMs) {
      ticks.push(d.getTime());
      d.setDate(d.getDate() + 1);
    }
  } else if (spanMs <= MONTH_MS) {
    const d = new Date(fromMs);
    d.setDate(1);
    d.setHours(0, 0, 0, 0);
    while (d.getTime() <= toMs) {
      if (d.getTime() >= fromMs) ticks.push(d.getTime());
      d.setDate(d.getDate() + 5);
    }
  } else {
    const d = new Date(fromMs);
    d.setDate(1);
    d.setHours(0, 0, 0, 0);
    while (d.getTime() <= toMs) {
      ticks.push(d.getTime());
      d.setMonth(d.getMonth() + 1);
    }
  }

  return ticks;
}

function makeFmtTick(spanMs: number) {
  return (ts: number): string => {
    const d = new Date(ts);
    if (spanMs <= 2 * DAY_MS) {
      return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }
    if (spanMs <= MONTH_MS) {
      return d.toLocaleDateString([], { weekday: 'short', day: 'numeric' });
    }
    return d.toLocaleDateString([], { month: 'short', year: '2-digit' });
  };
}

function ChartTooltip(props: { active?: boolean; payload?: { payload: DataPoint }[] }) {
  const { active, payload } = props;
  if (!active || !payload?.length) return null;
  const d  = payload[0].payload;
  const dt = new Date(d.ts);

  const datePart = dt.toLocaleDateString([], {
    weekday: 'short', day: 'numeric', month: 'short', year: 'numeric',
  });
  const timePart = dt.toLocaleTimeString([], {
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  });

  return (
    <div className={styles.tooltip}>
      <div className={styles.tooltipValue}>
        {d.value.toFixed(2)}{d.unit ? ` ${d.unit}` : ''}
      </div>
      <div className={styles.tooltipDate}>{datePart}</div>
      <div className={styles.tooltipTime}>{timePart}</div>
    </div>
  );
}

export default function TelemetryChart({ metricKey, readings, color, unit, from, to }: Props) {
  const data: DataPoint[] = [...readings]
    .reverse()
    .flatMap(r => {
      const m = r.metrics.find(x => x.key === metricKey);
      if (!m) return [];
      return [{ ts: new Date(r.timestamp).getTime(), value: m.value, unit: m.unit ?? unit ?? '' }];
    });

  const latest       = data[data.length - 1];
  const resolvedUnit = latest?.unit ?? unit ?? '';
  const stroke       = color ?? COLORS[metricKey.length % COLORS.length];

  const periodFromMs = from.getTime();
  const periodToMs   = to.getTime();
  const spanMs       = periodToMs - periodFromMs;

  const ticks   = generateTicks(periodFromMs, periodToMs, spanMs);
  const fmtTick = makeFmtTick(spanMs);

  if (data.length === 0) {
    return (
      <div className={styles.chartWrap}>
        <p className={styles.metricTitle}>{metricKey}</p>
        <ResponsiveContainer width="100%" height={100}>
          <AreaChart data={[]} margin={{ top: 4, right: 16, left: 0, bottom: 0 }}>
            <CartesianGrid strokeDasharray="4 3" stroke="var(--color-border)" vertical={false} />
            <XAxis
              type="number"
              dataKey="ts"
              scale="time"
              domain={[periodFromMs, periodToMs]}
              ticks={ticks}
              tickFormatter={fmtTick}
              tick={{ fontSize: 11, fill: 'var(--color-text-secondary)' }}
              axisLine={false}
              tickLine={false}
            />
            <YAxis hide />
          </AreaChart>
        </ResponsiveContainer>
        <div className={styles.noData}>No data in this period</div>
      </div>
    );
  }

  const values = data.map(d => d.value);
  const minV   = Math.min(...values);
  const maxV   = Math.max(...values);
  const pad    = (maxV - minV) * 0.15 || 1;

  return (
    <div className={styles.chartWrap}>
      <p className={styles.metricTitle}>{metricKey}</p>
      <p className={styles.currentValue}>
        {latest.value.toFixed(2)}
        {resolvedUnit && <span className={styles.unit}> {resolvedUnit}</span>}
      </p>

      <ResponsiveContainer width="100%" height={180}>
        <AreaChart data={data} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
          <defs>
            <linearGradient id={`grad-${metricKey}`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%"  stopColor={stroke} stopOpacity={0.25} />
              <stop offset="95%" stopColor={stroke} stopOpacity={0.02} />
            </linearGradient>
          </defs>

          <CartesianGrid strokeDasharray="4 3" stroke="var(--color-border)" vertical={false} />

          <XAxis
            dataKey="ts"
            type="number"
            scale="time"
            domain={[periodFromMs, periodToMs]}
            ticks={ticks}
            tickFormatter={fmtTick}
            tick={{ fontSize: 11, fill: 'var(--color-text-secondary)' }}
            axisLine={false}
            tickLine={false}
            minTickGap={50}
          />

          <YAxis
            domain={[minV - pad, maxV + pad]}
            tickFormatter={(v: number) => v % 1 === 0 ? v.toFixed(0) : v.toFixed(1)}
            tick={{ fontSize: 11, fill: 'var(--color-text-secondary)' }}
            axisLine={false}
            tickLine={false}
            width={44}
            tickCount={4}
          />

          <Tooltip content={<ChartTooltip />} />

          <Area
            type="monotone"
            dataKey="value"
            stroke={stroke}
            strokeWidth={2}
            fill={`url(#grad-${metricKey})`}
            dot={data.length <= 40 ? { r: 4, fill: stroke, strokeWidth: 0 } : false}
            activeDot={{ r: 6, fill: stroke, strokeWidth: 2, stroke: '#fff' }}
            isAnimationActive={false}
          />
        </AreaChart>
      </ResponsiveContainer>

      <div className={styles.minMax}>
        <span>Min: {minV.toFixed(2)} {resolvedUnit}</span>
        <span>{data.length} readings</span>
        <span>Max: {maxV.toFixed(2)} {resolvedUnit}</span>
      </div>
    </div>
  );
}
