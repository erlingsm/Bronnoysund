# Watch ↔ Phone companion protocol — v1

Wire format for Watch apps (`BrregWatch` on watchOS, `BrregWear` on Wear OS)
talking to the phone host (`Bronnoysund.MauiMobile`).

The Watch is a thin client. It never hits Brreg or any international registry
directly. The phone is the backend: it owns DI, configuration, caching, and the
`LookupAggregatedCompanyHandler`.

Source of truth for field semantics:
[CompanyResponse.cs](../src/Bronnoysund.Application/Dtos/CompanyResponse.cs).

## Transport

| Platform | API | Path/topic |
| --- | --- | --- |
| watchOS ↔ iPhone | `WCSession.sendMessage(_:replyHandler:errorHandler:)` | n/a (single channel) |
| Wear OS ↔ Android | `MessageClient.sendMessage(nodeId, path, data)` | `/com.bronnoysund/lookup/v1` |

Both transports carry the same UTF-8 JSON payload as a byte array. Use the same
JSON shape on both platforms so the protocol stays one spec, not two.

JSON encoding rules:

- Lower-camelCase field names.
- `null` only where the schema marks a field optional. Optional fields may be
  omitted; recipients treat omitted and explicit `null` the same.
- No trailing commas. UTF-8 without BOM.
- Numbers as JSON numbers; strings as JSON strings. No dates in v1.

## Versioning

Every request and response carries `"version": 1`. Bump only on
backwards-incompatible changes. Adding optional fields does NOT bump.

If the phone receives an unknown `version`, it responds with
`{ "version": 1, "result": "invalid", "code": "unsupportedVersion", "message": "..." }`.
The Watch shows that message verbatim — no client-side fallback or downgrade.

## Request — Watch → Phone

```json
{ "version": 1, "action": "lookup", "value": "919300388" }
{ "version": 1, "action": "search", "value": "equinor" }
```

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `version` | integer | yes | Currently `1`. |
| `action` | string | yes | `"lookup"` for identifier, `"search"` for name. |
| `value` | string | yes | The user's spoken input after light normalisation (strip whitespace, lowercase only for `search`). The phone's `ICountryDetector` does the real parsing. |

V1 scope: `"search"` is reserved and returns
`{ "result": "invalid", "code": "notImplemented" }` until phase 2. Watch UI may
still expose a search button — the error is the explicit response, not a Watch
client check.

## Response — Phone → Watch

Four mutually exclusive shapes keyed on `result`.

### `found`

```json
{
  "version": 1,
  "result": "found",
  "organizationNumber": "919300388",
  "organizationName": "EQUINOR ASA",
  "companyType": "ASA",
  "languageForm": "Bokmål",
  "countryCode": "NO"
}
```

| Field | Type | Required | Source |
| --- | --- | --- | --- |
| `organizationNumber` | string | yes | `CompanyResponse.OrganizationNumber` |
| `organizationName` | string | yes | `CompanyResponse.OrganizationName` |
| `companyType` | string | yes | `CompanyResponse.CompanyType` |
| `languageForm` | string | yes | `CompanyResponse.LanguageForm` |
| `countryCode` | string | yes | ISO 3166-1 alpha-2. From `CompanyIdentifier.CountryCode`. Lets the Watch pick a flag icon. |

V1 deliberately omits the optional `CompanyResponse` fields (website, addresses,
industry, employees, bankruptcy, etc.) to keep the on-watch payload small.
Phase 2 adds them under a versioned extension.

### `notFound`

```json
{ "version": 1, "result": "notFound", "value": "919300388", "message": "..." }
```

| Field | Type | Required |
| --- | --- | --- |
| `value` | string | yes | The input the user gave, so the Watch can show "no hit for X". |
| `message` | string | yes | Human-readable, already localised on the phone. Watch displays verbatim. |

### `invalid`

```json
{ "version": 1, "result": "invalid", "code": "unrecognizedFormat", "message": "..." }
```

| Field | Type | Required |
| --- | --- | --- |
| `code` | string | yes | Stable identifier, see table below. |
| `message` | string | yes | Human-readable, localised on the phone. |

Defined codes:

| Code | Meaning |
| --- | --- |
| `unsupportedVersion` | Phone does not speak the requested `version`. |
| `unsupportedAction` | `action` is not `lookup` or `search`. |
| `notImplemented` | Action is known but not implemented in this protocol version (e.g. `search` in v1). |
| `unrecognizedFormat` | `ICountryDetector` could not classify the value. |
| `emptyValue` | `value` was empty or whitespace. |

### `unavailable`

```json
{ "version": 1, "result": "unavailable", "message": "..." }
```

Use when the upstream registry is reachable but errored, or the phone has no
network connectivity. The Watch shows the message and a retry affordance.

## Latency expectations

The phone target round-trips a `lookup` in well under 2s on a warm cache, and
under 6s cold (Brreg + downstream providers). Watch UI should show a spinner
after 200ms and a "still working" hint after 3s — no client-side timeout in
v1. Both transports retry queued messages once the channel reconnects.

## Disconnected / unpaired Watch

If the companion channel is unreachable when the Watch tries to send:

- watchOS: `WCSession.isReachable == false` or `sendMessage` errorHandler fires.
- Wear OS: `MessageClient.sendMessage` returns failed `Task`.

The Watch surfaces the user-visible message
"Telefonen er ikke tilgjengelig. Åpne appen på telefonen din." (or its English
equivalent — locale follows the Watch's system language) plus a retry button.
No payload is queued on the Watch in v1.

## Security

The Watch trusts the paired phone implicitly. Both Apple WatchConnectivity and
Wear OS Data Layer authenticate the pairing at the OS level — no extra token
exchange in v1. Do not log full `value` strings in production; they may include
personally identifiable input.

## Reference: source of the schema

- Schema fields map directly to
  [CompanyResponse.cs](../src/Bronnoysund.Application/Dtos/CompanyResponse.cs).
- Result types map directly to
  [`CompanyLookupResult`](../src/Bronnoysund.Application/Results/CompanyLookupResult.cs).
  V1 uses [`LookupCompanyHandler`](../src/Bronnoysund.Application/UseCases/LookupCompany/LookupCompanyHandler.cs) — not the aggregated variant.
  The aggregated handler does parallel fan-out (roles, sub-units, UBO) that the
  Watch does not need in v1.
- Plan reference: `Plan/06-Fase5-Watch-apper.md` (gitignored).

## Phone-side implementation notes (non-normative)

`CompanyResponse` does not carry `countryCode`. The host must derive it before
serialising: run
[`ICountryDetector.Detect(input)`](../src/Bronnoysund.Application/Ports/ICountryDetector.cs)
once on the raw `value`, take `CompanyIdentifier.CountryCode`, then merge it
into the JSON payload after the handler returns.

DI scope: `LookupCompanyHandler` is registered transient. Each incoming
companion message must open its own `IServiceScope` (or `CreateAsyncScope()`)
before resolving the handler, otherwise scoped ports added later would be
captured by the listener singleton.

## Change log

| Version | Date | Change |
| --- | --- | --- |
| 1 | 2026-05-29 | Initial. lookup + search-stub. |
