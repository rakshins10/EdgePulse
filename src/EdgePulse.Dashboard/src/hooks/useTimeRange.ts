import { useState, useMemo } from 'react';

export type Scale = 'day' | 'week' | 'month' | 'year' | 'custom';

export interface TimeRangeResult {
  scale: Scale;
  from: Date;
  to: Date;
  periodLabel: string;
  canNext: boolean;
  isLive: boolean;
  customFromInput: string;
  customToInput: string;
  handleScaleChange: (s: Scale) => void;
  handleNav: (dir: -1 | 1) => void;
  setCustomFromInput: (s: string) => void;
  setCustomToInput: (s: string) => void;
  handleApplyCustom: () => void;
}

// ── date helpers ──────────────────────────────────────────────────────────────

export function toInputDate(d: Date) { return d.toISOString().slice(0, 10); }

function startOfDay(d: Date)   { const r = new Date(d); r.setHours(0,0,0,0); return r; }
function endOfDay(d: Date)     { const r = new Date(d); r.setHours(23,59,59,999); return r; }
function startOfWeek(d: Date)  {
  const r = new Date(d); const day = r.getDay();
  r.setDate(r.getDate() - (day === 0 ? 6 : day - 1)); return startOfDay(r);
}
function endOfWeek(d: Date)    { const r = startOfWeek(d); r.setDate(r.getDate() + 6); return endOfDay(r); }
function startOfMonth(d: Date) { return startOfDay(new Date(d.getFullYear(), d.getMonth(), 1)); }
function endOfMonth(d: Date)   { return endOfDay(new Date(d.getFullYear(), d.getMonth() + 1, 0)); }
function startOfYear(d: Date)  { return startOfDay(new Date(d.getFullYear(), 0, 1)); }
function endOfYear(d: Date)    { return endOfDay(new Date(d.getFullYear(), 11, 31)); }

export function getPeriodBounds(scale: Scale, base: Date): { from: Date; to: Date } {
  switch (scale) {
    case 'day':   return { from: startOfDay(base),   to: endOfDay(base)   };
    case 'week':  return { from: startOfWeek(base),  to: endOfWeek(base)  };
    case 'month': return { from: startOfMonth(base), to: endOfMonth(base) };
    case 'year':  return { from: startOfYear(base),  to: endOfYear(base)  };
    default:      return { from: startOfDay(base),   to: endOfDay(base)   };
  }
}

export function navigateBase(scale: Scale, base: Date, dir: -1 | 1): Date {
  const d = new Date(base);
  switch (scale) {
    case 'day':   d.setDate(d.getDate() + dir);         break;
    case 'week':  d.setDate(d.getDate() + dir * 7);     break;
    case 'month': d.setMonth(d.getMonth() + dir);       break;
    case 'year':  d.setFullYear(d.getFullYear() + dir); break;
  }
  return d;
}

export function formatPeriodLabel(scale: Scale, from: Date, to: Date): string {
  const fmt = (d: Date, opts: Intl.DateTimeFormatOptions) => d.toLocaleDateString(undefined, opts);
  switch (scale) {
    case 'day':
      return fmt(from, { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' });
    case 'week':
      return `${fmt(from, { day: 'numeric', month: 'short' })} – ${fmt(to, { day: 'numeric', month: 'short', year: 'numeric' })}`;
    case 'month':
      return fmt(from, { month: 'long', year: 'numeric' });
    case 'year':
      return String(from.getFullYear());
    case 'custom':
      return `${fmt(from, { day: 'numeric', month: 'short', year: 'numeric' })} – ${fmt(to, { day: 'numeric', month: 'short', year: 'numeric' })}`;
  }
}

// ── hook ──────────────────────────────────────────────────────────────────────

export function useTimeRange(): TimeRangeResult {
  const [scale,    setScale]    = useState<Scale>('day');
  const [baseDate, setBaseDate] = useState(() => new Date());

  const [customFromInput, setCustomFromInput] = useState(() => toInputDate(new Date()));
  const [customToInput,   setCustomToInput]   = useState(() => toInputDate(new Date()));
  const [appliedFrom,     setAppliedFrom]     = useState(() => startOfDay(new Date()));
  const [appliedTo,       setAppliedTo]       = useState(() => endOfDay(new Date()));

  const { from, to } = useMemo(() => {
    if (scale === 'custom') return { from: appliedFrom, to: appliedTo };
    return getPeriodBounds(scale, baseDate);
  }, [scale, baseDate, appliedFrom, appliedTo]);

  const periodLabel = formatPeriodLabel(scale, from, to);
  const canNext     = to < new Date();
  const isLive      = scale !== 'custom' && to >= new Date();

  function handleScaleChange(s: Scale) { setScale(s); setBaseDate(new Date()); }
  function handleNav(dir: -1 | 1)     { setBaseDate(prev => navigateBase(scale, prev, dir)); }
  function handleApplyCustom() {
    if (!customFromInput || !customToInput) return;
    setAppliedFrom(new Date(customFromInput + 'T00:00:00'));
    setAppliedTo(new Date(customToInput   + 'T23:59:59'));
  }

  return {
    scale, from, to, periodLabel, canNext, isLive,
    customFromInput, customToInput,
    handleScaleChange, handleNav,
    setCustomFromInput, setCustomToInput,
    handleApplyCustom,
  };
}
