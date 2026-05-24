// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using System.Text.Json;
using Bronnoysund.Lookup.Application.Ports;

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Configuration;

internal sealed class SettingsBackup(ISettingsRepository settings) : ISettingsBackup
{
    private const int CurrentVersion = 1;
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public async Task<string> ExportAsync(CancellationToken ct)
    {
        var all = await settings.GetAllAsync(ct);
        var payload = new BackupPayload(
            Version: CurrentVersion,
            ExportedAt: DateTimeOffset.UtcNow,
            Settings: all.ToDictionary(s => s.Key, s => new BackupEntry(s.Value, s.DataType)));
        return JsonSerializer.Serialize(payload, JsonOpts);
    }

    public async Task ImportAsync(string json, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidSettingsBackupException("Backup-payload er tom.");

        BackupPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BackupPayload>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidSettingsBackupException($"Ugyldig JSON: {ex.Message}");
        }

        if (payload is null)
            throw new InvalidSettingsBackupException("Backup-payload er null etter deserialisering.");
        if (payload.Version != CurrentVersion)
            throw new InvalidSettingsBackupException(
                $"Ukjent backup-versjon {payload.Version}; støtter {CurrentVersion}.");
        if (payload.Settings is null)
            throw new InvalidSettingsBackupException("Backup mangler 'Settings'-felt.");

        foreach (var (key, entry) in payload.Settings)
        {
            await settings.SetAsync(key, entry.Value, entry.DataType, ct);
        }
    }

    private sealed record BackupPayload(
        int Version,
        DateTimeOffset ExportedAt,
        Dictionary<string, BackupEntry> Settings);

    private sealed record BackupEntry(string Value, string DataType);
}
