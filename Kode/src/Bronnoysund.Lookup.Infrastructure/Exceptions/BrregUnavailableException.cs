// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Infrastructure.Exceptions;

/// <summary>
/// Kastes når Brreg-API er midlertidig utilgjengelig — timeout, 5xx, circuit breaker open, osv.
/// </summary>
public sealed class BrregUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);
