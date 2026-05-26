#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
#
# Local manual deploy for Bronnoysund.BlazorWeb -> Azure Container Apps.
# Mirrors .github/workflows/cd-web.yml.
#
# Usage:
#   ./deploy/deploy-web.sh           # full pipeline
#   ./deploy/deploy-web.sh --dry-run # build + test only
#
# Prereqs: az login, .NET 10 SDK, access to bronnoysund-lookup-rg.

set -euo pipefail

readonly RG="bronnoysund-lookup-rg"
readonly ACR="bronnoysundlookup30020"
readonly APP="bronnoysund-web"
readonly IMAGE="bronnoysund-web"
readonly DOCKERFILE="src/Bronnoysund.BlazorWeb/Dockerfile"

DRY_RUN=0
[[ "${1:-}" == "--dry-run" ]] && DRY_RUN=1

cd "$(dirname "$0")/../Kode"

echo "==> 1/5 Build"
dotnet build Bronnoysund.Web.slnf -c Release --nologo -v minimal

echo "==> 2/5 Test"
dotnet test Bronnoysund.Web.slnf -c Release --no-build --nologo

if [[ $DRY_RUN -eq 1 ]]; then
    echo "==> dry run complete (Azure steps skipped)"
    exit 0
fi

echo "==> 3/5 Verify Azure login"
if ! az account show --only-show-errors >/dev/null 2>&1; then
    echo "ERROR: 'az login' required before deploy." >&2
    exit 1
fi

TAG="$(git rev-parse --short HEAD)"
echo "==> 4/5 ACR build (${IMAGE}:${TAG})"
az acr build \
    --registry "$ACR" \
    --image "${IMAGE}:${TAG}" \
    --image "${IMAGE}:latest" \
    --file "$DOCKERFILE" \
    .

echo "==> 5/5 Update Container App"
az containerapp update \
    --name "$APP" \
    --resource-group "$RG" \
    --image "${ACR}.azurecr.io/${IMAGE}:${TAG}" \
    --query "properties.latestRevisionName" \
    -o tsv

echo "==> Smoke test"
FQDN=$(az containerapp show --name "$APP" --resource-group "$RG" \
    --query properties.configuration.ingress.fqdn -o tsv)
sleep 10
HTTP_CODE="$(curl -fsS --max-time 30 -o /dev/null -w '%{http_code}' "https://${FQDN}/" || echo 000)"
if [[ "$HTTP_CODE" == "200" ]]; then
    echo "Live: https://${FQDN}/ -> ${HTTP_CODE}"
else
    echo "WARN: smoke test got HTTP ${HTTP_CODE} (revision may still be warming up)" >&2
    exit 1
fi
