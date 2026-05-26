// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application.Ports;

namespace Bronnoysund.Lookup.MauiMobile;

/// <summary>
/// MAUI implementation of <see cref="IDeviceLayout"/>. Reads <c>DeviceInfo.Current.Idiom</c>
/// once at construction. The idiom is fixed for the lifetime of the process on iOS and Android
/// (Split View / Stage Manager / freeform windowing change the window size, not the idiom),
/// so the <see cref="LayoutChanged"/> event is never fired. A future iteration can hook
/// <c>Window.SizeChanged</c> to detect Stage Manager resizes inside Tablet mode.
/// </summary>
internal sealed class MauiDeviceLayout : IDeviceLayout
{
    public MauiDeviceLayout()
    {
        var idiom = DeviceInfo.Current.Idiom;
        Current = idiom == DeviceIdiom.Phone
            ? DeviceLayoutKind.Phone
            : idiom == DeviceIdiom.Tablet
                ? DeviceLayoutKind.Tablet
                : DeviceLayoutKind.Desktop;
    }

    public DeviceLayoutKind Current { get; }

#pragma warning disable CS0067
    public event EventHandler? LayoutChanged;
#pragma warning restore CS0067
}
