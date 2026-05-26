// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Infrastructure.Persistence.Entities;

/// <summary>
/// Configurable URL per registry (Brreg, Gjeldsregisteret, UK Companies House, ...).
/// The user can change these if APIs move, without updating the app.
/// </summary>
public sealed class RegisterEndpoint
{
    public required string Name { get; init; }      // PK: "Brreg", "Gjeldsregister", "Regnskap", "UkCompaniesHouse", ...
    public required string BaseUrl { get; set; }
    public required bool IsEnabled { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
}
