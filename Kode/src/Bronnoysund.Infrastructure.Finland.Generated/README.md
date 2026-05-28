# Generated PRH (Finland) client — DO NOT EDIT

Hver fil under `Generated/` er produsert av Microsoft Kiota fra Patentti-
ja rekisterihallitus (PRH) sin offisielle OpenAPI 3-spec for YTJ-API v3.
Håndredigeringer overlever ikke neste regenerering.

## Regenerering

```bash
./regenerate.sh
```

Speil av mønsteret fra `Bronnoysund.Infrastructure.Brreg.Generated`:
installerer Kiota CLI hvis nødvendig, henter spec'en på nytt, sletter
eksisterende `Generated/`-mappe og skriver alt på nytt. Sjekk inn diff-en
etterpå — det er DER du ser hva PRH faktisk har endret siden sist.

## Drift-check (uten å regenerere)

```bash
./regenerate.sh --check
```

Sammenligner committed `kiota-lock.json` med live spec.

## Spec-detaljer

- URL: <https://avoindata.prh.fi/opendata-ytj-api/v3/schema?lang=en>
- Base URL (fra spec): `https://avoindata.prh.fi/opendata-ytj-api/v3`
- Endepunkter (4): `/companies`, `/all_companies`, `/description`, `/post_codes`
- Autentisering: ingen
- Lisens: CC-BY 4.0 — "PRH og Verohallinto" må krediteres

## Hvordan kode bruker dette

Direkte bruk skal kun skje fra `Bronnoysund.Infrastructure.Finland`. Andre
lag (Application, ViewModels, UI) snakker via Application-Ports
(`ICompanyProvider`) — aldri direkte mot generert klient.
