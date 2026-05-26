#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
#
# One-time cleanup of the cross-project overlap that the first run of
# azure-bootstrap.sh caused: it accidentally reused the existing
# `github-bronnoysund-mvp` App Registration (prefix-match bug) and grafted
# the main repo's federated credentials + RBAC onto it.
#
# This script:
#
#   1. Creates a dedicated `github-bronnoysund` App Registration via the
#      (fixed) azure-bootstrap flow. Exact-match displayName filter keeps
#      this distinct from `github-bronnoysund-mvp`.
#   2. Removes the federated credentials for repo:erlingsm/Bronnoysund.Lookup
#      that were grafted onto github-bronnoysund-mvp.
#   3. Removes the role assignments on bronnoysund-lookup-rg resources
#      that were grafted onto the MVP service principal.
#   4. Repoints the GitHub repo secrets to the new dedicated app.
#
# Re-running is safe (idempotent probes at every step).

set -euo pipefail

readonly REPO="erlingsm/Bronnoysund.Lookup"
readonly RG="bronnoysund-lookup-rg"
readonly ACR="bronnoysundlookup30020"
readonly APP_WEB="bronnoysund-web"
readonly APP_WEBAPI="bronnoysund-webapi"
readonly NEW_APP_NAME="github-bronnoysund"
readonly MVP_APP_NAME="github-bronnoysund-mvp"

echo "==> Sanity-check tooling"
command -v az >/dev/null || { echo "ERROR: az CLI required" >&2; exit 1; }
command -v gh >/dev/null || { echo "ERROR: gh CLI required" >&2; exit 1; }
az account show --only-show-errors >/dev/null \
    || { echo "ERROR: 'az login' required" >&2; exit 1; }
gh auth status >/dev/null 2>&1 \
    || { echo "ERROR: 'gh auth login' required" >&2; exit 1; }

echo "==> Locating the MVP app (exact-match by displayName)"
MVP_APP_ID="$(az ad app list --filter "displayName eq '$MVP_APP_NAME'" --query '[0].appId' -o tsv)"
if [[ -z "$MVP_APP_ID" ]]; then
    echo "    No app named '$MVP_APP_NAME' found — nothing to clean up from."
    echo "    Run ./deploy/azure-bootstrap.sh to set up the new app from scratch."
    exit 0
fi
MVP_SP_OID="$(az ad sp list --filter "appId eq '$MVP_APP_ID'" --query '[0].id' -o tsv)"
echo "    MVP app:  $MVP_APP_NAME ($MVP_APP_ID)"
echo "    MVP SP:   $MVP_SP_OID"

echo "==> 1/4 Bootstrap the dedicated $NEW_APP_NAME"
# Defer to the bootstrap script (which now uses exact-match filtering + the
# new name). It creates the app + SP + federated creds + RBAC + GH vars.
"$(dirname "$0")/azure-bootstrap.sh"

echo ""
echo "==> 2/4 Remove main-repo federated credentials from the MVP app"
# Find any federated credential on the MVP app whose subject targets the main
# repo (master or main branch). Delete each by id.
SUBJECTS_TO_REMOVE=(
    "repo:${REPO}:ref:refs/heads/master"
    "repo:${REPO}:ref:refs/heads/main"
)
for SUBJECT in "${SUBJECTS_TO_REMOVE[@]}"; do
    CRED_ID="$(az ad app federated-credential list --id "$MVP_APP_ID" \
                 --query "[?subject=='${SUBJECT}'].id" -o tsv)"
    if [[ -z "$CRED_ID" ]]; then
        echo "    No federated credential for $SUBJECT on MVP app — already clean"
        continue
    fi
    az ad app federated-credential delete --id "$MVP_APP_ID" \
        --federated-credential-id "$CRED_ID"
    echo "    Removed federated credential $SUBJECT from $MVP_APP_NAME"
done

echo ""
echo "==> 3/4 Remove main-repo role assignments from the MVP service principal"
# AcrPush on the ACR used by the main repo.
ACR_ID="$(az acr show --name "$ACR" --query id -o tsv)"
SCOPES_TO_CHECK=(
    "$ACR_ID"
    "$(az containerapp show --name "$APP_WEB"    --resource-group "$RG" --query id -o tsv)"
    "$(az containerapp show --name "$APP_WEBAPI" --resource-group "$RG" --query id -o tsv)"
)
for SCOPE in "${SCOPES_TO_CHECK[@]}"; do
    while IFS= read -r ROLE; do
        [[ -z "$ROLE" ]] && continue
        az role assignment delete \
            --assignee "$MVP_APP_ID" \
            --scope "$SCOPE" \
            --role "$ROLE" >/dev/null
        echo "    Removed $ROLE on $(basename "$SCOPE") from MVP app"
    done < <(az role assignment list --assignee "$MVP_APP_ID" --scope "$SCOPE" \
              --query "[].roleDefinitionName" -o tsv)
done

echo ""
echo "==> 4/4 Repoint GitHub repo secrets to the new app"
# gh secret get does not exist (secrets are write-only), so we re-set every
# time. The bootstrap step above also writes them; this block is a belt-
# and-braces sanity update in case the user mutated them between runs.
NEW_APP_ID="$(az ad app list --filter "displayName eq '$NEW_APP_NAME'" --query '[0].appId' -o tsv)"
gh secret set AZURE_CLIENT_ID --repo "$REPO" --body "$NEW_APP_ID" >/dev/null
echo "    GitHub AZURE_CLIENT_ID -> $NEW_APP_ID"

cat <<DONE

==> Cleanup complete.

  - Dedicated app:     $NEW_APP_NAME ($NEW_APP_ID)
  - MVP app:           $MVP_APP_NAME ($MVP_APP_ID)
                       — federated credentials for $REPO removed
                       — role assignments on $RG / $ACR removed
  - GitHub vars on $REPO now point at the dedicated app.

Verify with:
  az ad app federated-credential list --id $MVP_APP_ID --query '[].subject' -o tsv
  az role assignment list --assignee $MVP_APP_ID --all --query '[].{role:roleDefinitionName, scope:scope}' -o table
  gh variable get AZURE_CLIENT_ID --repo $REPO
DONE
