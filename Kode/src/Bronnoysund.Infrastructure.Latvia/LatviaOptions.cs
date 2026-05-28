// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Latvia;

/// <summary>
/// Latvia (Uzņēmumu reģistrs CSV-dump) configuration. Latvia has no free live API for
/// per-orgnr lookups — the practical baseline is daily CSV-dump from dati.ur.gov.lv
/// imported into a local store (Plan 21/Latvia.md). Until the importer + index is in
/// place the adapter returns Unavailable.
/// </summary>
public sealed class LatviaOptions
{
    public const string SectionName = "Bronnoysund:International:Latvia";

    public string CsvUrl { get; set; } = "https://dati.ur.gov.lv/register/register.csv";

    /// <summary>Path to the locally-indexed snapshot once a bulk import is implemented;
    /// empty until then. The default Unavailable response references this setting.</summary>
    public string LocalIndexPath { get; set; } = string.Empty;

    public string UserAgent { get; set; } = "Bronnoysund/1.0 (+https://github.com/erlingsm/Bronnoysund)";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(LocalIndexPath);
}
