// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.UseCases.LookupCompany;

/// <summary>Forespørsel om å slå opp et selskap basert på et orgnr-input (rå streng).</summary>
public sealed record LookupCompanyQuery(string OrganizationNumberInput);
