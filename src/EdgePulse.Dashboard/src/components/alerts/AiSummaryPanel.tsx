import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { getAlertSummary } from '../../api/ai';
import styles from './AiSummaryPanel.module.css';

/**
 * Full-width panel rendered beneath an alert row. Fetches the AI summary on
 * mount (first time = the model runs, a few seconds; afterwards it is served
 * from the alert's cached AiSummary).
 *
 * The model is asked for three headed sections; we parse them for nice
 * rendering but fall back to plain text if a small model drifts from the
 * format — the content is still useful.
 */
interface Props {
  alertId: string;
  colSpan: number;
}

const HEADINGS = ['WHAT HAPPENED', 'LIKELY CAUSES', 'RECOMMENDED ACTION'] as const;

interface Section { title: string; body: string[] }

function parseSections(text: string): Section[] | null {
  const sections: Section[] = [];
  const re = /(WHAT HAPPENED|LIKELY CAUSES|RECOMMENDED ACTION)\s*:/gi;
  const parts = text.split(re).map(s => s.trim()).filter(Boolean);
  // split() yields [pre, H1, body1, H2, body2, ...]
  for (let i = 0; i < parts.length - 1; i++) {
    const maybeHeading = parts[i].toUpperCase();
    if ((HEADINGS as readonly string[]).includes(maybeHeading)) {
      const body = parts[i + 1]
        .split(/\n+/)
        .map(l => l.replace(/^[-•*]\s*/, '').trim())
        .filter(Boolean);
      sections.push({ title: maybeHeading, body });
      i++;
    }
  }
  return sections.length >= 2 ? sections : null;
}

export default function AiSummaryPanel({ alertId, colSpan }: Props) {
  const { t } = useTranslation();
  const qc = useQueryClient();

  const { data, isLoading, isFetching, refetch } = useQuery({
    queryKey: ['ai-summary', alertId],
    queryFn: () => getAlertSummary(alertId),
    staleTime: Infinity,        // cached server-side; no need to refetch
    retry: false,
  });

  async function regenerate() {
    const fresh = await getAlertSummary(alertId, true);
    qc.setQueryData(['ai-summary', alertId], fresh);
  }

  const sections = data?.summary ? parseSections(data.summary) : null;

  return (
    <tr className={styles.panelRow}>
      <td colSpan={colSpan}>
        <div className={styles.panel}>
          <div className={styles.header}>
            <span>✦ {t('ai.summaryTitle')}</span>
            {data?.provider && <span className={styles.provider}>{data.provider}</span>}
          </div>

          {(isLoading || isFetching) && (
            <div className={styles.loading}>
              <span className={styles.spinner} />
              {t('ai.thinking')}
            </div>
          )}

          {!isLoading && !isFetching && data && !data.available && (
            <div className={styles.unavailable}>
              {data.reason ?? t('ai.unavailable')}
              <div className={styles.footer}>
                <button className={styles.smallBtn} onClick={() => refetch()}>{t('ai.retry')}</button>
              </div>
            </div>
          )}

          {!isLoading && !isFetching && data?.available && data.summary && (
            <>
              {sections ? (
                sections.map(s => (
                  <div key={s.title} className={styles.section}>
                    <div className={styles.sectionTitle}>{t(`ai.sections.${s.title}`)}</div>
                    {s.body.length === 1
                      ? <div className={styles.plain}>{s.body[0]}</div>
                      : <ul>{s.body.map((line, i) => <li key={i}>{line}</li>)}</ul>}
                  </div>
                ))
              ) : (
                <div className={styles.plain}>{data.summary}</div>
              )}
              <div className={styles.footer}>
                <button className={styles.smallBtn} onClick={regenerate}>{t('ai.regenerate')}</button>
                {data.fromCache && <span className={styles.unavailable}>{t('ai.cached')}</span>}
              </div>
              <div className={styles.disclaimer}>{t('ai.disclaimer')}</div>
            </>
          )}
        </div>
      </td>
    </tr>
  );
}
