import { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { SUPPORTED_LANGUAGES } from '../../i18n';
import styles from './LanguageSwitcher.module.css';

export default function LanguageSwitcher() {
  const { i18n, t } = useTranslation();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const current = SUPPORTED_LANGUAGES.find(l => l.code === i18n.resolvedLanguage)
    ?? SUPPORTED_LANGUAGES[0];

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    if (open) document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, [open]);

  function change(code: string) {
    void i18n.changeLanguage(code);
    setOpen(false);
  }

  return (
    <div className={styles.wrapper} ref={ref}>
      <button
        className={styles.button}
        onClick={() => setOpen(v => !v)}
        aria-label={t('language.label')}
        aria-haspopup="listbox"
        aria-expanded={open}
        title={t('language.label')}
      >
        <span className={styles.flag}>{current.flag}</span>
        <span className={styles.code}>{current.code.toUpperCase()}</span>
      </button>

      {open && (
        <ul className={styles.menu} role="listbox">
          {SUPPORTED_LANGUAGES.map(lang => (
            <li key={lang.code}>
              <button
                className={`${styles.item} ${lang.code === current.code ? styles.itemActive : ''}`}
                onClick={() => change(lang.code)}
                role="option"
                aria-selected={lang.code === current.code}
                title={t('language.switchTo', { language: lang.label })}
              >
                <span className={styles.flag}>{lang.flag}</span>
                <span>{lang.label}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
