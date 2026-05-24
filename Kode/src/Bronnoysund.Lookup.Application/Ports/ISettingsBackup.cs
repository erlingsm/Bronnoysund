// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Ports;

/// <summary>
/// Eksport/import av brukerinnstillinger for å flytte mellom enheter.
/// JSON-formatet er versjonert så fremtidige skjema-endringer kan migreres.
/// </summary>
public interface ISettingsBackup
{
    /// <summary>Returnerer alle settings som JSON-payload klar for fil-lagring.</summary>
    Task<string> ExportAsync(CancellationToken ct);

    /// <summary>
    /// Leser payload og upserter alle nøkler. Eksisterende nøkler som ikke nevnes i payload
    /// beholdes (merge). Kaster <see cref="InvalidSettingsBackupException"/> ved ugyldig JSON
    /// eller ukjent versjon.
    /// </summary>
    Task ImportAsync(string json, CancellationToken ct);
}

public sealed class InvalidSettingsBackupException(string message) : Exception(message);
