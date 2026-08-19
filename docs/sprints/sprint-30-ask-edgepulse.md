# Sprint 30 — Ask EdgePulse: natural-language Q&A (#9, #39)

**Date:** August 2026
**Goal:** Second and final slice of the AI epics for v1.1.0 — let any user ask
plain-language questions about the plant and get answers grounded in the live
device / alert / work-order data they are allowed to see. Uses the Sprint 29
provider abstraction (Ollama on-prem or Azure OpenAI). Explained for
beginners in `docs/guides/05-ai-guide.md` Part 2B.

---

## What was delivered

### Backend
- `AskPrompts` — system prompt ("answer ONLY from DATA, say what is missing,
  cite device name+code, <150 words, hedge") and user prompt
  (`Current time` + `DATA:` block + `QUESTION:`).
- `AskQuestionQuery` handler — retrieval-augmented generation (RAG):
  1. device catalogue with tenant + role scoping (MillManager → mill,
     Operator → areas);
  2. focus: explicit `deviceId` → device codes/names mentioned in the
     question (deterministic, max 3) → plant-wide snapshot;
  3. compact DATA block: per device type/status/mill/area/last seen/installed,
     alerts (last 30 d + any still open) with severity breakdown + latest 5,
     open work orders; snapshot: open alerts by severity + latest 8, top-3
     devices by 7-day alert count, open work orders;
  4. model call; `null` → `available:false` + reason. Nothing stored.
  5. response carries `grounding { devices, alerts, workOrders, scope }`.
- `POST /api/ai/ask` on `AiController` (any authenticated role; 400 on empty
  / >500-char question; 404 for a device outside the caller's scope).

### Dashboard
- **✦ Ask EdgePulse** sidebar page (`/ask`): chat-style thread, example
  prompts, Enter-to-send, "Grounded on: …" line per answer, disclaimer with
  provider name, disabled message when `Ai:Provider=none`.
- **✦ Ask about this device** on the device detail page →
  `/ask?deviceId=…&deviceLabel=…` with a clearable "Focused on" chip.
- en/fi/sv strings (`ask.*`, `nav.ask`).

### Tests
- 12 unit tests (`AskQuestionTests`) asserting **what goes into the prompt**:
  device grounding, snapshot content, code/name matching (cap 3, prefers
  codes), operator area scoping, cross-tenant 404, validation, disabled and
  model-null paths. Suite: **149** (30 domain + 119 application).
- Playwright `e2e/sprint30-ask.spec.ts` (3 tests): sidebar → Ask page,
  grounded answer renders, device focus link.

## Verified end-to-end (live, Ollama llama3.2)
1. ✅ "Which devices have open alerts right now?" → 13 open alerts listed with
   real device names/codes; grounding `tenant · 13 alerts · 1 work order`
   (65 s cold incl. model load)
2. ✅ "What is going on with PUMP-LW-001 and is anyone working on it?" →
   grounded on that device only; cites the open work order WO-… unassigned
   (17 s)
3. ✅ "What was the oil pressure on MOTOR-LW-002 yesterday afternoon?" →
   "I have no readings for MOTOR-LW-002 in the data provided." (6 s)
4. ✅ Playwright 3/3; `tsc` + `eslint` clean; 149 unit tests green

## Design decisions
- **RAG, not tool-calling** — 3B local models invoke functions unreliably;
  retrieve-then-generate is deterministic, secure (we decide what is
  retrievable) and unit-testable.
- **Scoping in the retrieval** — the model can only see what the caller may
  see; there is no second enforcement point to get wrong.
- **Literal device matching** — predictable over clever; the per-device
  button covers the "which pump did you mean" case.
- **No caching, no storage** — questions vary; nothing leaves a trace except
  the normal request log.

## Gaps / follow-ups
- Telemetry readings, attachments and health scores are not in the DATA
  block yet (extension is a few lines in `BuildDeviceDataAsync`).
- No conversation memory (each question is independent).
- Streaming answers would improve perceived latency on CPU.

## Next
- Cut **v1.1.0** (changelogs, version bumps, tags), close #9 / #39.
