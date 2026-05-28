// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Domain;

namespace Bronnoysund.Application.Ports;

/// <summary>
/// Tries to recognise which national registry a raw user-typed identifier belongs to,
/// returning a fully-validated <see cref="CompanyIdentifier"/> on success. Implementations
/// iterate the known per-country format checkers (e.g. Norwegian 9-digit MOD11, Finnish
/// NNNNNNN-N) and return the first match. <c>null</c> means no registered country
/// pattern accepted the input.
/// </summary>
public interface ICountryDetector
{
    CompanyIdentifier? Detect(string? rawInput);
}
