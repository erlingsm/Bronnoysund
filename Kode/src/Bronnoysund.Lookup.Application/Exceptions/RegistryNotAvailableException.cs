// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Exceptions;

/// <summary>
/// Kastes av en register-provider når funksjonen ikke er tilgjengelig — enten fordi tilgang
/// ikke er på plass (Maskinporten, AML-grunnlag, kommersiell avtale) eller fordi det er en
/// stub som ikke er implementert ennå.
/// </summary>
public sealed class RegistryNotAvailableException(string registryName, string reason)
    : Exception($"Register '{registryName}' er ikke tilgjengelig: {reason}")
{
    public string RegistryName { get; } = registryName;
    public string Reason { get; } = reason;
}
