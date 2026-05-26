// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Exceptions;

/// <summary>
/// Thrown by a registry provider when the function is not available — either because access
/// is not in place (Maskinporten, AML grounds, commercial agreement) or because it is a
/// stub that is not implemented yet.
/// </summary>
public sealed class RegistryNotAvailableException(string registryName, string reason)
    : Exception($"Registry '{registryName}' is not available: {reason}")
{
    public string RegistryName { get; } = registryName;
    public string Reason { get; } = reason;
}
