// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Persistence;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public CacheOptions Cache { get; set; } = new();
    public HistoryOptions History { get; set; } = new();
    public MaintenanceOptions Maintenance { get; set; } = new();

    public sealed class CacheOptions
    {
        public int MaxSizeMB { get; set; } = 50;
        public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(24);
    }

    public sealed class HistoryOptions
    {
        public int RetentionDays { get; set; } = 30;
    }

    public sealed class MaintenanceOptions
    {
        public TimeSpan Interval { get; set; } = TimeSpan.FromHours(6);
    }
}
