# Deploy til Azure Container Apps (Oslo / `norwayeast`)

Komplett steg-for-steg-guide for å sette opp Bronnoysund.Lookup.WebApi på Azure i Norge-regionen.

**Estimert tid:** 30–60 minutter første gang
**Estimert kostnad:** ~50–250 NOK/måned for lav trafikk (scale-to-zero)
**Forutsetning:** Microsoft-konto med tilgang til <https://portal.azure.com>

## Hva du kommer til å sette opp

| Komponent | Pris/måned | Hva det gjør |
| --- | --- | --- |
| **Azure-subscription** (Pay-As-You-Go) | 0 NOK (betaler per bruk) | Konto-/fakturerings-rammeverket |
| **Resource Group** | 0 NOK | Logisk container for alle ressurser |
| **Container Registry Basic** | ~50 NOK | Lagrer Docker-image-en din |
| **Container Apps Environment** | 0 NOK (inkludert i Container Apps-prisen) | Kjøremiljø |
| **Container App** | 0–200 NOK (free tier dekker mye) | Selve applikasjonen — scale-to-zero |
| **(valgfri) Log Analytics Workspace** | ~20–50 NOK | Sentral logging |
| **(valgfri) Custom domain + SSL** | 0 NOK (Azure-administrert) | F.eks. `bronnoysund.dittfirma.no` |

## Hva du må gjøre **én gang** for å komme i gang

### Trinn 1: Opprett Azure-subscription

Hvis du allerede har Microsoft-konto med Azure-tilgang, hopp til trinn 2.

1. Gå til <https://portal.azure.com>
2. Logg inn med Microsoft-kontoen din
3. Hvis du IKKE har subscription, vil Azure tilby:
    - **Free Trial** (12 måneder, $200 i kreditt) — bra for å komme i gang
    - **Pay-As-You-Go** — betaler kun for det du bruker
4. Velg subscription-type og fullfør registrering (kortinfo må gis selv om Free Trial er gratis)

### Trinn 2: Installer Azure CLI lokalt

Vi bruker Azure CLI for det meste — raskere enn klikking i portalen.

**Mac:**

```bash
brew install azure-cli
az --version
```

**Windows:**

```powershell
winget install -e --id Microsoft.AzureCLI
az --version
```

**Linux:**

```bash
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
az --version
```

### Trinn 3: Logg inn fra terminal

```bash
az login
```

En nettleser åpnes — logg inn med Microsoft-kontoen. Etterpå viser CLI hvilke subscription(s) du har:

```bash
az account list --output table
az account set --subscription "Din Subscription-id eller navn"
```

### Trinn 4: Installer Container Apps-utvidelse

```bash
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights
```

(Tar ~1 minutt.)

### Trinn 5: Opprett ressursene

Velg navn og region (vi bruker Norge):

```bash
# Variabler (endre RG_NAME, ACR_NAME osv. hvis du vil)
RG_NAME="bronnoysund-lookup-rg"
LOCATION="norwayeast"
ACR_NAME="bronnoysundlookup$RANDOM"   # ACR-navn må være globalt unikt
ENV_NAME="bronnoysund-env"
APP_NAME="bronnoysund-webapi"

# 1) Ressursgruppe (logisk container)
az group create --name $RG_NAME --location $LOCATION

# 2) Container Registry (lagrer Docker-image-en)
az acr create \
    --resource-group $RG_NAME \
    --name $ACR_NAME \
    --sku Basic \
    --admin-enabled true

# 3) Container Apps Environment (kjøremiljø)
az containerapp env create \
    --name $ENV_NAME \
    --resource-group $RG_NAME \
    --location $LOCATION
```

Sjekk:

```bash
az group show --name $RG_NAME --output table
az acr show --name $ACR_NAME --output table
```

### Trinn 6: Bygg og push container-image

Du har to alternativer:

#### A) Bygg i sky (ingen lokal Docker kreves)

```bash
cd Kode
az acr build \
    --registry $ACR_NAME \
    --image bronnoysund-webapi:v1 \
    --file src/Bronnoysund.Lookup.WebApi/Dockerfile \
    .
```

(Tar ~3–5 minutter. Bruker Azure-bygg-server.)

#### B) Bygg lokalt og push (krever Docker installert)

```bash
cd Kode
docker build -f src/Bronnoysund.Lookup.WebApi/Dockerfile -t bronnoysund-webapi:v1 .

# Logg inn på registry
az acr login --name $ACR_NAME

# Tag og push
docker tag bronnoysund-webapi:v1 $ACR_NAME.azurecr.io/bronnoysund-webapi:v1
docker push $ACR_NAME.azurecr.io/bronnoysund-webapi:v1
```

### Trinn 7: Deploy Container App

```bash
ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --query loginServer --output tsv)
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --query username --output tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query "passwords[0].value" --output tsv)

az containerapp create \
    --name $APP_NAME \
    --resource-group $RG_NAME \
    --environment $ENV_NAME \
    --image $ACR_LOGIN_SERVER/bronnoysund-webapi:v1 \
    --target-port 8080 \
    --ingress external \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_USERNAME \
    --registry-password $ACR_PASSWORD \
    --min-replicas 0 \
    --max-replicas 5 \
    --cpu 0.25 \
    --memory 0.5Gi \
    --env-vars \
        ASPNETCORE_ENVIRONMENT=Production \
        Brreg__BaseUrl=https://data.brreg.no/enhetsregisteret/api/ \
        Brreg__CacheTtl=1.00:00:00 \
        Brreg__UserAgent="Bronnoysund.Lookup/0.1 (+https://github.com/erlingsm/Bronnoysund.Lookup)"
```

Etter 1–2 minutter får du tilbake en URL:

```text
https://bronnoysund-webapi.kindgrass-12345abc.norwayeast.azurecontainerapps.io
```

### Trinn 8: Verifiser

```bash
APP_URL=$(az containerapp show --name $APP_NAME --resource-group $RG_NAME --query properties.configuration.ingress.fqdn --output tsv)

curl https://$APP_URL/health
curl https://$APP_URL/companies/974760843
```

Forventet respons:

```json
{ "status": "ok", "service": "Bronnoysund.Lookup.WebApi" }
{ "organizationNumber": "974760843", "organizationName": "RIKSREVISJONEN", "companyType": "ORGL", "languageForm": "Bokmål" }
```

✅ **Du er live på Azure!**

## Daglig drift

### Publiser ny versjon

Etter endringer i koden:

```bash
cd Kode

# Bygg ny versjon
az acr build --registry $ACR_NAME --image bronnoysund-webapi:v2 \
    --file src/Bronnoysund.Lookup.WebApi/Dockerfile .

# Oppdater Container App til ny versjon
az containerapp update \
    --name $APP_NAME \
    --resource-group $RG_NAME \
    --image $ACR_LOGIN_SERVER/bronnoysund-webapi:v2
```

Container Apps gjør **rolling update** automatisk — null nedetid.

### Se logger

```bash
az containerapp logs show \
    --name $APP_NAME \
    --resource-group $RG_NAME \
    --follow
```

### Endre konfigurasjon

```bash
az containerapp update \
    --name $APP_NAME \
    --resource-group $RG_NAME \
    --set-env-vars Brreg__CacheTtl=2.00:00:00
```

### Skaler

```bash
# Maks antall instanser (1-30)
az containerapp update --name $APP_NAME --resource-group $RG_NAME --max-replicas 10

# CPU/minne per instans
az containerapp update --name $APP_NAME --resource-group $RG_NAME --cpu 0.5 --memory 1.0Gi
```

## Custom domain + HTTPS (valgfritt)

For å bruke f.eks. `bronnoysund.dittfirma.no`:

1. Legg til CNAME-record i DNS:

   ```text
   bronnoysund.dittfirma.no  CNAME  bronnoysund-webapi.<env>.norwayeast.azurecontainerapps.io
   ```

2. Bekreft eierskap og legg til i Container App:

   ```bash
   az containerapp hostname add \
       --name $APP_NAME \
       --resource-group $RG_NAME \
       --hostname bronnoysund.dittfirma.no

   az containerapp hostname bind \
       --name $APP_NAME \
       --resource-group $RG_NAME \
       --hostname bronnoysund.dittfirma.no \
       --validation-method CNAME
   ```

Azure håndterer Let's Encrypt-sertifikatet automatisk. Det fornyes hvert 90. dag uten innblanding.

## Stoppe og slette ressurser

For å slå AV (pause faktureringen) uten å miste oppsettet:

```bash
az containerapp update --name $APP_NAME --resource-group $RG_NAME --min-replicas 0 --max-replicas 0
```

For å **slette alt** (uomstøttelig):

```bash
az group delete --name $RG_NAME --yes
```

Sletter ressursgruppen + alt innenfor på ett kall.

## Kostnads-monitorering

Sett opp budsjett-alert:

1. Gå til <https://portal.azure.com> → "Cost Management" → "Budgets"
2. Opprett et månedlig budsjett (f.eks. 300 NOK)
3. Sett alert ved 50 % og 90 % bruk

Du får e-post hvis utgiftene begynner å løpe løpsk.

## Vanlige feilsøkings-spørsmål

**Problem:** `Provider registration must be done before` — kjør `az provider register --namespace Microsoft.App` på nytt og vent et minutt.

**Problem:** "Operation returned an invalid status 'BadRequest'" — sjekk at `--target-port` er 8080 (vår Dockerfile lytter der).

**Problem:** App svarer ikke — sjekk logger med `az containerapp logs show ... --follow`.

**Problem:** `Image pull failed` — verifiser at `--registry-username` og `--registry-password` er riktig satt og at ACR-navnet matcher.

## Hva nå?

- **CI/CD med GitHub Actions:** Sett opp automatisk bygg + deploy fra `main`-branch. Se `docs/development/cicd-github-actions.md` (kommer).
- **Sentral database:** Når favoritter/historikk skal synkroniseres mellom enheter, legg til Azure Database for PostgreSQL Flexible Server (~250 NOK/mnd).
- **Application Insights:** For APM og strukturert logging, legg til `azureapplicationinsights` extension (~50 NOK/mnd).
- **Bytte til full AKS:** Hvis du vokser ut av Container Apps, samme Docker-image kan kjøre i full Kubernetes. Migrasjon er en helg-jobb, ikke en omskriving.
