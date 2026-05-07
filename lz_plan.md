# Liva — Dashboard & AI Advisor Module Plan

**Module:** Dashboard & AI Advisor  
**Scope:** Aggregated summary view, AI-generated financial tips  
**Stack:** React + TypeScript (frontend), ASP.NET Core C# (backend)

---

## What this module does

The dashboard has no database collections of its own. It pulls data from all other modules at request time and presents a unified financial overview. The AI advisor takes that same summary and asks the selected AI provider for personalised tips.

Backend endpoints:
- `GET /api/dashboard` — aggregated summary (net worth, income/expenses, budget usage, active goals)
- `POST /api/advisor/analyse` — sends financial context to AI, returns tips; body: `{ "provider": "ollama" | "claude", "goals": ["save_more", ...] }`
- `POST /api/dev/seed` — dev-only; clears and re-seeds realistic sample data for the authenticated user

---

## Progress

### Backend

- [x] `DashboardController.cs` — aggregates net worth, monthly income/expenses, budget usage per category, active goals with progress
- [x] `AdvisorController.cs` — gathers financial context and routes to selected AI provider with user goal context
- [x] `IAiAdvisor` interface + `ClaudeAdvisor` / `OllamaAdvisor` implementations — registered as keyed singletons in DI
- [x] `SeedController.cs` — `POST /api/dev/seed` populates all 6 collections with 3 months of realistic data for the logged-in user
- [x] `DashboardSummary.cs` — response models: `DashboardSummary`, `BudgetUsage`, `GoalProgress` (computed `UsagePercent`, `PercentReached`, `Remaining`)
- [x] `FirebaseAuthMiddleware.cs` — dev bypass: if Firebase Admin SDK not initialised, assigns fixed `dev-user` (no Firebase project needed in dev)
- [ ] Add portfolio data (total invested, net worth breakdown) to the advisor prompt — currently only transactions, budgets, and goals are sent
- [ ] Return `ProjectedCompletion` date on `GoalProgress` — field exists in model, not yet populated by `DashboardController`
- [ ] `ClaudeAdvisor` error handling — `EnsureSuccessStatusCode()` throws on API errors; wrap in try/catch and return a user-friendly message instead

### Frontend

- [x] Overview cards — net worth, total invested, total saved, monthly net (income − expenses) with colour coding
- [x] Budget usage section — one progress bar per category, spent / limit, % used, red at ≥ 90%, yellow at ≥ 70%
- [x] Active savings goals section — progress bar per goal, current / target, % reached
- [x] AI Advisor section — goal selection toggle buttons (save more, reduce expenses, invest, emergency fund, pay debt, budget better), Ollama and Claude buttons, loading spinner, displays tips with provider label
- [x] `firebase.ts` — `firebaseConfigured` flag; skips auth flow when env vars missing
- [x] `api/client.ts` — omits `Authorization` header when Firebase not configured
- [ ] AI advisor tip display — response is a raw string from the AI; render as a formatted list (split on numbered items or newlines)
- [ ] Currency from Firebase token claim — monetary values hardcoded to EUR; should read `currency` claim from the Firebase token set by Roberts' auth module
- [ ] Empty / zero states — show a helpful prompt when user has no transactions, no budgets set, or no active goals

---

## Integration points (depends on other modules)

| Data needed | Source collection | Owner |
|---|---|---|
| Net worth (assets / liabilities) | `assets`, `liabilities` | Janis |
| Monthly income / expenses | `transactions` | Laura |
| Budget limits + category spend | `budgets`, `transactions` | Laura |
| Active goals + current amounts | `savings_goals` | Feodors |
| Preferred currency claim | Firebase token | Roberts |

All data is read-only from this module's perspective — no writes to other collections.

---

## Testing Claude

1. Get an Anthropic API key from [console.anthropic.com](https://console.anthropic.com)
2. Add it to `backend/BudgetApp.Api/appsettings.json` under `"Anthropic": { "ApiKey": "sk-ant-..." }`
3. Make sure the backend is running and seeded (`POST /api/dev/seed` via Swagger or curl)
4. Open the Dashboard, pick goals, click **Claude** — tips should appear

---

## Notes

- Tips are never persisted — generated fresh on every `POST /api/advisor/analyse` call.
- Budget usage is computed on demand from transaction data, not stored.
- AI providers: Claude (key in `Anthropic:ApiKey`) and Ollama (local, `Ollama:BaseUrl` + `Ollama:Model` in config, defaults to `llama3.2` at `http://ollama:11434`).
- Ollama model is pulled automatically on container start via `.devcontainer/pull-ollama-model.sh` — called by `postStartCommand` in `devcontainer.json`, runs in background, idempotent.
- Backend and frontend also auto-start via `postStartCommand` on every container start. Logs at `/tmp/backend.log` and `/tmp/frontend.log`.
- Monetary values from the API come as `decimal` (Decimal128 in Mongo) — keep as numbers in the frontend, only format for display.
