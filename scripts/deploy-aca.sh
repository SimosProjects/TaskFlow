#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   scripts/deploy-aca.sh v0.2.6
#
# Required env vars:
#   TF_RG, TF_CA_APP
#
# Optional env vars:
#   DH_IMAGE_BASE (default: docker.io/simosdevapps/taskflow-api)

TAG="${1:-}"
if [[ -z "$TAG" ]]; then
  echo "ERROR: Missing tag. Example: scripts/deploy-aca.sh v0.2.6"
  exit 1
fi

: "${TF_RG:?TF_RG is required (e.g., taskflow-prod-rg-westus2)}"
: "${TF_CA_APP:?TF_CA_APP is required (e.g., taskflow-hello)}"

DH_IMAGE_BASE="${DH_IMAGE_BASE:-docker.io/simosdevapps/taskflow-api}"
DH_IMAGE="${DH_IMAGE_BASE}:${TAG}"

echo "==> Building API project..."
dotnet build TaskFlow.Api/TaskFlow.Api.csproj

echo "==> Building + pushing image: ${DH_IMAGE}"
docker buildx build \
  --no-cache \
  --platform linux/amd64,linux/arm64 \
  -t "$DH_IMAGE" \
  --push \
  .

echo "==> Deploying to Azure Container Apps..."
az containerapp update \
  --name "$TF_CA_APP" \
  --resource-group "$TF_RG" \
  --image "$DH_IMAGE" \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://+:8080 EnableSwagger=true

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
echo "Swagger: https://${FQDN}/swagger"
curl -s -o /dev/null -w "GET /swagger -> %{http_code}\n" "https://${FQDN}/swagger"
curl -s -o /dev/null -w "GET /swagger/v1/swagger.json -> %{http_code}\n" "https://${FQDN}/swagger/v1/swagger.json"
curl -s -o /dev/null -w "GET /api/tasks -> %{http_code}\n" "https://${FQDN}/api/tasks"

echo "==> Done."
