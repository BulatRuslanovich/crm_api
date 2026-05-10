#!/bin/sh
set -e

MODEL="${OLLAMA_MODEL:-qwen2.5:7b}"

ollama serve &
SERVER_PID=$!

echo "Waiting for ollama to come up..."
until ollama list >/dev/null 2>&1; do
  sleep 1
done

if ! ollama list | awk 'NR>1 {print $1}' | grep -qx "$MODEL"; then
  echo "Pulling model $MODEL..."
  ollama pull "$MODEL"
fi

echo "Ollama ready with model: $MODEL"
wait "$SERVER_PID"
