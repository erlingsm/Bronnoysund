# Generated Brreg client — DO NOT EDIT

Hver fil under `Generated/` er produsert av Microsoft Kiota fra
Brønnøysundregistrenes offisielle OpenAPI 3-spec. Håndredigeringer
overlever ikke neste regenerering.

## Regenerering

```bash
./regenerate.sh
```

Scriptet installerer Kiota CLI hvis nødvendig, henter spec'en på nytt,
sletter eksisterende `Generated/`-mappe og skriver alt på nytt. Sjekk
inn diff-en etterpå — det er DER du ser hva Brreg faktisk har endret
siden sist.

## Drift-check (uten å regenerere)

```bash
./regenerate.sh --check
```

Sammenligner committed `kiota-lock.json` med live spec. Brukes av
`.github/workflows/spec-drift.yml` for nattlig sjekk.

## Når regenerere

- Brreg har annonsert API-endring (følg <https://brreg.github.io/docs/>)
- CI-en `spec-drift`-jobb varslet at live spec ≠ committed lock
- Du skal aktivere et endepunkt vi ikke har eksponert ennå
- Det er ≥ 1 måned siden siste regenerering — gjør det proaktivt
  før noe brytende treffer prod

## Når IKKE regenerere

- Som "kanskje får jeg det til å bli pent"-tiltak. Adapter-laget i
  `Bronnoysund.Infrastructure/Brreg/Adapters/` er der hvor du
  reagerer på endringer — generert kode er kontrakten.
- Når du har en uskjekket sammenheng mellom kompileringsfeil og
  Brreg-endring. Les diff-en først.

## Hvordan kode bruker dette

Direkte bruk skal kun skje fra `Bronnoysund.Infrastructure`. Andre lag
(Application, ViewModels, UI) snakker via Application-Ports og
adapter-laget i Infrastructure — aldri direkte mot generert klient.

Se [Plan 50-55](../../../Plan/) for full kontekst.
