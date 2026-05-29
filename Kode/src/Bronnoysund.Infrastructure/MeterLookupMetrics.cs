// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Diagnostics.Metrics;
using Bronnoysund.Application.Ports;

namespace Bronnoysund.Infrastructure;

/// <summary>
/// <see cref="ILookupMetrics"/> implementation backed by <see cref="Meter"/>. The same
/// counter + histogram are observable via the standard <c>System.Diagnostics.Metrics</c>
/// stack and exported as Prometheus text by the WebApi's
/// <c>OpenTelemetry.Exporter.Prometheus.AspNetCore</c> registration.
/// </summary>
internal sealed class MeterLookupMetrics : ILookupMetrics, IDisposable
{
    public const string MeterName = "Bronnoysund.Lookup";

    private readonly Meter _meter;
    private readonly Counter<long> _lookupTotal;
    private readonly Histogram<double> _lookupDuration;

    public MeterLookupMetrics()
    {
        _meter = new Meter(MeterName, version: "1.0.0");
        _lookupTotal = _meter.CreateCounter<long>(
            name: "bronnoysund_lookup_total",
            unit: "lookups",
            description: "Total number of company-identifier lookups, tagged by country and result kind.");
        _lookupDuration = _meter.CreateHistogram<double>(
            name: "bronnoysund_lookup_duration_seconds",
            unit: "s",
            description: "Wall-clock duration of company-identifier lookups, tagged by country.");
    }

    public void Record(string countryCode, LookupResultKind resultKind, TimeSpan duration)
    {
        var countryTag = new KeyValuePair<string, object?>("country", countryCode.ToUpperInvariant());
        var resultTag = new KeyValuePair<string, object?>("result", resultKind.ToString());
        _lookupTotal.Add(1, countryTag, resultTag);
        _lookupDuration.Record(duration.TotalSeconds, countryTag);
    }

    public void Dispose() => _meter.Dispose();
}
