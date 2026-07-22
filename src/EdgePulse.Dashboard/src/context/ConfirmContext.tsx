import { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import ConfirmDialog, { type ConfirmVariant } from '../components/common/ConfirmDialog';

export interface ConfirmOptions {
  /** Explicit heading. If omitted, the first paragraph of `message` is used. */
  title?: string;
  /** Body text. Supports "\n\n" — the first paragraph becomes the title when none is given. */
  message?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: ConfirmVariant;
}

/** Returns a promise that resolves true (confirmed) or false (cancelled/dismissed). */
export type ConfirmFn = (opts: ConfirmOptions | string) => Promise<boolean>;

const ConfirmContext = createContext<ConfirmFn | null>(null);

// eslint-disable-next-line react-refresh/only-export-components
export function useConfirm(): ConfirmFn {
  const ctx = useContext(ConfirmContext);
  if (!ctx) throw new Error('useConfirm must be used within a ConfirmProvider');
  return ctx;
}

interface DialogState {
  open: boolean;
  title: string;
  message?: string;
  confirmLabel: string;
  cancelLabel: string;
  variant: ConfirmVariant;
}

export function ConfirmProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const [state, setState] = useState<DialogState>({
    open: false, title: '', confirmLabel: '', cancelLabel: '', variant: 'danger',
  });
  const resolver = useRef<((result: boolean) => void) | null>(null);

  const confirm = useCallback<ConfirmFn>((opts) => {
    const o = typeof opts === 'string' ? { message: opts } : opts;

    // If no explicit title, split "Question?\n\nDetail." into title + message.
    let title = o.title;
    let message = o.message;
    if (!title && message) {
      const idx = message.indexOf('\n\n');
      if (idx !== -1) {
        title = message.slice(0, idx).trim();
        message = message.slice(idx + 2).trim();
      } else {
        title = message;
        message = undefined;
      }
    }

    setState({
      open: true,
      title: title ?? '',
      message,
      confirmLabel: o.confirmLabel ?? t('common.confirm'),
      cancelLabel: o.cancelLabel ?? t('common.cancel'),
      variant: o.variant ?? 'danger',
    });

    return new Promise<boolean>(resolve => { resolver.current = resolve; });
  }, [t]);

  const settle = useCallback((result: boolean) => {
    setState(s => ({ ...s, open: false }));
    resolver.current?.(result);
    resolver.current = null;
  }, []);

  const value = useMemo(() => confirm, [confirm]);

  return (
    <ConfirmContext.Provider value={value}>
      {children}
      <ConfirmDialog
        open={state.open}
        title={state.title}
        message={state.message}
        confirmLabel={state.confirmLabel}
        cancelLabel={state.cancelLabel}
        variant={state.variant}
        onConfirm={() => settle(true)}
        onCancel={() => settle(false)}
      />
    </ConfirmContext.Provider>
  );
}
