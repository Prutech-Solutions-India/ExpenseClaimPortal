#!/usr/bin/env bash
# Deployment hook for the pilot ASP.NET Core service.
#
# Replace the body with your real deployment mechanism (Azure Web App, container
# push, ECS...). It must be idempotent and must exit non-zero on failure so the
# Release Agent records an accurate deployment conclusion.
set -euo pipefail

ENVIRONMENT="${1:?environment argument is required (dev|qa)}"
COMMIT_SHA="${2:?commit sha argument is required}"

: "${DEPLOY_TARGET:?DEPLOY_TARGET must be set for this environment}"
: "${DEPLOY_TOKEN:?DEPLOY_TOKEN must be set for this environment}"

if [ ! -d publish ]; then
  echo "No publish/ directory: the build output was not unpacked." >&2
  exit 1
fi

echo "Deploying commit ${COMMIT_SHA} to ${ENVIRONMENT} target ${DEPLOY_TARGET}"

# --- replace from here -----------------------------------------------------
echo "No deployment backend is wired up yet."
echo "Implement the promotion for '${ENVIRONMENT}' before running the pilot."
exit 1
# --- to here ---------------------------------------------------------------
