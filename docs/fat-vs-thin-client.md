# Fat Client vs Thin Client — to moduser, én kodebase

Bronnoysund.Lookup leveres med **to driftsmoduser** som velges via konfigurasjon. Samme kompilerte applikasjon (Web, Desktop, Mobile) kan kjøre i hvilken som helst av dem — ingen rekompilering kreves.

## Den korte versjonen

| Modus | Hvor data kommer fra | Bruker-konto | Funksjonalitet | Når brukes |
| --- | --- | --- | --- | --- |
| **Fat Client** (default) | Brreg direkte fra enheten | Ikke nødvendig | Norsk Brreg-oppslag | Gratis app, App Store/Google Play default |
| **Thin Client** (Subscription) | Vår Web API i Azure-sky | Innlogging | Norsk Brreg **+** flere registre, flere land, synkronisering mellom enheter | Betalt subscription via in-app purchase |

**Begge bruker nøyaktig samme kompilerte binær.** Kun en konfigurasjons-fil avgjør hvilken modus appen kjører i.

## Den arkitektoniske mekanismen

Application-laget definerer porten:

```csharp
public interface ICompanyProvider
{
    Task<CompanyLookupResult> LookupAsync(OrganizationNumber org, CancellationToken ct);
}
```

Infrastructure har to adaptere som implementerer den:

- **`BrregCompanyProvider`** — kaller `https://data.brreg.no/enhetsregisteret/api/...` direkte
- **`RemoteApiCompanyProvider`** — kaller `https://<din-sky-deployment>.azurecontainerapps.io/companies/...`

DI-containeren velger riktig adapter basert på `DataSource:Mode` i konfigurasjon. Hele resten av applikasjonen (UI, ViewModels, caching, validering, logging) er uberørt av valget.

Dette er **Strategy-pattern** + **Ports & Adapters** i praksis. Begrunnelse i [docs/architecture.md](architecture.md).

## Konfigurasjon — slik bytter du modus

### Fat Client (default, brukes når du publiserer til App Store/Google Play)

`appsettings.json`:

```jsonc
{
  "DataSource": {
    "Mode": "Direct"           // <-- Fat Client
  },
  "Brreg": {
    "BaseUrl": "https://data.brreg.no/enhetsregisteret/api/",
    "RequestTimeout": "00:00:10",
    "CacheTtl": "1.00:00:00",
    "UserAgent": "Bronnoysund.Lookup/0.1 (+https://github.com/erlingsm/Bronnoysund.Lookup)"
  }
}
```

Når `Mode=Direct`:

- Brreg er den eneste eksterne tjenesten som kontaktes
- Cache ligger lokalt på enheten (HybridCache in-memory)
- Ingen brukerkonto kreves
- Fungerer offline for tidligere cachede oppslag

### Thin Client (Subscription-modus mot sky)

`appsettings.json`:

```jsonc
{
  "DataSource": {
    "Mode": "RemoteApi"        // <-- Thin Client
  },
  "RemoteApi": {
    "BaseUrl": "https://bronnoysund-webapi.<env>.norwayeast.azurecontainerapps.io/",
    "RequestTimeout": "00:00:10",
    "UserAgent": "Bronnoysund.Lookup.SubscriptionClient/0.1"
  }
}
```

Når `Mode=RemoteApi`:

- All trafikk går via vår sky (`Bronnoysund.Lookup.WebApi` på Azure)
- Skyen kaller Brreg + andre registre på vegne av brukeren
- Brukerkonto kreves for å aktivere subscription og logge inn
- Senere: synkronisering av favoritter mellom alle enheter brukeren er logget inn på

Mode-bytte ved kjøretid (når subscription kjøpes/utløper) kan gjøres via `IOptionsMonitor<RemoteApiOptions>` — uten å starte appen på nytt.

## Hva får man **bare** i Thin Client (Subscription)?

På sikt blir Subscription-versjonen rikere på funksjonalitet som krever sentralisert tilgang:

| Funksjon | Fat Client | Thin Client | Hvorfor sentralisert |
| --- | --- | --- | --- |
| Norsk Brreg-oppslag | ✅ | ✅ | Åpent API |
| Komprimert lokal cache | ✅ | ✅ | Lokalt i begge |
| Søkehistorikk | ✅ (lokal) | ✅ (synkronisert mellom enheter) | Krever bruker-konto |
| Favoritter | ✅ (lokal) | ✅ (synkronisert mellom enheter) | Krever bruker-konto |
| Brreg Roller (styret, daglig leder) | ✅ | ✅ | Åpent — kan være i begge |
| Brreg Underenheter | ✅ | ✅ | Åpent — kan være i begge |
| Brreg Regnskap (årsregnskap) | ❌ | ✅ | Krever Maskinporten-sertifikat — kan ikke distribueres til enheter |
| Reelle rettighetshavere (eiere) | ❌ | ✅ | Begrenset tilgang som krever juridisk grunnlag |
| Gjeldsregisteret | ❌ | ✅ | Kommersiell avtale + 2-veis TLS, sertifikat må ligge sikkert |
| UK Companies House | ❌ | ✅ (senere) | API-nøkkel deles ikke ut til hver enhet |
| Andre EU-land (FR, DE, IT, ES) | ❌ | ✅ (senere) | Samme grunn |
| Sentral logging (audit-spor) | ❌ | ✅ | Kreves for regulerte bransjer |
| API-rate-limiting på tvers av brukere | ❌ | ✅ | Sky-orkestrert |

**Forretningsmodellen:** Fat Client er det gratis tilbudet (dekker det opprinnelige MVP-løftet). Thin Client er det betalte tilbudet med rikere data og synkronisering — krever sky som har sentrale sertifikater og brukerkontoer.

## Hva som ligger i repoet og hva som distribueres

GitHub-repoet inneholder kode for **begge moduser**. Når du publiserer apper:

| Distribusjon | Konfigurasjon | Hva endrings-mekanismen er |
| --- | --- | --- |
| App Store iOS / TestFlight | `appsettings.json` med `Mode=Direct` | Standard build — bake inn `appsettings.json` |
| Google Play | `appsettings.json` med `Mode=Direct` | Samme — én bygg-pipeline |
| Subscription-aktivering i app | App leser ny konfig fra sky etter kjøp og bytter til `Mode=RemoteApi` | In-app purchase trigger + `IOptionsMonitor.OnChange` |
| Web Server (Azure Container Apps) | `appsettings.json` med `Mode=Direct` (WebApi snakker til Brreg direkte — det ER vår sky) | Sky-deployment via GitHub Actions |

## Bytte mellom moduser ved test

Kjør Fat Client lokalt:

```bash
cd Kode
dotnet run --project src/Bronnoysund.Lookup.WebApi --urls http://localhost:5099
curl http://localhost:5099/companies/974760843
# Logg viser: kall mot data.brreg.no direkte
```

Kjør Thin Client lokalt mot en annen lokal WebApi-instans:

```bash
# Terminal 1: kjør "sky"-versjon på port 5099
cd Kode
dotnet run --project src/Bronnoysund.Lookup.WebApi --urls http://localhost:5099 \
  --DataSource:Mode=Direct

# Terminal 2: kjør Blazor Web som Thin Client mot port 5099
dotnet run --project src/Bronnoysund.Lookup.BlazorWeb --urls http://localhost:5199 \
  --DataSource:Mode=RemoteApi \
  --RemoteApi:BaseUrl=http://localhost:5099/
# Logg viser: BlazorWeb kaller localhost:5099, som kaller data.brreg.no
```

Dette demonstrerer at samme binær funker som begge moduser — kun konfig endres.

## Når blir Thin Client tilgjengelig?

| Trinn | Status |
| --- | --- |
| Arkitektur klar (Strategy + DI) | ✅ Ferdig |
| `RemoteApiCompanyProvider` implementert | ✅ Ferdig |
| Web API i sky (Azure Container Apps) | ⏳ Krever at brukeren fullfører oppsett i `docs/install/azure-deploy.md` |
| In-app purchase-integrasjon (iOS + Android) | 📋 Planlagt etter MVP er live |
| Bruker-konto + innlogging | 📋 Planlagt etter sky-deploy |
| Synkronisering av favoritter mellom enheter | 📋 Planlagt når SQLite-Persistens (Plan 13) + bruker-konto er på plass |

Inntil videre er **Fat Client den eneste produksjonsklare modusen** og dekker MVP-leveransen.
