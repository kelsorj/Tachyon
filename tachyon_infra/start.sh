#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

ENV_FILE=".env"
if [[ ! -f "$ENV_FILE" ]]; then
  if [[ -f "env.example" ]]; then
    echo "No .env found; using defaults. To customize: cp env.example .env"
  fi
  docker compose -f compose.yaml up -d
else
  docker compose --env-file "$ENV_FILE" -f compose.yaml up -d
fi

echo "Infra up:"
echo "  Redis:  localhost:6379"
echo "  MinIO:  http://localhost:9000 (S3)  /  http://localhost:9001 (console)"





