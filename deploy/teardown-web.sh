#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
#
# Tear down the Azure resource group that hosts the Web + WebApi
# container apps. Destroys both container apps, the managed environment,
# the registry, and any other resources inside the group.
#
# Usage:
#   ./deploy/teardown-web.sh         # prints what would happen, exits
#   ./deploy/teardown-web.sh --yes   # confirms intent, performs deletion

set -euo pipefail

readonly RG="bronnoysund-lookup-rg"

if [[ "${1:-}" != "--yes" ]]; then
    cat <<USAGE
This will permanently delete resource group '${RG}' and EVERY resource in it:

  * Container App  bronnoysund-web
  * Container App  bronnoysund-webapi
  * Container Apps env  bronnoysund-env
  * Container Registry  bronnoysundlookup30020 (+ all image tags)
  * Log Analytics workspace
  * Application Insights component

Re-run with --yes to confirm:
    ./deploy/teardown-web.sh --yes
USAGE
    exit 1
fi

if ! az account show --only-show-errors >/dev/null 2>&1; then
    echo "ERROR: 'az login' required before teardown." >&2
    exit 1
fi

echo "==> Deleting resource group ${RG} (async)"
az group delete --name "$RG" --yes --no-wait
echo "Deletion queued. Check status with:"
echo "    az group show --name ${RG} 2>&1 | head -3"
echo ""
echo "Note: the Azure AD App Registration for GitHub OIDC lives in your"
echo "tenant, not in the resource group, and survives this teardown."
echo "Remove it separately if you want a fully clean slate:"
echo "    az ad app delete --id <appId>"
