# Budget App

A full-stack personal finance application for tracking transactions, managing budgets, monitoring savings goals, and analyzing net worth with AI-powered financial insights.

## Tech Stack

**Backend**
- ASP.NET Core 8.0 (C#)
- MongoDB 7
- Firebase Admin SDK (Authentication)
- Anthropic Claude API (AI Financial Advisor — cloud)
- Ollama (AI Financial Advisor — local)

**Frontend**
- React 19 with TypeScript
- Vite (Build tool)
- React Router (Routing)
- Firebase Authentication
- Orval (OpenAPI TypeScript code generation)
- Biome (Linting & Formatting)

**Infrastructure**
- Docker & Docker Compose (MongoDB + Ollama services)
- Dev Container support

## Features

- Transaction tracking with categories
- Budget management with spending limits
- Savings goals with contribution tracking
- Portfolio management (assets & liabilities)
- Net worth calculation
- AI-powered financial advisor
- Firebase authentication

## Architecture

### Backend Architecture

The backend follows a **modular, feature-based architecture** built on ASP.NET Core:

```
backend/BudgetApp.Api/
├── Program.cs                    # Application entry point, DI configuration
├── Configuration/                # Settings classes
│   └── MongoDbSettings.cs
├── Middleware/                   # Request pipeline middleware
│   └── FirebaseAuthMiddleware.cs # JWT validation & user context
├── Controllers/                  # Base controllers
│   └── ApiControllerBase.cs
└── Modules/                      # Feature modules (domain-driven)
    ├── Auth/                     # User authentication & profiles
    │   ├── AuthController.cs
    │   └── Models/
    ├── Dashboard/                # Summary data & AI advisor
    │   ├── DashboardController.cs
    │   ├── AdvisorController.cs
    │   └── Models/
    ├── Transactions/             # Transactions & budgets
    │   ├── TransactionsController.cs
    │   ├── BudgetsController.cs
    │   └── Models/
    │       ├── Transaction.cs
    │       ├── Budget.cs
    │       └── Categories.cs
    ├── Savings/                  # Goals & contributions
    │   ├── GoalsController.cs
    │   ├── ContributionsController.cs
    │   └── Models/
    └── Portfolio/                # Assets, liabilities, net worth
        ├── AssetsController.cs
        ├── LiabilitiesController.cs
        ├── NetWorthController.cs
        └── Models/
```

**Key Design Patterns:**
- **Modular Organization**: Each domain (Transactions, Savings, Portfolio) is a self-contained module
- **Dependency Injection**: MongoDB client, Firebase Admin, HTTP client registered in `Program.cs`
- **Middleware Pipeline**: Firebase JWT authentication runs before all requests
- **Document-based Models**: MongoDB documents map directly to C# classes
- **RESTful APIs**: Controllers expose standard CRUD operations

**Authentication Flow:**
1. Frontend sends Firebase JWT in `Authorization` header
2. `FirebaseAuthMiddleware` validates token with Firebase Admin SDK
3. User ID extracted and available to all controllers
4. All data operations are scoped to authenticated user

### Frontend Architecture

The frontend follows a **module-based architecture** with React:

```
frontend/src/
├── main.tsx                      # Application entry point
├── App.tsx                       # Root component with routing
├── firebase.ts                   # Firebase configuration
├── api/
│   ├── client.ts                 # Fetch API client with auth
│   └── generated/                # Auto-generated types (via Orval)
└── modules/                      # Feature modules (route-based)
    ├── auth/
    │   └── LoginPage.tsx         # Firebase Auth UI
    ├── dashboard/
    │   └── DashboardPage.tsx     # Summary & AI insights
    ├── transactions/
    │   └── TransactionsPage.tsx  # Transactions & budgets
    ├── savings/
    │   └── SavingsPage.tsx       # Goals & contributions
    └── portfolio/
        └── PortfolioPage.tsx     # Assets, liabilities, net worth
```

**Key Design Patterns:**
- **Module-based Organization**: Each feature area is a separate module with its own page
- **Centralized API Client**: `api/client.ts` handles all HTTP requests with automatic auth token injection
- **Protected Routes**: React Router guards routes requiring authentication
- **Firebase Integration**: Authentication state managed via Firebase SDK
- **Component-per-Route**: Each module has a single page component

**Authentication Flow:**
1. User logs in via Firebase Auth UI
2. Firebase returns JWT token
3. Token stored in memory and sent with all API requests
4. API client automatically attaches token to `Authorization` header
5. Protected routes redirect to login if no token present

**Data Flow:**
```
User Action → Component → API Client → Backend API → MongoDB
                ↓              ↓
          Local State ←  Response
```

## Setup

### Option 1: Dev Container (Recommended) ⭐

**One-click setup with auto-start!**

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/)
2. Install [VS Code](https://code.visualstudio.com/) with [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
3. Open project in VS Code
4. Click "Reopen in Container" when prompted (or use Command Palette: `Dev Containers: Reopen in Container`)
5. Container will automatically:
   - Install .NET 8 SDK and Node.js 20
   - Start MongoDB 7 and Ollama
   - Run `npm install` and `dotnet restore`
   - **Start the backend and frontend** (via `postStartCommand`)
   - Pull the `llama3.2` model into Ollama in the background
   - Open the frontend at http://localhost:5173 in your browser

That's it — all services start automatically on every container start.

**View logs:**
```bash
tail -f /tmp/backend.log    # ASP.NET Core API
tail -f /tmp/frontend.log   # Vite dev server
tail -f /tmp/ollama-pull.log  # Ollama model pull
```

### Option 2: Manual Setup

**Prerequisites:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/products/docker-desktop/) (for MongoDB)

**Steps:**

1. **Start MongoDB**
   ```bash
   docker run -d --name mongodb \
     -p 27017:27017 \
     -e MONGO_INITDB_DATABASE=budgetapp \
     mongo:7
   ```

2. **Install dependencies**
   ```bash
   # Backend
   cd backend && dotnet restore

   # Frontend
   cd frontend && npm install
   ```

3. **Configure environment variables**

   Create `frontend/.env`:
   ```env
   VITE_API_URL=http://localhost:5000
   VITE_FIREBASE_API_KEY=your_firebase_api_key
   VITE_FIREBASE_AUTH_DOMAIN=your_project.firebaseapp.com
   VITE_FIREBASE_PROJECT_ID=your_project_id
   ```

   **For manual setup only**, update `backend/BudgetApp.Api/appsettings.Development.json`:
   ```json
   {
     "MongoDB": {
       "ConnectionString": "mongodb://localhost:27017",
       "DatabaseName": "budgetapp"
     },
     "Firebase": {
       "ProjectId": "your_project_id",
       "ServiceAccountPath": "path/to/firebase-service-account.json"
     },
     "Anthropic": {
       "ApiKey": "your_anthropic_api_key"
     }
   }
   ```

   **Note**: Dev container users should keep `mongodb://mongo:27017` (already configured).

## Running the Application

### Start

**Option 1: VS Code Tasks (Recommended)**

Press `Ctrl+Shift+P` → Run Task → Select:
- **"Start All Services"** - Starts both backend and frontend
- **"Start Backend"** - Starts only backend
- **"Start Frontend"** - Starts only frontend

**Option 2: Command Line**

**In Dev Container:**
```bash
# Backend (http://localhost:5000)
cd /workspace/backend/BudgetApp.Api
dotnet run

# Frontend (http://localhost:5173)
cd /workspace/frontend
npm run dev
```

**Manual Setup:**
```bash
# Backend (http://localhost:5000)
cd backend/BudgetApp.Api
dotnet run

# Frontend (http://localhost:5173)
cd frontend
npm run dev
```

**Option 3: Startup Script (Dev Container)**
```bash
./start-dev.sh
```

## Database Connection

**In Dev Container:**
- Connection string: `mongodb://mongo:27017`
- Database: `budgetapp`

**Manual setup:**
- Connection string: `mongodb://localhost:27017`
- Database: `budgetapp`

### Accessing MongoDB

MongoDB uses a binary protocol (not HTTP), so you can't access it via web browser. Use one of these tools:

**Option 1: VS Code MongoDB Extension** (Already installed in dev container)
1. Click MongoDB icon in VS Code sidebar
2. Add connection: `mongodb://localhost:27017` (or `mongodb://mongo:27017` inside container)
3. Browse databases and collections visually

**Option 2: MongoDB Compass** (GUI Application)
1. Download from [mongodb.com/products/compass](https://www.mongodb.com/products/compass)
2. Connect to `mongodb://localhost:27017`
3. Full-featured database management

**Option 3: MongoDB Shell** (Command line)
```bash
# From host machine (if mongosh installed)
mongosh mongodb://localhost:27017

# From inside dev container or MongoDB container
docker exec -it <mongodb-container-id> mongosh
```

MongoDB port 27017 is forwarded to the host machine for external access.

## Authentication

The app uses Firebase Authentication. Users must sign up/login through the Firebase Auth UI in the frontend.

**Development without Firebase:** If `VITE_FIREBASE_API_KEY` is not set in `frontend/.env`, the frontend skips the login screen entirely. On the backend, `FirebaseAuthMiddleware` detects that Firebase Admin SDK is not initialised and assigns a fixed `dev-user` ID to every request. This lets you develop and test all API endpoints without a Firebase project.

## AI Advisor

The advisor supports two providers, selectable per-request from the Dashboard:

| Provider | How it works | Requires |
|---|---|---|
| **Ollama** (default) | Runs `llama3.2` locally in the `ollama` Docker service | Nothing — included in docker-compose |
| **Claude** | Calls Anthropic's API | `Anthropic:ApiKey` in `appsettings.json` |

### Setting up Ollama

The `ollama` Docker service starts automatically with the dev container. The script `.devcontainer/pull-ollama-model.sh` runs in the background on every container start — it waits for Ollama to be ready then pulls `llama3.2` if not already present. No manual steps needed.

The model name and Ollama URL are configured in `appsettings.json`:
```json
"Ollama": {
  "BaseUrl": "http://ollama:11434",
  "Model": "llama3.2"
}
```

### Setting up Claude

Add your Anthropic API key to `backend/BudgetApp.Api/appsettings.json`:
```json
"Anthropic": {
  "ApiKey": "sk-ant-..."
}
```

Or set it as an environment variable / user secret — never commit the key to git.

## Type-Safe API Client with OpenAPI

This project uses **Orval** to automatically generate TypeScript types and API client code from the backend's OpenAPI specification. This ensures type safety between your C# backend models and TypeScript frontend.

### How It Works

1. **Backend exposes OpenAPI spec** at `http://localhost:5000/swagger/v1/swagger.json`
2. **Orval reads the spec** and generates TypeScript types matching your C# models
3. **Frontend uses generated types** for type-safe API calls

### Setup

The project is already configured with:
- ✅ Orval installed (`orval.config.ts`)
- ✅ Generation script in `package.json`
- ✅ Custom API client wrapper (`src/api/client.ts`)

### Generating Types

**Before generating**, ensure the backend is running:

```bash
# Terminal 1: Start the backend
cd backend/BudgetApp.Api
dotnet run
```

Then generate the TypeScript types:

```bash
# Terminal 2: Generate API client
cd frontend
npm run generate:api
```

This creates type-safe API client code in `frontend/src/api/generated/`:
- `budgetapp.schemas.ts` - TypeScript interfaces matching your C# models
- `*.ts` - API functions grouped by controller

### Usage Example

**Before (manual typing):**
```typescript
const transactions = await apiFetch<any>('/api/transactions');
```

**After (type-safe):**
```typescript
import { Transaction } from './api/generated';

const transactions = await apiFetch<Transaction[]>('/api/transactions');
// transactions is now typed as Transaction[]
// Autocomplete and type checking work!
```

### Benefits

- ✅ **Type Safety**: Compile-time errors if frontend doesn't match backend
- ✅ **Autocomplete**: Full IntelliSense for all API models
- ✅ **Single Source of Truth**: C# models define the contract
- ✅ **Auto-Sync**: Regenerate when backend changes

### When to Regenerate

Run `npm run generate:api` whenever you:
- Add/modify C# model classes
- Add/modify API controller endpoints
- Change request/response types

### Configuration

See `frontend/orval.config.ts` for configuration options. The generated code uses your existing `apiFetch` wrapper, so Firebase authentication is automatically handled.

## Development Workflow

### Common Commands

**Backend:**
```bash
cd backend/BudgetApp.Api
dotnet run                    # Run the API
dotnet build                  # Build the project
dotnet restore                # Restore dependencies
```

**Frontend:**
```bash
cd frontend
npm run dev                   # Start dev server
npm run build                 # Build for production
npm run generate:api          # Generate TypeScript types from backend
npm run lint                  # Lint code
npm run format                # Format code with Biome
npm run check                 # Lint and format
```

### Project Structure

```
budget-app/
├── backend/
│   └── BudgetApp.Api/        # .NET API
│       ├── Modules/          # Feature modules
│       ├── Middleware/       # Auth middleware
│       └── Program.cs        # Entry point
├── frontend/
│   └── src/
│       ├── api/              # API client
│       ├── modules/          # Feature pages
│       └── firebase.ts       # Auth config
├── .devcontainer/            # Dev container config
└── README.md                 # This file
```

### API Documentation

- **Swagger UI**: http://localhost:5000/swagger
- **OpenAPI JSON**: http://localhost:5000/swagger/v1/swagger.json

### Troubleshooting

**Backend won't start:**
- Check MongoDB is running: `docker ps | grep mongo`
- Check port 5000 isn't in use: `netstat -ano | findstr :5000` (Windows) or `lsof -i :5000` (Mac/Linux)

**Frontend can't connect to API:**
- Verify `VITE_API_URL` in `.env` matches backend URL
- Check CORS settings in `Program.cs`

**MongoDB connection fails:**
- Verify connection string matches your setup (mongo vs localhost)
- Check MongoDB container is running
- Verify port 27017 is accessible

**Type generation fails:**
- Ensure backend is running and accessible
- Check `http://localhost:5000/swagger/v1/swagger.json` returns valid JSON
- Verify `orval.config.ts` has correct URL
