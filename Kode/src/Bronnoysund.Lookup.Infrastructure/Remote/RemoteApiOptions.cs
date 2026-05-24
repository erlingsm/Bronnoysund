// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Remote;

/// <summary>
/// Konfigurasjon for å bytte fra Fat Client (direkte Brreg-kall) til Thin Client (kall
/// mot vår egen Web API som ligger i sky). Bindes mot "RemoteApi"-seksjonen i appsettings.
/// </summary>
public sealed class RemoteApiOptions
{
    public const string SectionName = "RemoteApi";

    /// <summary>Base-URL til vår Bronnoysund.Lookup.WebApi som ligger i sky.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public string UserAgent { get; set; } = "Bronnoysund.Lookup.RemoteClient/0.1";
}

/// <summary>
/// Velger om appen kjører som Fat Client (Direct = Brreg direkte) eller Thin Client (RemoteApi = mot sky).
/// Settes via "DataSource:Mode" i konfig.
/// </summary>
public enum DataSourceMode
{
    Direct = 0,    // Fat Client — kaller Brreg direkte
    RemoteApi = 1, // Thin Client — kaller vår Web API i sky
}
