// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Domain;

/// <summary>
/// Country-agnostic identifier for a company in some national registry. Concrete subclasses
/// (e.g. <see cref="OrganizationNumber"/>, FinnishBusinessId, EstonianRegistryCode) enforce
/// per-country format and checksum rules in their factory methods. <see cref="CountryCode"/>
/// is an ISO 3166-1 alpha-2 code so a router can pick the correct provider.
/// </summary>
public abstract record CompanyIdentifier(string CountryCode, string Value);
