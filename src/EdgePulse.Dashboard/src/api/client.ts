import axios from 'axios';
import keycloak from '../keycloak';

const apiClient = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

// Attach Keycloak bearer token to every request
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
  return config;
});

export default apiClient;
