#!/usr/bin/env bash
set -e

# ── Checks ────────────────────────────────────────────────────────────────────

if ! command -v docker &>/dev/null; then
  echo "ERROR: Docker is not installed. Get it at https://www.docker.com/products/docker-desktop/"
  exit 1
fi

if ! docker info &>/dev/null; then
  echo "ERROR: Docker is not running. Start Docker Desktop and try again."
  exit 1
fi

# Copy frontend/.env to root .env if root .env is missing
if [ ! -f .env ]; then
  if [ -f frontend/.env ]; then
    cp frontend/.env .env
    echo "Copied frontend/.env → .env"
  else
    echo "ERROR: frontend/.env not found."
    echo "       Copy frontend/.env.example to frontend/.env and fill in your Firebase keys."
    exit 1
  fi
fi

if [ ! -f backend/BudgetApp.Api/firebase-service-account.json ]; then
  echo "WARNING: backend/BudgetApp.Api/firebase-service-account.json not found."
  echo "         The backend will start but authentication will not work."
  echo "         Place your Firebase service account file there and restart."
fi

# ── Start services ────────────────────────────────────────────────────────────

echo "Starting all services..."
docker compose up -d --build

# ── Pull Ollama model if not already present ──────────────────────────────────

echo "Checking Ollama model..."
if ! docker compose exec -T ollama ollama list 2>/dev/null | grep -q "llama3.2"; then
  echo "Pulling llama3.2 model (~2 GB, first time only)..."
  docker compose exec ollama ollama pull llama3.2
  echo "Model ready."
else
  echo "llama3.2 already present."
fi

# ── Done ──────────────────────────────────────────────────────────────────────

echo ""
echo "Budget App is running at http://localhost:3000"
echo ""
echo "Useful commands:"
echo "  docker compose logs -f        # stream all logs"
echo "  docker compose logs -f api    # backend logs only"
echo "  docker compose down           # stop everything"
