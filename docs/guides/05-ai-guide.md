# EdgePulse — AI Features: A Beginner's Guide

> Written for someone who has never worked with AI/LLMs before. It explains
> every concept the first time it appears, using EdgePulse's own AI features
> as the worked example. Read it top to bottom once; afterwards the section
> headings work as a reference.

---

## Part 1 — The concepts (no code)

### 1.1 What is an LLM?

A **Large Language Model** is a program that has been trained on an enormous
amount of text and learned to *predict what text comes next*. That sounds
trivial, but at scale it produces something that can summarise, explain,
answer questions and follow instructions written in plain English.

Key mental model: **it is a very good autocomplete, not a database.** It does
not "look things up"; it generates the most plausible continuation of the
text you give it. Three consequences matter for us:

| Consequence | What it means for EdgePulse |
|---|---|
| It only knows what you tell it (plus general training) | We must put the alert facts *into the prompt*; it cannot see our database |
| It can sound confident while being wrong ("hallucinate") | We ask it to hedge ("likely", "possible") and we show a disclaimer |
| Output varies run to run | We cache the first answer so the text is stable |

### 1.2 Model sizes — why "3B" matters

Models are measured in **parameters** (roughly, learned weights). More
parameters → smarter, but needs far more memory and compute:

- `llama3.2:1b` — 1 billion, ~1.3 GB RAM. Fast, but follows instructions poorly.
- **`llama3.2` (3B)** — 3 billion, ~3 GB RAM. Good instruction-following. **EdgePulse default.**
- GPT-4-class — hundreds of billions. Cloud-only; you rent it by the token.

We chose 3B because it's the smallest size that *reliably* follows the
"three headings" format we need. That's not a guess — the 1B model drifts.

### 1.3 What is Ollama?

**Ollama** is an open-source program that downloads open models (Llama,
Mistral, …) and serves them over a tiny HTTP API on *your own machine*. No
account, no API key, no internet needed after the download.

EdgePulse runs it as a Docker container (`edgepulse-ollama`) right next to
SQL Server. The API talks to it at `http://localhost:11434`. **Alert text
never leaves the mill network** — which is exactly the data-sovereignty
promise on-premise customers care about.

### 1.4 Prompts — how you "program" a model

You steer a model with plain text in two parts:

1. **System prompt** — persistent rules: who the model is, what format to
   answer in, what it must never do. Think *job description*.
2. **User prompt** — the actual request plus the data it needs.

The model writes a continuation that fits both. What makes prompts good:

- **Be specific about FORMAT** → predictable, parseable output
- **Give the FACTS** → it can't invent what you supplied
- **Say what to do when unsure** → it hedges instead of guessing
- **Keep it short** (especially for small models) → they follow short,
  concrete instructions far better than long ones

You'll see both EdgePulse prompts in full in Part 2.

### 1.5 Temperature

A number (0–1) controlling randomness. **0.2** (what we use) = consistent,
factual, "boring". 0.9 = creative, varied. For maintenance text we want the
same facts to give the same answer every time → low temperature.

### 1.6 Tokens and why the first call is slow

Models read and write in **tokens** (~¾ of a word). Generation is
token-by-token, so a 120-word answer is ~160 generation steps. On a CPU
that's a few seconds. The **first** call is slower (~40 s on a laptop)
because Ollama has to load the 2 GB model into RAM; afterwards it stays
loaded and calls take 5–15 s. This is why the feature is **on demand and
cached**, never in a hot path.

---

## Part 2 — What EdgePulse built and how it works

### 2.1 Feature: "✦ Explain" on every alert

On the Alerts page, each row has an **✦ Explain** button. Click it and a
panel opens with three sections written by the model:

```
WHAT HAPPENED:       one sentence of fact
LIKELY CAUSES:       2–3 bullets, most probable first
RECOMMENDED ACTION:  2–3 bullets a technician should do now
```

Real output from the demo (vibration alert on a feed-water pump):

> **What happened:** The Feed Water Pump (PUMP-LW-001) exceeded its vibration
> alert threshold of 8 mm/s with a measured value of 11.4 mm/s.
> **Likely causes:** possible bearing wear or misalignment · loose or
> damaged mounting hardware · overloaded pump
> **Recommended action:** check alignment and tighten loose bolts · inspect
> bearings for wear · review operating parameters for overload

### 2.2 The request, end to end

```
Browser            API                 Application                 Infrastructure      Ollama
  |  GET /ai/alerts/{id}/summary
  +------------------>  AiController
                        +--MediatR--> GetAlertSummaryQueryHandler
                                       1. Alert.AiSummary set?  --> return (cache hit, ~60 ms)
                                       2. IAiAssistant.IsEnabled false? --> available=false
                                       3. load device name/type, parse recent readings
                                       4. build prompts (AlertSummaryPrompts)
                                       5. IAiAssistant.CompleteAsync --> OllamaAiAssistant --> POST /api/chat
                                                                     <-- message.content <--
                                       6. null? --> available=false + reason
                                       7. alert.SetAiSummary(text); SaveChanges (cached)
  <---- JSON { available, summary, fromCache, provider, reason }
```

### 2.3 The three design decisions (and why)

**1. On demand, not at alert time.**
The alert engine (Telemetry Processor) evaluates readings every few
seconds. A model call takes seconds. Putting the LLM in that path would
delay or drop telemetry. So summaries are generated the *first time a
human asks*, in the API. The engine never knows AI exists.

**2. Cached on the alert.**
`Alert.AiSummary` (a column that existed since Sprint 8) stores the text
after the first generation. Second request: ~60 ms, zero model work, and the
wording is stable for the audit trail. A **Regenerate** button bypasses the
cache when you want a fresh take.

**3. Never fails the caller.**
`IAiAssistant.CompleteAsync` returns `null` on any problem; the handler turns
that into `available: false` + a human-readable `reason`. Ollama stopped?
Model still loading? Provider set to `none`? The alert page works exactly as
before — the panel just says so. Verified live: stopped the Ollama
container, asked for a new alert → HTTP 200, `available=false`; cached
alerts still served.

### 2.4 The code map

| Layer | File | Role |
|---|---|---|
| Application | `Common/Interfaces/IAiAssistant.cs` | The contract: `IsEnabled`, `Description`, `CompleteAsync(system, user)` |
| Application | `Features/Ai/AlertSummaryPrompts.cs` | **Both prompts, fully commented** — start here to tune behaviour |
| Application | `Features/Ai/GetAlertSummaryQuery.cs` | The handler: cache → enabled? → facts → model → cache |
| Infrastructure | `Services/Ai/OllamaAiAssistant.cs` | Talks to Ollama `/api/chat` (stream=false, temp 0.2, num_predict 300) |
| Infrastructure | `Services/Ai/AzureOpenAiAssistant.cs` | Same job against Azure OpenAI (cloud profile) |
| Infrastructure | `Services/Ai/NullAiAssistant.cs` | Used when `Ai:Provider = none` |
| Infrastructure | `Services/Ai/AiOptions.cs` | Binds the `Ai` config section |
| Infrastructure | `DependencyInjection.cs` | Picks the provider from `Ai:Provider` |
| API | `Controllers/AiController.cs` | `GET /api/ai/status`, `GET /api/ai/alerts/{id}/summary?regenerate` |
| Dashboard | `api/ai.ts`, `components/alerts/AiSummaryPanel.tsx` | Client + panel (parses headings, plain-text fallback) |
| Dashboard | `pages/alerts/AlertsPage.tsx` | ✦ Explain button (only when `/ai/status` says enabled) |
| Compose | `infrastructure/docker-compose.onpremise.yml` | `ollama` + `ollama-pull` services |
| Tests | `tests/…/Features/Ai/AlertSummaryTests.cs` | 7 tests with a **fake** IAiAssistant — no model needed |

### 2.5 The actual prompts

**System prompt** (`AlertSummaryPrompts.System`):
```
You are a maintenance assistant for an industrial plant. You explain
equipment alerts to shift operators and maintenance technicians.

Rules:
- Write in plain English, 3 short sections with these exact headings:
  WHAT HAPPENED: one sentence stating the fact.
  LIKELY CAUSES: 2-3 bullet points, most probable first.
  RECOMMENDED ACTION: 2-3 bullet points the technician should do now.
- Use only the data you are given. If you are not sure, say
  "likely" or "possible" — never state a cause as certain.
- No preamble, no sign-off, no markdown other than the bullets.
- Keep the whole answer under 120 words.
```

**User prompt** (built per alert by `AlertSummaryPrompts.ForAlert`):
```
Explain this alert.

Device: Feed Water Pump (PUMP-LW-001), type: Centrifugal Pump
Metric: vibration
Measured value: 11.4 mm/s
Alert threshold: 8 mm/s
Severity: HIGH
Triggered at: 2026-07-24 19:47 UTC
Recent readings (oldest to newest): 7.9 mm/s, 9.6 mm/s, 11.4 mm/s
```

Notice: labelled fields (the model can't confuse value with threshold), the
device *type* (so "pump" advice, not generic), and the recent readings (so it
can say "rising" vs "sudden spike").

### 2.6 Why the abstraction matters (and lets us unit-test AI)

`IAiAssistant` has one method. The handler never knows whether Ollama,
Azure or nothing is behind it. Two wins:

- **Swap providers by config**, no code change — a mill with no internet uses
  Ollama; an Azure tenant flips `Ai:Provider` to `azureopenai`.
- **Unit-test all the logic without a model.** The 7 tests use an NSubstitute
  fake that returns a canned string, `null`, or `IsEnabled=false` — and
  verify caching, regenerate, the disabled path, the failure path, the
  prompt contents and tenant isolation. Deterministic, milliseconds, in CI.

---

## Part 3 — Running, configuring, extending

### 3.1 Turn it on / off

`src/backend/EdgePulse.API/appsettings.json` → `Ai` section:

```json
"Ai": {
  "Provider": "ollama",
  "TimeoutSeconds": 90,
  "Ollama":  { "BaseUrl": "http://localhost:11434", "Model": "llama3.2" },
  "AzureOpenAi": { "Endpoint": "", "Deployment": "gpt-4o-mini",
                   "ApiKey": "<SET-VIA-USER-SECRETS-OR-ENV>", "ApiVersion": "2024-10-21" }
}
```

- `"Provider": "none"` → AI disabled; the ✦ Explain button disappears; nothing else changes.
- `"ollama"` → default. Needs the `ollama` container running (next section).
- `"azureopenai"` → set `Endpoint` and put the key in user-secrets:
  `dotnet user-secrets set "Ai:AzureOpenAi:ApiKey" "…" --project src/backend/EdgePulse.API`
- In Docker/prod use env vars: `Ai__Provider`, `Ai__Ollama__BaseUrl`
  (`http://ollama:11434` in-network), `Ai__AzureOpenAi__ApiKey`.

### 3.2 Start Ollama and fetch the model

```bash
docker compose -f infrastructure/docker-compose.onpremise.yml up -d ollama ollama-pull
```
First time: ~1 GB Ollama image + **~2 GB llama3.2 download** (once; kept in
the `edgepulse_ollama_models` volume). Watch progress:
`docker logs -f edgepulse-ollama-pull`. Ready when:
```bash
curl http://localhost:11434/api/tags        # → lists llama3.2
```
**RAM:** Ollama is capped at 4 GB (`mem_limit`). Docker Desktop's VM needs
≥ 6–8 GB total for the full stack + model; stop other Docker projects if
answers time out.

### 3.3 Test it by hand (no UI)

```bash
# 1. talk to Ollama directly — proves the model works
curl http://localhost:11434/api/chat -d '{"model":"llama3.2","stream":false,"messages":[{"role":"user","content":"Say hello in five words"}]}'

# 2. through EdgePulse (needs a JWT — see Setup guide)
curl -H "Authorization: Bearer $TOKEN" http://localhost:5104/api/ai/status
curl -H "Authorization: Bearer $TOKEN" http://localhost:5104/api/ai/alerts/<alertId>/summary
curl -H "Authorization: Bearer $TOKEN" "http://localhost:5104/api/ai/alerts/<alertId>/summary?regenerate=true"
```

### 3.4 Tuning the answers

Everything the model sees is in **`AlertSummaryPrompts.cs`**. Typical tweaks:
- Want Finnish output? Add "Answer in Finnish." to the system prompt (or make
  it follow `Accept-Language` — a good next step).
- Too long? Lower "under 120 words" and `NumPredict` in `OllamaAiAssistant`.
- Too vague? Add the device's maintenance history to the user prompt.

Change the prompt → existing cached summaries keep the old text; users click
**Regenerate** for the new style.

### 3.5 Try a different model

`ollama pull mistral` (or `qwen2.5:7b`, `llama3.1:8b` if you have the RAM),
then set `Ai:Ollama:Model`. Bigger models = better reasoning, slower, more RAM.

### 3.6 Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Panel says "did not return a summary" | First call timed out while the model loaded, or Ollama down | Click Retry; check `docker ps` for `edgepulse-ollama`; raise `Ai:TimeoutSeconds` |
| No ✦ Explain button | `Ai:Provider` is `none`, or `/api/ai/status` unreachable | Check appsettings / restart API |
| `model 'llama3.2' not found` in API log | Pull didn't finish | `docker compose up -d ollama-pull`, wait for `curl :11434/api/tags` |
| Answers ignore the headings | Model too small / temperature too high | Keep 3B+, temperature ≤ 0.3; the panel falls back to plain text anyway |
| Very slow every call (not just first) | RAM pressure → model evicted each time | Free RAM; stop other containers; consider `llama3.2:1b` for demos |

---

## Part 4 — Honest limits

- **Not a diagnosis.** The model reasons from the alert numbers and general
  knowledge — it has never seen *your* pump. The UI says so. Treat output as
  a structured starting point for a technician.
- **Small model = occasional format drift.** The panel handles it (plain-text
  fallback), and regenerate usually fixes it.
- **CPU inference is slow.** A GPU (or a cloud provider) makes it
  near-instant; on a laptop expect 5–40 s.
- **No memory between alerts.** Each summary is independent. Cross-alert
  reasoning ("this pump has alerted 3 times this week") is what the next
  feature — natural-language device Q&A — adds.
