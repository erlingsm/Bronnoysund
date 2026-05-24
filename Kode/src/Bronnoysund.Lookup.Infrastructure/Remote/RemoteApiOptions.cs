// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Remote;

/// <summary>
/// Configuration for switching from Fat Client (direct Brreg calls) to Thin Client (calls
/// against our own Web API hosted in the cloud). Bound to the "RemoteApi" section in appsettings.
/// </summary>
public sealed class RemoteApiOptions
{
    public const string SectionName = "RemoteApi";

    /// <summary>Base URL of our Bronnoysund.Lookup.WebApi hosted in the cloud.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public string UserAgent { get; set; } = "Bronnoysund.Lookup.RemoteClient/0.1";
}

/// <summary>
/// Selects whether the app runs as Fat Client (Direct = Brreg directly) or Thin Client (RemoteApi = against the cloud).
/// Set via "DataSource:Mode" in config.
/// </summary>
public enum DataSourceMode
{
    Direct = 0,    // Fat Client — calls Brreg directly
    RemoteApi = 1, // Thin Client — calls our Web API in the cloud
}
