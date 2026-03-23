#!/usr/bin/env bash
set -euo pipefail

SOURCE_PATH="${1:-../openai-api-service/openapi.yaml}"
DESTINATION="openapi.yaml"

if [[ ! -f "$SOURCE_PATH" ]]; then
  echo "Source OpenAPI file not found: $SOURCE_PATH" >&2
  exit 1
fi

cp "$SOURCE_PATH" "$DESTINATION"
echo "Synced OpenAPI to $DESTINATION"
