// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Lookup.Application.Ports;

/// <summary>
/// Logical layout class derived from the physical screen size of the host. Razor components
/// branch on this to render a phone-stack vs. a tablet master-detail vs. a wide desktop layout
/// without depending on platform-specific APIs. Adapters resolve the value differently:
/// MAUI hosts read <c>DeviceInfo.Current.Idiom</c> once at startup; the Blazor Web host runs a
/// small JS-interop against <c>window.matchMedia</c> on first interactive render and on resize;
/// tests register a fixed value.
/// </summary>
public enum DeviceLayoutKind
{
    Phone,
    Tablet,
    Desktop,
}

/// <summary>
/// Port for resolving the current device's logical layout class. Implementations must be
/// inexpensive to call — Razor components may read <see cref="Current"/> on every render.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LayoutChanged"/> event lets adapters report runtime changes — primarily the
/// Web adapter, where the initial value is a safe default and is refined once a JS-interop call
/// returns, and also when the browser is resized (Stage Manager, freeform windowing, browser
/// drag-resize). MAUI adapters resolve the value once at startup and never fire the event.
/// </para>
/// <para>
/// Components that want to react to the event should subscribe in <c>OnInitialized</c>,
/// unsubscribe in <c>Dispose</c>, and call <c>InvokeAsync(StateHasChanged)</c> from the handler.
/// </para>
/// </remarks>
public interface IDeviceLayout
{
    DeviceLayoutKind Current { get; }

    event EventHandler? LayoutChanged;
}
