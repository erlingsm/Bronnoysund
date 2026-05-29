// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Telemetry sink for company-identifier lookups (Plan 26, Fase 4). Each handler
/// records the outcome class + country + wall-clock duration once per lookup; the
/// implementation in Infrastructure surfaces these as Prometheus
/// <c>bronnoysund_lookup_total{country,result}</c> and
/// <c>bronnoysund_lookup_duration_seconds{country}</c>.
/// </summary>
public interface ILookupMetrics
{
    void Record(string countryCode, LookupResultKind resultKind, TimeSpan duration);
}

public enum LookupResultKind
{
    Found,
    NotFound,
    InvalidInput,
    Unavailable,
}
