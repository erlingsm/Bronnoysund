// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Persistence.Entities;

/// <summary>
/// Konfigurerbar URL per register (Brreg, Gjeldsregisteret, UK Companies House, ...).
/// Brukeren kan endre disse hvis API-er flyttes uten å oppdatere appen.
/// </summary>
public sealed class RegisterEndpoint
{
    public required string Name { get; init; }      // PK: "Brreg", "Gjeldsregister", "Regnskap", "UkCompaniesHouse", ...
    public required string BaseUrl { get; set; }
    public required bool IsEnabled { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
}
