// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;

namespace Bronnoysund.MauiDesktop;

/// <summary>
/// MAUI Desktop implementation of <see cref="IDeviceLayout"/>. On Mac Catalyst and Windows,
/// <c>DeviceInfo.Current.Idiom</c> returns <see cref="DeviceIdiom.Desktop"/>; we map that to
/// <see cref="DeviceLayoutKind.Desktop"/> and never raise <see cref="LayoutChanged"/>. The
/// tablet layout would also render correctly on desktop, but desktop users expect maximum
/// horizontal density and the desktop layout takes advantage of that.
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
