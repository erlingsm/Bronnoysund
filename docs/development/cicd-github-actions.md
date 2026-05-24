# GitHub Actions CI/CD

To workflows er konfigurert i `.github/workflows/`:

| Workflow | Trigger | Hva den gjør | Krever secrets |
| --- | --- | --- | --- |
| `build-and-test.yml` | Hver push + PR til master/main | Bygger alle .NET-prosjekter og kjører alle 47 tester | ❌ Ingen — kjører gratis |
| `deploy-azure.yml` | Hver push til master/main (kun WebApi-relaterte endringer) | Bygger Docker-image i Azure Container Registry, ruller ut ny versjon til Container Apps med rolling update, smoke-tester `/health` | ✅ 4 secrets fra Azure |

## Build-and-test (fungerer umiddelbart)

Push noe til master/main → GitHub kjører automatisk på en Ubuntu-runner:

1. Checkout
2. Setup .NET 10
3. Restore + Build + Test (`Core.slnf` + `Web.slnf`)
4. Last opp test-results som artifact (beholdes i 30 dager)

Ingen oppsett nødvendig — kjører ut av boksen.

**Se status:** <https://github.com/erlingsm/Bronnoysund.Lookup/actions>

## Deploy-azure (krever oppsett først)

Forutsetning: Du har fulgt `docs/install/azure-deploy.md` minst én gang og har Container App live.

### Engangs-oppsett: Service Principal + GitHub Secrets

GitHub trenger en "konto" hos Azure som kan deploye. Den heter Service Principal (SP).

1. **Opprett Service Principal** (kjør lokalt med din innloggede `az`-CLI):

   ```bash
   SUBSCRIPTION_ID=$(az account show --query id --output tsv)
   RG_NAME="bronnoysund-lookup-rg"

   az ad sp create-for-rbac \
       --name "github-actions-bronnoysund-lookup" \
       --role contributor \
       --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RG_NAME" \
       --sdk-auth
   ```

   Output er en JSON som ser slik ut:

   ```json
   {
     "clientId": "...",
     "clientSecret": "...",
     "subscriptionId": "...",
     "tenantId": "...",
     ...
   }
   ```

   **Kopier hele JSON-en** — du trenger den i neste steg. (Den vises kun én gang!)

2. **Sett GitHub Secrets** (alle 4 må settes — du trenger dette én gang per repo):

   ```bash
   # Lim inn JSON fra steg 1 når gh ber om input
   gh secret set AZURE_CREDENTIALS

   gh secret set AZURE_RESOURCE_GROUP --body "bronnoysund-lookup-rg"
   gh secret set AZURE_CONTAINER_REGISTRY --body "bronnoysundlookup<ditt-nummer>"
   gh secret set AZURE_CONTAINER_APP_NAME --body "bronnoysund-webapi"
   ```

   Sjekk at de er satt:

   ```bash
   gh secret list
   ```

   Du skal se 4 secrets med "Updated" timestamp.

3. **Test workflow-en manuelt** før du stoler på den:

   ```bash
   gh workflow run deploy-azure.yml
   gh run watch  # følger live-output
   ```

   Eller via GitHub-UI: Actions → "Deploy to Azure Container Apps" → "Run workflow".

   Hvis alt går bra ser du i loggen:

   ```text
   ✅ Deployed to: https://bronnoysund-webapi.kindgrass-12345.norwayeast.azurecontainerapps.io
   ```

   Og smoke-testen sjekker at `/health` returnerer `ok`.

### Daglig flyt

Etter oppsett: hver gang du pusher kode til master som rører `Kode/src/Bronnoysund.Lookup.WebApi`, `Application`, `Infrastructure` eller `Domain`:

1. GitHub Actions trigges automatisk
2. ~3–5 min senere er ny versjon live i Azure
3. Smoke-test bekrefter `/health` OK

**Eksplisitt deploy uten kode-endring:** trigger manuelt via Actions-tab eller `gh workflow run deploy-azure.yml`.

### Image-tagging

Hvert deploy lager to tags:

- `latest` (samme som siste deploy)
- `${{ github.sha }}` (commit-SHA, f.eks. `90873c7...`)

For å rulle tilbake til en eldre versjon:

```bash
az containerapp update \
    --name bronnoysund-webapi \
    --resource-group bronnoysund-lookup-rg \
    --image <ACR_NAVN>.azurecr.io/bronnoysund-webapi:<eldre-sha>
```

Container Apps gjør rolling update tilbake — fortsatt null nedetid.

### Hvis deploy feiler

Workflow-en stopper hvis:

- ACR build feiler (sjekk Dockerfile lokalt med `docker build`)
- Container App ikke finnes (ikke kjørt `docs/install/azure-deploy.md`-oppsettet ennå)
- Smoke-test feiler (sjekk Container App-logger med `az containerapp logs show ...`)

Forrige versjon **forblir kjørende** — rolling update ruller bare tilbake hvis ny versjon ikke probe-er OK.

## Hva som IKKE er konfigurert (planlagte utvidelser)

- **Pull request preview-deploys** (en egen Container App per PR) — fint å ha senere
- **Staging-environment** før prod — krever en ekstra Container App
- **Slack/Discord-notifikasjon ved deploy** — kort tillegg
- **Container vulnerability scanning** (`microsoft/container-scan-action`) — bør legges til før produksjon med eksterne brukere

## Kostnader

- **build-and-test.yml**: GitHub Actions er gratis for offentlige repos. For private: 2000 min/mnd inkludert i Free-plan, deretter $0.008/min.
- **deploy-azure.yml**: Selve workflow-kjøringen er gratis (ubuntu-latest 5-10 min per deploy). Azure-kostnadene er de samme som hvis du gjør deploy manuelt (ingen ekstra cost per workflow-run).

Estimat: 0 NOK ekstra for CI/CD med vårt bruks-mønster.
