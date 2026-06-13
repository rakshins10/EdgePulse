import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Provider as ReduxProvider } from 'react-redux';
import { store } from './store/index';
import keycloak from './keycloak';
import { ThemeProvider } from './context/ThemeContext';
import i18n from './i18n';
import { loadUiOverrides } from './i18n/translationTools';
import App from './App';
import './index.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 15_000,
    },
  },
});

// When the UI language changes: (1) layer DB-backed UI string overrides on top
// of bundled JSON for that locale, (2) drop cached query data so server-resolved
// names (device types, statuses, …) refetch in the newly-selected locale.
let lastLoadedOverrides = '';
i18n.on('languageChanged', (lng: string) => {
  if (lng && lng !== lastLoadedOverrides && lng !== 'en') {
    lastLoadedOverrides = lng;
    void loadUiOverrides(lng);
  }
  void queryClient.invalidateQueries();
});

async function bootstrap() {
  try {
    const authenticated = await keycloak.init({
      onLoad: 'login-required',
      checkLoginIframe: false,
    });

    if (!authenticated) {
      keycloak.login();
      return;
    }
  } catch {
    console.error('Keycloak initialization failed');
  }

  // Load DB UI-string overrides for the initial (persisted) language, if not English.
  const initialLng = i18n.resolvedLanguage ?? i18n.language;
  if (initialLng && initialLng !== 'en') {
    lastLoadedOverrides = initialLng;
    void loadUiOverrides(initialLng);
  }

  const root = document.getElementById('root');
  if (!root) throw new Error('Root element not found');

  createRoot(root).render(
    <StrictMode>
      <ThemeProvider>
        <ReduxProvider store={store}>
          <QueryClientProvider client={queryClient}>
            <App />
          </QueryClientProvider>
        </ReduxProvider>
      </ThemeProvider>
    </StrictMode>
  );
}

void bootstrap();
