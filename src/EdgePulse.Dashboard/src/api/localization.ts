import apiClient from './client';

export interface LocaleDto {
  id: string;
  code: string;
  displayName: string;
  nativeName: string;
  flag: string | null;
  isEnabled: boolean;
  isDefault: boolean;
  sortOrder: number;
}

export interface LookupTranslationRow {
  itemId: string;
  sourceName: string;
  sourceDescription: string | null;
  translatedName: string | null;
  translatedDescription: string | null;
}

// ── Locales ──────────────────────────────────────────────────────────────────

export const getLocales = (enabledOnly = false): Promise<LocaleDto[]> =>
  apiClient
    .get<LocaleDto[]>('/localization/locales', { params: { enabledOnly } })
    .then(r => r.data);

export interface CreateLocaleBody {
  code: string;
  displayName: string;
  nativeName: string;
  flag?: string;
  isEnabled: boolean;
  sortOrder: number;
}

export const createLocale = (body: CreateLocaleBody): Promise<string> =>
  apiClient.post<string>('/localization/locales', body).then(r => r.data);

export interface UpdateLocaleBody {
  displayName: string;
  nativeName: string;
  flag?: string;
  isEnabled: boolean;
  sortOrder: number;
}

export const updateLocale = (id: string, body: UpdateLocaleBody): Promise<void> =>
  apiClient.put(`/localization/locales/${id}`, body).then(() => undefined);

export const deleteLocale = (id: string): Promise<void> =>
  apiClient.delete(`/localization/locales/${id}`).then(() => undefined);

export const setDefaultLocale = (id: string): Promise<void> =>
  apiClient.post(`/localization/locales/${id}/set-default`).then(() => undefined);

// ── Lookup translations ────────────────────────────────────────────────────

export const getLookupTranslations = (
  lookupType: string,
  locale: string,
): Promise<LookupTranslationRow[]> =>
  apiClient
    .get<LookupTranslationRow[]>('/localization/translations', {
      params: { lookupType, locale },
    })
    .then(r => r.data);

export interface UpsertTranslationBody {
  lookupType: string;
  itemId: string;
  localeCode: string;
  name?: string;
  description?: string;
}

export const upsertLookupTranslation = (body: UpsertTranslationBody): Promise<void> =>
  apiClient.put('/localization/translations', body).then(() => undefined);
