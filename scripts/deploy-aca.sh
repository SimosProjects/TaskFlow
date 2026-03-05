#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   scripts/deploy-aca.sh v0.3.0
#
# Required env vars:
#   TF_RG          — Azure resource group (e.g., taskflow-prod-rg-westus2)
#   TF_CA_APP      — Container App name (e.g., taskflow-hello)
#   TF_CONNECTION_STRING — Postgres connection string (never passed as plaintext)
#
# Optional env vars:
#   DH_IMAGE_BASE  — Docker Hub image base (default: docker.io/simosdevapps/taskflow-api)

TAG="${1:-}"
if [[ -z "$TAG" ]]; then
  echo "ERROR: Missing tag. Example: scripts/deploy-aca.sh v0.3.0"
  exit 1
fi

: "${TF_RG:?TF_RG is required (e.g., taskflow-prod-rg-westus2)}"
: "${TF_CA_APP:?TF_CA_APP is required (e.g., taskflow-hello)}"
: "${TF_CONNECTION_STRING:?TF_CONNECTION_STRING is required — set it in your shell, never commit it}"

DH_IMAGE_BASE="${DH_IMAGE_BASE:-docker.io/simosdevapps/taskflow-api}"
DH_IMAGE="${DH_IMAGE_BASE}:${TAG}"

ACA_SECRET_NAME="taskflowdb-connection-string"

echo "==> Building API project..."
dotnet build TaskFlow.Api/TaskFlow.Api.csproj

echo "==> Building + pushing image: ${DH_IMAGE}"
docker buildx build \
  --no-cache \
  --platform linux/amd64,linux/arm64 \
  -t "$DH_IMAGE" \
  --push \
  .

echo "==> Setting ACA secret: ${ACA_SECRET_NAME}"
# Stores the connection string as an ACA secret so it never appears in
# revision history, shell logs, or az containerapp show output.
az containerapp secret set \
  --name "$TF_CA_APP" \
  --resource-group "$TF_RG" \
  --secrets "${ACA_SECRET_NAME}=${TF_CONNECTION_STRING}"

echo "==> Deploying to Azure Container Apps..."
az containerapp update \
  --name "$TF_CA_APP" \
  --resource-group "$TF_RG" \
  --image "$DH_IMAGE" \
  --set-env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    EnableSwagger=true \
    "ConnectionStrings__TaskFlowDb=secretref:${ACA_SECRET_NAME}"

echo "==> Waiting for latest revision to become ready..."
az containerapp show \
  --name "$TF_CA_APP" \
  --resource-group "$TF_RG" \
  --query "{image:properties.template.containers[0].image, latestReady:properties.latestReadyRevisionName, latestRevision:properties.latestRevisionName}" \
  -o jsonc

FQDN="$(az containerapp show \
  --name "$TF_CA_APP" \
  --resource-group "$TF_RG" \
  --query "properties.configuration.ingress.fqdn" -o tsv)"

echo "==> Verifying endpoints..."
echo "FQDN: https://${FQDN}"
curl -s -o /dev/null -w "GET /swagger -> %{http_code}\n" "https://${FQDN}/swagger"
curl -s -o /dev/null -w "GET /swagger/v1/swagger.json -> %{http_code}\n" "https://${FQDN}/swagger/v1/swagger.json"
curl -s -o /dev/null -w "GET /api/tasks -> %{http_code}\n" "https://${FQDN}/api/tasks"
curl -s -o /dev/null -w "GET /health/live -> %{http_code}\n" "https://${FQDN}/health/live"
curl -s -o /dev/null -w "GET /health/ready -> %{http_code}\n" "https://${FQDN}/health/ready"

echo "==> Done."