#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
#
# One-time Azure setup for the cd-web.yml and cd-webapi.yml workflows.
# Idempotent — re-running is safe. Creates / verifies in order:
#
#   1. Azure AD App Registration + Service Principal       (GitHub OIDC identity)
#   2. Federated credential                                (trust on repo + branch)
#   3. AcrPush role on the container registry              (so the SP can push images)
#   4. Container App `bronnoysund-web` (BlazorWeb host)    (provisioned with placeholder image)
#   5. Contributor role on both container apps             (so the SP can roll new images)
#   6. GitHub repo secrets                                 (AZURE_CLIENT_ID / TENANT_ID / SUBSCRIPTION_ID)
#
# Prereqs: az login + gh auth status, plus the existing resources from
# the prior Azure setup (resource group, ACR, Container Apps environment,
# bronnoysund-webapi). This script does not create the foundation; see
# the original azure-deploy walkthrough in the docs for that.

set -euo pipefail

readonly REPO="erlingsm/Bronnoysund.Lookup"
readonly RG="bronnoysund-lookup-rg"
readonly ACR="bronnoysundlookup30020"
readonly ENV="bronnoysund-env"
readonly APP_WEB="bronnoysund-web"
readonly APP_WEBAPI="bronnoysund-webapi"
readonly AD_APP_NAME="github-bronnoysund"
readonly PLACEHOLDER_IMAGE="mcr.microsoft.com/k8se/quickstart:latest"

echo "==> Sanity-check tooling"
command -v az >/dev/null || { echo "ERROR: az CLI not on PATH" >&2; exit 1; }
command -v gh >/dev/null || { echo "ERROR: gh CLI not on PATH (brew install gh)" >&2; exit 1; }
az account show --only-show-errors >/dev/null \
    || { echo "ERROR: 'az login' required" >&2; exit 1; }
gh auth status >/dev/null 2>&1 \
    || { echo "ERROR: 'gh auth login' required" >&2; exit 1; }

SUB_ID="$(az account show --query id -o tsv)"
TENANT_ID="$(az account show --query tenantId -o tsv)"
echo "    Subscription: $SUB_ID"
echo "    Tenant:       $TENANT_ID"

echo "==> 1/6 Azure AD App Registration"
# IMPORTANT: --filter "displayName eq" matches EXACTLY. The older --display-name
# parameter does a prefix match, which would silently reuse an app whose name is
# a prefix of $AD_APP_NAME — see the 2026-05-26 incident where it picked up
# github-bronnoysund-mvp instead of creating a fresh github-bronnoysund.
APP_ID="$(az ad app list --filter "displayName eq '$AD_APP_NAME'" --query '[0].appId' -o tsv)"
if [[ -z "$APP_ID" ]]; then
    APP_ID="$(az ad app create --display-name "$AD_APP_NAME" --query appId -o tsv)"
    echo "    Created app $AD_APP_NAME -> $APP_ID"
else
    echo "    Reusing existing app $AD_APP_NAME -> $APP_ID"
fi

# The Service Principal is what RBAC role assignments hang on. Creating it is
# a no-op if it already exists, but `az ad sp create` errors in that case —
# so probe first.
SP_OID="$(az ad sp list --filter "appId eq '$APP_ID'" --query '[0].id' -o tsv)"
if [[ -z "$SP_OID" ]]; then
    SP_OID="$(az ad sp create --id "$APP_ID" --query id -o tsv)"
    echo "    Created service principal $SP_OID"
else
    echo "    Reusing existing service principal $SP_OID"
fi

echo "==> 2/6 Federated credential (GitHub OIDC for $REPO master + workflow_dispatch)"
# One credential per OIDC subject. master covers push triggers; we add a second
# for workflow_dispatch from master so manual runs in the Actions tab also work.
for SUBJECT in "repo:${REPO}:ref:refs/heads/master" "repo:${REPO}:ref:refs/heads/main"; do
    CRED_NAME="github-$(echo "$SUBJECT" | tr ':/' '--')"
    EXISTING="$(az ad app federated-credential list --id "$APP_ID" \
                  --query "[?subject=='${SUBJECT}'].name" -o tsv)"
    if [[ -n "$EXISTING" ]]; then
        echo "    Skipping existing federated credential for $SUBJECT"
        continue
    fi
    az ad app federated-credential create --id "$APP_ID" --parameters "{
        \"name\": \"${CRED_NAME}\",
        \"issuer\": \"https://token.actions.githubusercontent.com\",
        \"subject\": \"${SUBJECT}\",
        \"audiences\": [\"api://AzureADTokenExchange\"]
    }" >/dev/null
    echo "    Added federated credential for $SUBJECT"
done

echo "==> 3/6 AcrPush on $ACR"
ACR_ID="$(az acr show --name "$ACR" --query id -o tsv)"
if az role assignment list --assignee "$APP_ID" --scope "$ACR_ID" \
       --query "[?roleDefinitionName=='AcrPush']" -o tsv | grep -q .; then
    echo "    AcrPush already assigned"
else
    az role assignment create --assignee "$APP_ID" --scope "$ACR_ID" --role "AcrPush" >/dev/null
    echo "    Granted AcrPush on $ACR"
fi

echo "==> 4/6 Provision Container App $APP_WEB (BlazorWeb)"
if az containerapp show --name "$APP_WEB" --resource-group "$RG" >/dev/null 2>&1; then
    echo "    $APP_WEB already exists — skipping create"
else
    # Pull credentials from the existing ACR (admin enabled in the original
    # bootstrap). This matches how bronnoysund-webapi is wired today; switch
    # to managed identity later if you want to retire the admin user.
    ACR_USERNAME="$(az acr credential show --name "$ACR" --query username -o tsv)"
    ACR_PASSWORD="$(az acr credential show --name "$ACR" --query 'passwords[0].value' -o tsv)"

    az containerapp create \
        --name "$APP_WEB" \
        --resource-group "$RG" \
        --environment "$ENV" \
        --image "$PLACEHOLDER_IMAGE" \
        --target-port 8080 --ingress external \
        --min-replicas 1 --max-replicas 1 \
        --cpu 0.5 --memory 1.0Gi \
        --registry-server "${ACR}.azurecr.io" \
        --registry-username "$ACR_USERNAME" \
        --registry-password "$ACR_PASSWORD" >/dev/null
    echo "    Created $APP_WEB (placeholder image — first deploy flips it to real)"
fi

WEB_FQDN="$(az containerapp show --name "$APP_WEB" --resource-group "$RG" \
            --query properties.configuration.ingress.fqdn -o tsv)"
echo "    URL: https://$WEB_FQDN/"

echo "==> 5/6 Contributor on both container apps"
for APP in "$APP_WEB" "$APP_WEBAPI"; do
    APP_ID_SCOPE="$(az containerapp show --name "$APP" --resource-group "$RG" --query id -o tsv)"
    if az role assignment list --assignee "$APP_ID" --scope "$APP_ID_SCOPE" \
           --query "[?roleDefinitionName=='Contributor']" -o tsv | grep -q .; then
        echo "    Contributor already on $APP"
    else
        az role assignment create --assignee "$APP_ID" --scope "$APP_ID_SCOPE" \
            --role "Contributor" >/dev/null
        echo "    Granted Contributor on $APP"
    fi
done

echo "==> 6/6 GitHub repo secrets on $REPO"
# Workflows read these via secrets.AZURE_* (not vars). Technically the three
# values aren't sensitive — they're OIDC identifiers, no client-secret is
# exchanged — but storing as secrets matches the convention most reviewers
# expect and lets the user audit them under Settings -> Secrets.
gh secret set AZURE_CLIENT_ID       --repo "$REPO" --body "$APP_ID"   >/dev/null
gh secret set AZURE_TENANT_ID       --repo "$REPO" --body "$TENANT_ID" >/dev/null
gh secret set AZURE_SUBSCRIPTION_ID --repo "$REPO" --body "$SUB_ID"   >/dev/null
echo "    Set AZURE_CLIENT_ID / AZURE_TENANT_ID / AZURE_SUBSCRIPTION_ID"

cat <<DONE

==> Done.

  - GitHub App Registration: $AD_APP_NAME ($APP_ID)
  - Web URL:                 https://$WEB_FQDN/
  - WebApi URL:              https://$(az containerapp show --name "$APP_WEBAPI" --resource-group "$RG" \
                                       --query properties.configuration.ingress.fqdn -o tsv)/health

Next:
  1. Test the new web pipeline:
       gh workflow run cd-web.yml --repo $REPO
       gh run watch  --repo $REPO
  2. Test the WebApi pipeline:
       gh workflow run cd-webapi.yml --repo $REPO
  3. Optional: turn on branch protection on master so CI must pass before
     merge — see deploy/README.md ("How to require an approved PR").
DONE
