# Sprint 29 — AI Alert Explanations (#9, #39)

**Date:** August 2026
**Goal:** First slice of the AI epics — every alert can be explained in plain
English by a local LLM (Ollama, on-premise) or Azure OpenAI, on demand and
cached, with zero impact on the telemetry hot path. Shipped with a
beginner-level guide (`docs/guides/05-ai-guide.md`) explaining every concept.

---

## What was delivered

### Infrastructure
- `ollama` service in `docker-compose.onpremise.yml` (ollama/ollama:0.5.7,
  port 11434, `mem_limit: 4g`, named volume `edgepulse_ollama_models`) plus a
  one-shot `ollama-pull` that fetches `llama3.2` (3B, ~2 GB) once.

### Backend
- `IAiAssistant` (Application) — `IsEnabled`, `Description`,
  `CompleteAsync(systemPrompt, userPrompt)`; returns `null` on any failure.
- Providers (Infrastructure/Services/Ai): `OllamaAiAssistant` (`/api/chat`,
  temperature 0.2, num_predict 300), `AzureOpenAiAssistant`, `NullAiAssistant`;
  selected by `Ai:Provider` = `ollama | azureopenai | none`.
- `AlertSummaryPrompts` — system prompt enforcing the three headings
  WHAT HAPPENED / LIKELY CAUSES / RECOMMENDED ACTION, hedged language,
  <120 words; user prompt with labelled alert facts + recent readings trend.
- `GetAlertSummaryQuery` — tenant-scoped; cache hit on `Alert.AiSummary` →
  disabled check → device/type lookup → prompt → model → persist. Never
  throws: unavailable results carry a human-readable `reason`.
- `AiController`: `GET /api/ai/status`, `GET /api/ai/alerts/{id}/summary?regenerate`.
- `Ai` config section (Provider, TimeoutSeconds, Ollama, AzureOpenAi with
  `<SET-VIA-USER-SECRETS-OR-ENV>` key placeholder).

### Dashboard
- `✦ Explain` button on every alert row (only when `/api/ai/status` is
  enabled) → `AiSummaryPanel`: parsed three-section view, plain-text
  fallback, Retry / Regenerate, "cached" note, disclaimer. en/fi/sv strings.

### Docs
- New **AI Guide** (`docs/guides/05-ai-guide.md`): LLM concepts, Ollama,
  prompts, design decisions, code map, running/tuning/troubleshooting, limits.
- README, setup/config/functionality/technical guides, API reference,
  operations, deployment, integrations, DOCKER-COMMANDS updated in parallel.

## Verified end-to-end (live)
1. ✅ `/api/ai/status` → `{ enabled: true, provider: "ollama/llama3.2" }`
2. ✅ First summary (Feed Water Pump vibration 11.4 > 8 mm/s HIGH) in 39 s,
   correct three-section output
3. ✅ Second call 67 ms, `fromCache: true`; `Alert.AiSummary` persisted
4. ✅ Ollama stopped → uncached alert HTTP 200 `available:false` + reason;
   cached alert still served; alerts page unaffected
5. ✅ 137 unit tests green (7 new, all with a fake `IAiAssistant` — no model
   needed in CI)

## Design decisions
- **On demand, not at alert time** — the Telemetry Processor never calls the
  model; a multi-second LLM call has no place in the ingestion path.
- **Cached on the alert** — stable wording for the audit trail, ~60 ms repeat
  reads; explicit Regenerate when wanted.
- **Graceful degradation** — `null` from the provider becomes
  `available:false`, never a 5xx.
- **Provider abstraction** — on-prem Ollama (data never leaves the network)
  vs Azure OpenAI by config; logic unit-testable without a model.

## Lessons / gotchas
- A separate Docker project holding 8080/3000 masquerades as a Keycloak
  failure — check `docker ps` across projects first.
- `--force-recreate` of Keycloak alone can leave it without a network
  (JDBC crash loop); recreate `postgres keycloak` together.
- The 3B model is the smallest that reliably follows the heading format;
  1B drifts. The panel's plain-text fallback covers residual drift.

## Next (Sprint 30)
- Natural-language device Q&A (`/api/ai/ask`) grounded in live device/alert
  data, with an Ask page; then cut **v1.1.0**.
