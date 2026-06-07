# Budget App

A full-stack personal finance application for tracking transactions, managing budgets, monitoring savings goals, and analysing net worth with AI-powered financial insights.

## Features

- **Dashboard** — overview cards (net worth, invested, saved, monthly flow) with colour-coded metrics and AI financial advisor
- **Transactions** — income/expense tracking with categories, date filters, and budget progress
- **Savings Goals** — create goals, deposit/withdraw contributions, track progress per goal
- **Portfolio** — assets and liabilities with price history, allocation breakdown, monthly performance, and day-by-day net worth history chart
- **Reports** — monthly report combining income, expenses, savings contributions, and portfolio change, exportable as CSV or PDF
- **Profile** — change display name, email, currency, and password from the sidebar
- **AI Advisor** — personalised tips via Claude (cloud) or Ollama (local/free)
- **Collapsible sidebar** — icon-only mode to save screen space
- **Firebase Authentication** — sign up, log in, password reset

## Tech Stack

**Backend**

- ASP.NET Core 8 (C#)
- MongoDB 7
- Firebase Admin SDK (JWT authentication)
- Anthropic Claude API (AI advisor — cloud)
- Ollama (AI advisor — local, free)

**Frontend**

- React 19 + TypeScript
- Vite
- React Router
- Recharts (net worth history chart)
- Firebase Authentication (client SDK)
- Biome (linting & formatting)

**Infrastructure**

- Docker & Docker Compose
- GitHub Actions CI
- Dev Container support (VS Code)

## Running the App

### Option 1: `start.sh` — one command (recommended)

**Prerequisites:** [Docker Desktop](https://www.docker.com/products/docker-desktop/)

```bash
./start.sh
```

That's it. The script handles everything automatically:

- Checks Docker is running
- Copies `frontend/.env` → `.env` if the root file is missing
- Warns if `firebase-service-account.json` is absent
- Builds and starts all containers
- Pulls the `llama3.2` Ollama model on first run (~2 GB, skipped on subsequent runs)

App is available at **<http://localhost:3000>**.

**Stop everything:**

```bash
docker compose down
```

| Service  | URL                         |
| -------- | --------------------------- |
| Frontend | <http://localhost:3000>     |
| Backend  | <http://localhost:5000>     |
| MongoDB  | `mongodb://localhost:27017` |
| Ollama   | <http://localhost:11434>    |

---

### Option 2: Dev Container (VS Code)

One-click setup that auto-starts all services.

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/) and the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
2. Open the project in VS Code and click **"Reopen in Container"**
3. The container automatically installs dependencies, starts MongoDB and Ollama, runs the backend and frontend, and pulls the `llama3.2` model in the background

Frontend is available at **<http://localhost:5173>**.

View logs:

```bash
tail -f /tmp/backend.log
tail -f /tmp/frontend.log
tail -f /tmp/ollama-pull.log
```

---

### Option 3: Manual

**Prerequisites:** .NET 8 SDK, Node.js 20+, Docker (for MongoDB)

```bash
# Start MongoDB
docker run -d --name mongodb -p 27017:27017 mongo:7

# Install dependencies
cd backend && dotnet restore
cd frontend && npm install

# Start backend (http://localhost:5000)
cd backend/BudgetApp.Api && dotnet run

# Start frontend (http://localhost:5173)
cd frontend && npm run dev
```

Configure `frontend/.env` (copy from `frontend/.env.example`):

```env
VITE_API_URL=http://localhost:5000
VITE_FIREBASE_API_KEY=...
VITE_FIREBASE_AUTH_DOMAIN=...
VITE_FIREBASE_PROJECT_ID=...
VITE_FIREBASE_STORAGE_BUCKET=...
VITE_FIREBASE_MESSAGING_SENDER_ID=...
VITE_FIREBASE_APP_ID=...
```

---

## Environment Files

| File | Tracked | Purpose |
| ---- | ------- | ------- |
| `frontend/.env` | No | Firebase client keys + API URL for local dev |
| `frontend/.env.example` | Yes | Template — fill in and rename to `.env` |
| `.env` (root) | No | Same vars, used by `docker compose` at build time |
| `backend/appsettings.json` | Yes | MongoDB URL, Firebase project ID, Ollama config |
| `backend/firebase-service-account.json` | No | Firebase Admin SDK private key (backend only) |

**Development without Firebase:** if `VITE_FIREBASE_API_KEY` is not set, the login screen is skipped and the backend assigns a fixed `dev-user` ID to all requests — useful for local API testing.

---

## AI Advisor

| Provider | Cost | Requires |
|---|---|---|
| **Ollama** (default) | Free | Ollama running with `llama3.2` pulled |
| **Claude** | Paid | Anthropic API key entered in the UI |

The Claude API key is entered by the user in the dashboard — it is never stored server-side permanently. A server-level key can be set in `appsettings.json` under `Anthropic:ApiKey`.

---

## Architecture

### Backend

```
backend/BudgetApp.Api/
├── Program.cs                    # Entry point, DI setup
├── Configuration/                # Settings classes (MongoDB, etc.)
├── Middleware/
│   └── FirebaseAuthMiddleware.cs # Validates JWT, sets UserId on every request
├── Controllers/
│   └── ApiControllerBase.cs      # Exposes UserId to all controllers
└── Modules/                      # Feature modules (domain-driven)
    ├── Auth/                     # Profile read/update
    ├── Dashboard/                # Summary + AI advisor
    ├── Transactions/             # Transactions & budgets
    ├── Savings/                  # Goals & contributions
    ├── Portfolio/                # Assets, liabilities, net worth history
    ├── Reports/                  # Monthly report generation & export
    └── Dev/                      # Local data seeding
```

**Authentication flow:**
1. React app sends Firebase JWT in the `Authorization` header
2. `FirebaseAuthMiddleware` validates the token with Firebase Admin SDK
3. `UserId` is extracted and scoped to the request — all DB queries are filtered by it

### Frontend

```
frontend/src/
├── App.tsx                       # Root layout: collapsible sidebar + routing
├── firebase.ts                   # Firebase client config
├── api/
│   ├── client.ts                 # Fetch wrapper with auto auth token injection
│   ├── schemas.ts                # Zod validation schemas
│   ├── types.ts                  # Derived TypeScript types
│   ├── openapi.gen.ts            # Generated OpenAPI types (npm run generate:api)
│   └── generated/                # Generated API schema helpers
├── components/
│   ├── AppNavLink.tsx            # Sidebar nav link (hides label when collapsed)
│   ├── DatePicker.tsx            # Shared calendar date picker component
│   └── ErrorBoundary.tsx         # Catches render errors in the component tree
├── contexts/
│   └── CurrencyContext.tsx       # User currency preference
├── hooks/
│   └── useCurrencyFormatter.ts   # Formats amounts per the active currency
├── utils/
│   ├── currency/                 # Currency constants, formatter, token extractor
│   └── encryption.ts             # Client-side encryption helper
└── modules/
    ├── auth/
    │   ├── LoginPage.tsx         # Sign in / sign up
    │   └── ProfileModal.tsx      # Change display name, email, currency & password
    ├── dashboard/
    │   └── DashboardPage.tsx
    ├── transactions/
    │   └── TransactionsPage.tsx
    ├── savings/
    │   ├── SavingsPage.tsx
    │   ├── GoalPage.tsx
    │   ├── SavingsFormsSection.tsx
    │   └── GoalProgressSection.tsx
    ├── portfolio/
    │   ├── PortfolioPage.tsx     # Assets, liabilities, performance, net worth chart
    │   ├── DatePicker.tsx
    │   └── MonthPicker.tsx
    └── reports/
        └── ReportsPage.tsx       # Monthly report view with CSV/PDF export
```

---

## Tests

Backend tests live in `backend/BudgetApp.Tests/` and use xUnit + Moq.

```
BudgetApp.Tests/
├── Dashboard/
│   ├── DashboardServiceTests.cs
│   ├── DashboardControllerTests.cs
│   ├── AdvisorServiceTests.cs
│   ├── AdvisorControllerTests.cs
│   ├── ClaudeAdvisorTests.cs
│   └── SpendingTrendServiceTests.cs
├── Transactions/
│   ├── TransactionsControllerTests.cs
│   ├── BudgetServiceTests.cs
│   └── BudgetsControllerTests.cs
├── Savings/
│   ├── SavingsServiceTestBase.cs
│   ├── SavingsServiceContributionTests.cs
│   ├── SavingsServiceBalanceAndAbandonTests.cs
│   └── SavingsProgressServiceTests.cs
├── Portfolio/
│   ├── PortfolioCalculatorTests.cs  # Price/amount history resolution logic
│   └── PortfolioServiceTests.cs     # Net worth, allocation, performance calculations
├── Reports/
│   └── MonthlyReportServiceTests.cs
└── Helpers/
    └── FakeHttpHandler.cs
```

Run tests:
```bash
cd backend
dotnet test BudgetApp.Tests/BudgetApp.Tests.csproj
```

---

## CI

GitHub Actions runs on every pull request to `main` (`.github/workflows/ci.yml`):

- **Backend** — restore, build (warnings as errors), format check, test
- **Frontend** — install, Biome lint, TypeScript type-check, test

---

## Common Commands

**Backend:**
```bash
dotnet run          # Start API (http://localhost:5000)
dotnet build        # Build
dotnet test         # Run tests
dotnet format       # Fix formatting
```

**Frontend:**
```bash
npm run dev         # Start dev server (http://localhost:5173)
npm run build       # Production build
npm run lint        # Biome lint
npm run check       # Lint + format
npx tsc -b          # Type-check
```

**Docker:**
```bash
docker compose up --build          # Build and start all services
docker compose up -d               # Start in background
docker compose down                # Stop all services
docker compose logs -f api         # Stream backend logs
docker compose exec ollama ollama pull llama3.2  # Pull AI model (first run)
```

---

## Troubleshooting

### Backend won't start

- Check MongoDB is running: `docker ps | grep mongo`
- Check port 5000 is free: `lsof -i :5000`
- Verify `firebase-service-account.json` exists in `backend/BudgetApp.Api/`

### Frontend can't reach the API (manual setup)

- Verify `VITE_API_URL` in `frontend/.env` matches the backend port
- Check CORS settings in `backend/Program.cs`

### Ollama returns 503

- The model hasn't been pulled yet — run `ollama pull llama3.2` (inside the container or locally)
- Check the container is running: `docker compose ps ollama`

### MongoDB connection fails

- Use `mongodb://mongo:27017` inside Docker / dev container
- Use `mongodb://localhost:27017` for manual local setup
