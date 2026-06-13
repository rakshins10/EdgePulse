import i18n from './index';
import { getUiStrings, getLookupSourceItems, getLookupTranslations } from '../api/localization';

// ── Flatten / runtime overrides ─────────────────────────────────────────────

/** Flatten a nested translation object into dot-path keys. */
export function flatten(obj: Record<string, unknown>, prefix = ''): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [k, v] of Object.entries(obj)) {
    const key = prefix ? `${prefix}.${k}` : k;
    if (v && typeof v === 'object' && !Array.isArray(v)) {
      Object.assign(out, flatten(v as Record<string, unknown>, key));
    } else if (typeof v === 'string') {
      out[key] = v;
    }
  }
  return out;
}

/** The bundled English UI strings, flattened — the canonical key registry. */
export function englishUiStrings(): Record<string, string> {
  const bundle = i18n.getResourceBundle('en', 'translation') as Record<string, unknown> | undefined;
  return bundle ? flatten(bundle) : {};
}

/** Fetch DB UI overrides for a locale and layer them onto i18next at runtime. */
export async function loadUiOverrides(locale: string): Promise<void> {
  if (!locale || locale === 'en') return; // English is the bundled source
  try {
    const overrides = await getUiStrings(locale);
    for (const [key, value] of Object.entries(overrides)) {
      if (value) i18n.addResource(locale, 'translation', key, value);
    }
    // Nudge react-i18next consumers to re-render with the new resources.
    if (i18n.resolvedLanguage === locale) {
      void i18n.changeLanguage(locale);
    }
  } catch {
    // Non-fatal: fall back to bundled JSON.
  }
}

// ── CSV build / parse ───────────────────────────────────────────────────────

function csvEscape(value: string): string {
  if (/[",\n\r]/.test(value)) return `"${value.replace(/"/g, '""')}"`;
  return value;
}

export interface CsvRow {
  category: 'UI' | 'Lookup';
  key: string;          // UI: dot-key. Lookup: "LookupType/itemId"
  source: string;       // English source
  translation: string;  // current locale translation (may be empty)
}

export function buildCsv(rows: CsvRow[]): string {
  const header = 'Category,Key,Source,Translation';
  const lines = rows.map(r =>
    [r.category, r.key, r.source, r.translation].map(csvEscape).join(','));
  return [header, ...lines].join('\r\n');
}

/** Minimal RFC-4180 CSV parser (handles quoted fields, commas, newlines). */
export function parseCsv(text: string): string[][] {
  const rows: string[][] = [];
  let row: string[] = [];
  let field = '';
  let inQuotes = false;
  let i = 0;

  // strip BOM
  if (text.charCodeAt(0) === 0xfeff) text = text.slice(1);

  while (i < text.length) {
    const c = text[i];
    if (inQuotes) {
      if (c === '"') {
        if (text[i + 1] === '"') { field += '"'; i += 2; continue; }
        inQuotes = false; i++; continue;
      }
      field += c; i++; continue;
    }
    if (c === '"') { inQuotes = true; i++; continue; }
    if (c === ',') { row.push(field); field = ''; i++; continue; }
    if (c === '\r') { i++; continue; }
    if (c === '\n') { row.push(field); rows.push(row); row = []; field = ''; i++; continue; }
    field += c; i++;
  }
  // last field/row
  if (field.length > 0 || row.length > 0) { row.push(field); rows.push(row); }
  return rows.filter(r => r.length > 1 || (r.length === 1 && r[0] !== ''));
}

// ── Export / import orchestration ───────────────────────────────────────────

/** Build the full CSV (UI + lookups) for a target locale. */
export async function buildExportCsv(locale: string): Promise<string> {
  const rows: CsvRow[] = [];

  // UI strings: English source from bundle, translation from DB overrides.
  const enUi = englishUiStrings();
  const uiOverrides = locale === 'en' ? {} : await getUiStrings(locale).catch(() => ({}));
  for (const [key, source] of Object.entries(enUi)) {
    rows.push({
      category: 'UI',
      key,
      source,
      translation: uiOverrides[key] ?? '',
    });
  }

  // Lookup items: English source + current translations for this locale.
  const sourceItems = await getLookupSourceItems();
  const byType = new Map<string, Map<string, string>>(); // type -> (itemId -> translated)
  const types = Array.from(new Set(sourceItems.map(s => s.lookupType)));
  for (const type of types) {
    const trs = await getLookupTranslations(type, locale).catch(() => []);
    const m = new Map<string, string>();
    trs.forEach(t => { if (t.translatedName) m.set(t.itemId, t.translatedName); });
    byType.set(type, m);
  }
  for (const it of sourceItems) {
    rows.push({
      category: 'Lookup',
      key: `${it.lookupType}/${it.itemId}`,
      source: it.sourceName,
      translation: byType.get(it.lookupType)?.get(it.itemId) ?? '',
    });
  }

  return buildCsv(rows);
}

export interface ImportResult { uiAffected: number; lookupAffected: number; skipped: number; }

/** Parse an uploaded CSV and split rows into UI + lookup entries. */
export function splitImportRows(text: string): {
  ui: { key: string; value?: string }[];
  lookups: { lookupType: string; itemId: string; name?: string }[];
  skipped: number;
} {
  const rows = parseCsv(text);
  const ui: { key: string; value?: string }[] = [];
  const lookups: { lookupType: string; itemId: string; name?: string }[] = [];
  let skipped = 0;

  for (let r = 0; r < rows.length; r++) {
    const cols = rows[r];
    if (r === 0 && cols[0]?.toLowerCase() === 'category') continue; // header
    if (cols.length < 4) { skipped++; continue; }
    const [category, key, , translation] = cols;
    if (category === 'UI') {
      ui.push({ key, value: translation });
    } else if (category === 'Lookup') {
      const slash = key.indexOf('/');
      if (slash < 0) { skipped++; continue; }
      lookups.push({
        lookupType: key.slice(0, slash),
        itemId: key.slice(slash + 1),
        name: translation,
      });
    } else {
      skipped++;
    }
  }
  return { ui, lookups, skipped };
}
