// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;

namespace Bronnoysund.Application;

/// <summary>
/// Fallback <see cref="IDeviceLayout"/> implementation registered by
/// <see cref="ServiceCollectionExtensions.AddBronnoysundApplication"/> via <c>TryAdd</c>.
/// Hosts that can do better (MAUI via <c>DeviceInfo.Idiom</c>, Blazor Web via
/// <c>matchMedia</c> JS-interop) register their own implementation up front and win the
/// container slot. Defaulting to <see cref="DeviceLayoutKind.Desktop"/> is safe — phone-only
/// rendering paths add visible UI (e.g. drill-down navigation) that would feel wrong on a
/// large screen if accidentally triggered.
/// </summary>
internal sealed class DesktopDeviceLayout : IDeviceLayout
{
    public DeviceLayoutKind Current => DeviceLayoutKind.Desktop;

    // Layout cannot change at runtime for this implementation; declared to satisfy the
    // interface contract. Subscribers can still attach without issue — the event simply
    // never fires.
#pragma warning disable CS0067
    public event EventHandler? LayoutChanged;
#pragma warning restore CS0067
}
