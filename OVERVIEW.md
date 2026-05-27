# Budget App — Project Overview

A full-stack personal finance web application built as a university group project. It lets users track their income and expenses, set monthly spending budgets, manage savings goals, monitor an investment/asset portfolio, and get AI-powered financial advice — all behind a secure login.

---

## What the App Does

### Dashboard
The landing page after login. Shows a summary of the user's financial health:
- **Net worth** — total assets minus total liabilities
- **Total saved** — sum of all savings goal balances
- **Monthly flow** — income minus expenses for the current month
- **Spending trend chart** — a bar chart of the last 6 months of expenses
- **AI Advisor** — generates personalised financial tips based on the user's real data using either Claude (cloud, requires API key) or Ollama (local, free)

### Transactions
- Log income and expense transactions, each with a category, date, amount, and optional description
- Filter transactions by date range, category, amount range, or keyword
- View per-category budget progress inline (how much of the monthly limit has been spent)

### Budgets
- Set a monthly spending limit for any expense category (e.g. Food, Transport)
- See remaining budget and usage percentage per category in real time

### Savings Goals
- Create goals with a name, target amount, and deadline
- Deposit money toward a goal or withdraw from it (withdrawals require a reason)
- See progress as a percentage and a projected completion date based on the last 30 days of deposits
- Pause, resume, or abandon goals; abandoned goals automatically withdraw the remaining balance and close the goal

### Portfolio
- Track assets (stocks, crypto, real estate, cash, etc.) and liabilities (loans, credit cards, etc.)
- Record price/value history entries per asset or liability
- View allocation breakdown (pie chart of asset types)
- Monthly performance chart — how net worth changed each month
- Full day-by-day net worth history chart for any date range up to 5 years

### Reports
- Monthly report for any past month combining: income, expenses (by category), savings contributions, and portfolio change
- Export as CSV or PDF

### Profile
- Change display name, email address, currency preference, or password from the sidebar profile modal

---

## Tech Stack

### Backend
| Technology | Role |
|---|---|
| **ASP.NET Core 8 (C#)** | REST API server |
| **MongoDB 7** | Database (document store, one collection per domain) |
| **Firebase Admin SDK** | Validates Firebase JWTs on every request |
| **FluentValidation** | Request body validation |
| **xUnit + Moq** | Unit and integration tests |

### Frontend
| Technology | Role |
|---|---|
| **React 19 + TypeScript** | UI framework |
| **Vite** | Dev server and build tool |
| **React Router** | Client-side page routing |
| **Recharts** | Charts (spending trend, net worth history, allocation) |
| **Zod** | Runtime API response validation |
| **Biome** | Linting and code formatting |
| **Firebase JS SDK** | User sign-in, sign-up, password reset in the browser |

### Infrastructure
| Technology | Role |
|---|---|
| **Docker & Docker Compose** | Runs the full stack (API + MongoDB + Ollama) in one command |
| **GitHub Actions** | CI pipeline — builds, lints, type-checks, and tests on every PR |
| **Dev Container** | VS Code one-click environment (auto-starts all services) |

---

## External Services

| Service | What it does | Where configured |
|---|---|---|
| **Firebase Authentication** | Handles user accounts, sign-in, sign-up, password reset, and issues JWT tokens | Firebase console; keys in `frontend/.env` and `backend/firebase-service-account.json` |
| **Anthropic Claude API** | Cloud AI advisor — generates financial tips from the user's transaction and goal data | API key entered by the user in the dashboard UI (or set server-side in `appsettings.json`) |
| **Ollama (llama3.2)** | Local AI advisor — same features as Claude but free; runs on the user's own machine | Auto-started by Docker Compose; model pulled on first run (~2 GB) |
| **MongoDB Atlas** *(optional)* | Managed cloud database alternative to the local Docker MongoDB | `appsettings.json` → `MongoDB:ConnectionString` |

---

## Team Responsibilities

### Roberts
**User authentication and account management**
- Firebase Auth middleware (`FirebaseAuthMiddleware.cs`) — validates JWTs and scopes every request to the correct user
- Auth controller and service (`AuthController.cs`, `FirebaseAuthService.cs`) — profile read, display name update, email change, password change
- `ApiControllerBase.cs` — base class that exposes the authenticated `UserId` to all other controllers

### Laura
**Transactions and budgets**
- Transactions controller and repository — create, read, update, delete transactions with filtering
- Budgets controller and service — monthly category budget limits, usage calculation (spending vs. limit per category)
- `FinancialCalculations.cs` — shared helpers for computing income, expenses, and monthly date ranges

### Feodors
**Savings goals**
- Goals and contributions controllers — full CRUD for goals and individual contribution records
- Savings services — business rules for deposits, withdrawals, balance calculation, goal status transitions (Active → Completed → Paused → Abandoned)
- `GoalProjectionCalculator.cs` — estimates completion date from the last 30 days of deposit activity
- `SavingsProgressService.cs` — aggregates live balance and projection for the goals list view

### Janis
**Portfolio**
- Assets and liabilities controllers and repositories — create, read, update, delete portfolio items with price/value history
- `PortfolioCalculator.cs` — resolves asset values at a given date from historical entries (used for net worth history, monthly performance, and allocation breakdown)
- Net worth controller — current snapshot and day-by-day history series
- Portfolio service — allocation grouping, monthly performance calculation

### Liva
**AI integration, project setup, frontend foundation**
- Dashboard controller and AI advisor services — `ClaudeAdvisor.cs` and `OllamaAdvisor.cs`; prompt construction from live financial data; provider selection logic
- Initial project scaffolding — solution structure, `Program.cs`, DI wiring, Docker Compose, GitHub Actions CI, Dev Container
- Frontend foundation — Vite config, routing, Firebase client setup, API fetch client (`client.ts`), Zod schemas, currency context and formatter, encryption utility

### All Team Members
- **MongoDB schema design** — collection structure, field naming, indexing decisions (decided and reviewed together)
- **Frontend feature pages** — each person built or contributed to the frontend page(s) matching their backend domain (transactions page, savings page, portfolio page, dashboard)
- **Code reviews** — all PRs reviewed by at least one other team member before merge

---

## How the Authentication Flow Works

1. The user signs in through the React app using the Firebase JS SDK (email + password or password reset)
2. Firebase issues a signed JWT (ID token) to the browser
3. Every API request includes this token in the `Authorization: Bearer <token>` header
4. `FirebaseAuthMiddleware` on the backend validates the token with the Firebase Admin SDK
5. The user's Firebase UID is extracted and stored on the request context
6. `ApiControllerBase.UserId` exposes this UID, and every database query is filtered by it — no user can ever read or write another user's data

**Development mode:** if `VITE_FIREBASE_API_KEY` is not set, the login screen is skipped and the backend assigns a fixed `dev-user` ID to all requests, making it easy to test the API locally without a Firebase project.

---

## MongoDB Collections

| Collection | Owner module | What it stores |
|---|---|---|
| `transactions` | Transactions | Every income and expense entry |
| `budgets` | Transactions | Monthly category spending limits |
| `savings_goals` | Savings | Goal definitions (name, target, status, current balance) |
| `goal_contributions` | Savings | Individual deposit/withdrawal records per goal |
| `assets` | Portfolio | Asset definitions with price history arrays |
| `liabilities` | Portfolio | Liability definitions with balance history arrays |

All collections are scoped by `userId` — every document stores the owner's Firebase UID and all queries include it as a filter.
