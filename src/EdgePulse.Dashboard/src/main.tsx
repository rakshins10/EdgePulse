import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Provider as ReduxProvider } from 'react-redux';
import { store } from './store/index';
import keycloak from './keycloak';
import { ThemeProvider } from './context/ThemeContext';
import i18n from './i18n';
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

// When the UI language changes, server-resolved names (device types, statuses,
// etc.) change too — drop cached query data so everything refetches in the
// newly-selected locale.
i18n.on('languageChanged', () => {
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
