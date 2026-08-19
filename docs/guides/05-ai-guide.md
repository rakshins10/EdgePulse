# EdgePulse — AI Features: A Beginner's Guide

> Written for someone who has never worked with AI/LLMs before. It explains
> every concept the first time it appears, using EdgePulse's own AI features
> as the worked example — alert explanations (Part 2) and Ask EdgePulse (Part 2B). Read it top to bottom once; afterwards the section
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

## Part 2B — "Ask EdgePulse": questions answered from live data (Sprint 30)

### 2B.1 The feature

A new sidebar page, **✦ Ask EdgePulse**, where you type a question in plain
English and get an answer that is based on *your* current devices, alerts
and work orders:

> **Q:** Which devices have open alerts right now?
> **A:** Open alerts: 13 — Feed Water Pump (PUMP-LW-001): vibration,
> bearing_temp · Feed Water Pump (PUMP-RV-001): vibration · Batch Digester
> (DGST-RV-001): temperature · PM1 Drive Motor (MOTOR-LW-002): vibration …

> **Q:** What was the oil pressure on MOTOR-LW-002 yesterday afternoon?
> **A:** I have no readings for MOTOR-LW-002 in the data provided.

Under every answer the page shows **"Grounded on: …"** — which devices, how
many alerts and work orders the assistant was actually looking at. The
device detail page has an **✦ Ask about this device** button that opens the
page focused on that device.

### 2B.2 The big idea: RAG ("retrieve, then generate")

Part 1 said the model is *autocomplete, not a database*. So how can it
answer questions about your plant? By **us** doing the database part:

```
  1. RETRIEVE   EdgePulse queries SQL for the devices/alerts/work orders the
                user may see, and renders them as a short plain-text block.
  2. AUGMENT    That block is pasted into the prompt, under a "DATA:" heading,
                followed by "QUESTION:" and the user's question.
  3. GENERATE   The model writes an answer — and is instructed to use ONLY the
                DATA block and to say when something is not in it.
```

This is called **Retrieval-Augmented Generation (RAG)**. It's the standard
way to make a general model answer questions about private, changing data
without training it on that data. Three things follow:

- **Accuracy comes from the retrieval, not the model.** If the DATA block
  is right, a small model does well. The 3B llama3.2 answered the examples
  above correctly.
- **Nothing is learned or stored.** Every question re-reads the database;
  the model has no memory between questions.
- **Security is enforced by us.** The DATA block is built with the same
  role scoping as the Alerts/Devices APIs — an Operator's question can
  only ever be answered from their own areas, because that's all we
  retrieve. The model never has a way to "look further".

> **Why not let the model query the database itself ("tool use" / "function
> calling")?** Big cloud models can do this; 3B local models do it
> unreliably — they invent function arguments or skip the call. RAG is
> simpler, deterministic and, crucially, **unit-testable**: we can assert
> exactly what data went into the prompt.

### 2B.3 The flow (what the code does)

```
POST /api/ai/ask { question, deviceId? }
  │
  ├─ 1. Validate (non-empty, ≤ 500 chars)
  ├─ 2. Device catalogue the caller may see   (tenant + role scoping)
  ├─ 3. Focus:  deviceId given?            → that device           scope=device
  │             device code/name in text?  → up to 3 matches       scope=mentioned-devices
  │             otherwise                  → plant-wide snapshot   scope=tenant
  ├─ 4. Build DATA block
  │       per device:  type, status, mill, area, last seen, installed;
  │                    alerts (last 30 d + any still open) with severity
  │                    breakdown + latest 5; open work orders
  │       snapshot:    open alerts by severity + latest 8; top-3 devices by
  │                    alerts in 7 d; open work orders
  ├─ 5. AI disabled? → available=false (no model call)
  ├─ 6. model.CompleteAsync(system, DATA + QUESTION)
  └─ 7. → { available, answer, provider, reason, grounding }
```

Step 3's device matching is deliberately *literal* (code or full name,
case-insensitive). It is cheap, predictable and testable; the trade-off is
that "the feed pump" won't match "Feed Water Pump" — use the code, or the
per-device button.

### 2B.4 The Ask prompts

**System** (`AskPrompts.System`):
```
You are EdgePulse Assistant, a plant-monitoring helper for operators and
maintenance staff at an industrial site.

Rules:
- Answer ONLY from the DATA section of the message. It is the live,
  authoritative state of the plant. Never invent devices, numbers, dates or causes.
- If the DATA does not contain what is needed, say exactly what is missing
  (for example: "I have no readings for PUMP-LW-001 in the data provided").
- Refer to devices by name and code (e.g. Feed Water Pump (PUMP-LW-001)).
- Be concise: plain English, short sentences or a short bullet list, under 150 words.
- When asked for advice, hedge ("likely", "consider") — you are not a diagnosis.
- No preamble, no sign-off.
```

**User** (built by `AskPrompts.ForQuestion`) — a real one, trimmed:
```
Current time (UTC): 2026-08-19 19:05

DATA:
DEVICE: Feed Water Pump (PUMP-LW-001)
  type: Centrifugal Pump; status: Active; mill: Lakewood Mill; area: Fiberline
  last seen: 2026-08-19 18:59 UTC; installed: 2021-03-10
  alerts (last 30 days + any still open): 2 total, 2 open (1 HIGH, 1 MEDIUM)
    - 2026-08-19 17:44 HIGH vibration 11.4mm/s (threshold 8mm/s), status OPEN
    - 2026-08-18 09:12 MEDIUM bearing_temp 78C (threshold 75C), status ACKNOWLEDGED
  open work orders: 1
    - WO-3CC25E83 "Investigate vibration alert on Feed Water Pump (PUMP-LW-001)" OPEN priority HIGH, unassigned

QUESTION:
What is going on with PUMP-LW-001 and is anyone working on it?
```

The "Current time" line lets the model answer "today"/"this week" questions;
labelled fields and the explicit *status* per alert stop it confusing open
with resolved.

### 2B.5 Code map (additions)

| Layer | File | Role |
|---|---|---|
| Application | `Features/Ai/AskPrompts.cs` | System + user prompt (with the RAG explanation in comments) |
| Application | `Features/Ai/AskQuestionQuery.cs` | Scoping, device matching, DATA block, model call, grounding |
| API | `Controllers/AiController.cs` | `POST /api/ai/ask` |
| Dashboard | `pages/ask/AskPage.tsx` (+ `.module.css`) | Chat-style page, examples, grounding line, device focus chip |
| Dashboard | `pages/devices/DeviceDetailPage.tsx` | "✦ Ask about this device" |
| Dashboard | `api/ai.ts` | `askQuestion()` |
| Tests | `tests/…/Features/Ai/AskQuestionTests.cs` | 12 tests: what goes INTO the prompt, scoping, matching, validation, unavailable paths |
| E2E | `src/EdgePulse.Dashboard/e2e/sprint30-ask.spec.ts` | Sidebar → Ask → grounded answer; device focus |

### 2B.6 Extending it

Want the assistant to know about something new (e.g. last 10 telemetry
readings, attachments, health score)? You don't touch the model or the
prompt rules — you add a few lines to `BuildDeviceDataAsync` that query the
data and append it to the DATA block, plus a unit test asserting it appears
in the prompt. That is the whole extension model of RAG.

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

# 3. Ask EdgePulse (Sprint 30)
curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \n  -d '{"question":"Which devices have open alerts right now?"}' http://localhost:5104/api/ai/ask
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
- **No conversation memory.** Each alert summary and each Ask question is
  independent; the server re-grounds every question from scratch. Follow-up
  questions must restate the subject ("and PUMP-LW-001?" will not work).
- **Ask only sees what we put in the DATA block.** Raw telemetry readings,
  attachments and audit history are not included (yet) — the assistant will
  say so. Adding a data source means adding a few lines to
  `AskQuestionQuery.cs`, not retraining anything.
- **Device matching is literal.** "the feed pump" won't match "Feed Water
  Pump"; use the code or the full name, or open the device page and click
  *Ask about this device*.
