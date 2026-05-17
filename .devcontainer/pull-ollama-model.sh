#!/bin/bash
# Waits for the Ollama service to be ready, then pulls the model if not already present.
# Runs in the background on container start — safe to call multiple times.

MODEL="llama3.2"

echo "[ollama] Waiting for Ollama service..."
until curl -sf http://ollama:11434 > /dev/null 2>&1; do
  sleep 3
done

if curl -s http://ollama:11434/api/tags | grep -q "\"$MODEL\""; then
  echo "[ollama] Model $MODEL already present."
else
  echo "[ollama] Pulling $MODEL (this takes a few minutes on first run)..."
  curl -s http://ollama:11434/api/pull -d "{\"name\":\"$MODEL\"}" > /dev/null
  echo "[ollama] Model $MODEL ready."
fi
