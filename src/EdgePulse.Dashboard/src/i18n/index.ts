import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import en from './locales/en.json';
import fi from './locales/fi.json';
import sv from './locales/sv.json';

export const SUPPORTED_LANGUAGES = [
  { code: 'en', label: 'English',  flag: '🇬🇧' },
  { code: 'fi', label: 'Suomi',    flag: '🇫🇮' },
  { code: 'sv', label: 'Svenska',  flag: '🇸🇪' },
] as const;

export type LanguageCode = typeof SUPPORTED_LANGUAGES[number]['code'];

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      fi: { translation: fi },
      sv: { translation: sv },
    },
    fallbackLng: 'en',
    supportedLngs: SUPPORTED_LANGUAGES.map(l => l.code),
    interpolation: { escapeValue: false },
    detection: {
      order: ['localStorage', 'navigator', 'htmlTag'],
      lookupLocalStorage: 'edgepulse-lang',
      caches: ['localStorage'],
    },
    returnNull: false,
  });

export default i18n;
