import apiClient from './client';

export interface AiStatus {
  enabled: boolean;
  provider: string;          // e.g. "ollama/llama3.2" or "disabled"
}

export interface AlertSummary {
  alertId: string;
  available: boolean;
  summary: string | null;
  fromCache: boolean;
  provider: string;
  reason: string | null;     // why unavailable
}

export const getAiStatus = (): Promise<AiStatus> =>
  apiClient.get<AiStatus>('/ai/status').then(r => r.data);

/** The model can take several seconds on CPU — give it a generous timeout. */
export const getAlertSummary = (alertId: string, regenerate = false): Promise<AlertSummary> =>
  apiClient
    .get<AlertSummary>(`/ai/alerts/${alertId}/summary`, {
      params: regenerate ? { regenerate: true } : undefined,
      timeout: 120_000,
    })
    .then(r => r.data);

// ---- Ask EdgePulse (Sprint 30) ---------------------------------------------

export interface AskGrounding {
  devices: string[];         // "Feed Water Pump (PUMP-LW-001)"
  alerts: number;
  workOrders: number;
  scope: 'device' | 'mentioned-devices' | 'tenant';
}

export interface AskResult {
  available: boolean;
  answer: string | null;
  provider: string;
  reason: string | null;
  grounding: AskGrounding;
}

/** Natural-language question, answered from live data the caller may see. */
export const askQuestion = (question: string, deviceId?: string): Promise<AskResult> =>
  apiClient
    .post<AskResult>('/ai/ask', { question, deviceId }, { timeout: 120_000 })
    .then(r => r.data);
