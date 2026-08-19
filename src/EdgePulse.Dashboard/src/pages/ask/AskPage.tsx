import { useState, useRef, useEffect, type FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { askQuestion, getAiStatus, type AskResult } from '../../api/ai';
import styles from './AskPage.module.css';

/**
 * "Ask EdgePulse" — a small chat-style page over POST /api/ai/ask.
 *
 * Each turn is independent on the server (no conversation memory): the API
 * re-grounds every question in live data. The thread shown here is purely a
 * client-side history for the session.
 *
 * Optional ?deviceId=…&deviceLabel=… (from the device detail page) focuses
 * every question on that device until the chip is cleared.
 */
interface Turn {
  id: number;
  question: string;
  result?: AskResult;      // undefined while loading
  error?: boolean;
}

const EXAMPLE_KEYS = ['ex1', 'ex2', 'ex3', 'ex4'] as const;

export default function AskPage() {
  const { t } = useTranslation();
  const [params, setParams] = useSearchParams();
  const deviceId = params.get('deviceId') ?? undefined;
  const deviceLabel = params.get('deviceLabel') ?? undefined;

  const { data: status } = useQuery({ queryKey: ['ai-status'], queryFn: getAiStatus, staleTime: 5 * 60_000 });

  const [question, setQuestion] = useState('');
  const [turns, setTurns] = useState<Turn[]>([]);
  const [busy, setBusy] = useState(false);
  const nextId = useRef(1);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [turns]);

  const clearDevice = () => {
    params.delete('deviceId'); params.delete('deviceLabel');
    setParams(params, { replace: true });
  };

  const ask = async (q: string) => {
    const text = q.trim();
    if (!text || busy) return;
    const id = nextId.current++;
    setTurns(prev => [...prev, { id, question: text }]);
    setQuestion('');
    setBusy(true);
    try {
      const result = await askQuestion(text, deviceId);
      setTurns(prev => prev.map(x => x.id === id ? { ...x, result } : x));
    } catch {
      setTurns(prev => prev.map(x => x.id === id ? { ...x, error: true } : x));
    } finally {
      setBusy(false);
    }
  };

  const onSubmit = (e: FormEvent) => { e.preventDefault(); void ask(question); };

  if (status && !status.enabled) {
    return (
      <div className={styles.page}>
        <div className={styles.disabledCard}>{t('ask.disabled')}</div>
      </div>
    );
  }

  const groundingText = (g: AskResult['grounding']) => {
    const parts: string[] = [];
    if (g.devices.length) parts.push(g.devices.join(', '));
    else parts.push(t('ask.groundPlant'));
    parts.push(t('ask.groundAlerts', { count: g.alerts }));
    parts.push(t('ask.groundWorkOrders', { count: g.workOrders }));
    return parts.join(' · ');
  };

  return (
    <div className={styles.page}>
      <p className={styles.hint}>{t('ask.hint')}</p>

      <div className={styles.card}>
        <form className={styles.form} onSubmit={onSubmit}>
          <textarea
            className={styles.input}
            value={question}
            maxLength={500}
            placeholder={deviceLabel ? t('ask.placeholderDevice', { device: deviceLabel }) : t('ask.placeholder')}
            onChange={e => setQuestion(e.target.value)}
            onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); void ask(question); } }}
            disabled={busy}
          />
          <button type="submit" className={styles.askBtn} disabled={busy || !question.trim()}>
            {busy ? t('ask.thinking') : t('ask.ask')}
          </button>
        </form>

        <div className={styles.meta}>
          {deviceLabel && (
            <span className={styles.scopeChip}>
              {t('ask.focusedOn', { device: deviceLabel })}
              <button type="button" onClick={clearDevice} aria-label={t('common.clear')}>✕</button>
            </span>
          )}
          {turns.length === 0 && (
            <div className={styles.examples}>
              {EXAMPLE_KEYS.map(k => (
                <button key={k} type="button" className={styles.example} onClick={() => void ask(t(`ask.${k}`))}>
                  {t(`ask.${k}`)}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>

      {turns.length > 0 && (
        <div className={`${styles.card} ${styles.thread}`}>
          {turns.map(turn => (
            <div key={turn.id} className={styles.turn}>
              <div className={styles.question}>{turn.question}</div>
              {!turn.result && !turn.error && (
                <div className={styles.loading}><span className={styles.spinner} />{t('ask.thinkingLong')}</div>
              )}
              {turn.error && <div className={styles.answerUnavailable}>{t('ask.requestFailed')}</div>}
              {turn.result && turn.result.available && (
                <>
                  <div className={styles.answer}>{turn.result.answer}</div>
                  <div className={styles.grounding}>
                    {t('ask.groundedOn')}: {groundingText(turn.result.grounding)}
                  </div>
                </>
              )}
              {turn.result && !turn.result.available && (
                <div className={styles.answerUnavailable}>
                  {turn.result.reason ?? t('ask.unavailable')}
                </div>
              )}
            </div>
          ))}
          <div ref={bottomRef} />
          <p className={styles.disclaimer}>{t('ask.disclaimer', { provider: status?.provider ?? '' })}</p>
        </div>
      )}
    </div>
  );
}
