import axios from 'axios';
import keycloak from '../keycloak';
import i18n from '../i18n';

const apiClient = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

// Attach Keycloak bearer token + current UI language to every request
apiClient.interceptors.request.use(async (config) => {
  if (keycloak.authenticated) {
    // Refresh token if expiring in the next 30 seconds
    try {
      await keycloak.updateToken(30);
    } catch {
      keycloak.login();
    }
    config.headers.Authorization = `Bearer ${keycloak.token}`;
  }

  // Tell the API which locale to resolve lookup names in.
  const lang = i18n.resolvedLanguage ?? i18n.language;
  if (lang) config.headers['Accept-Language'] = lang;

  return config;
});

export default apiClient;
