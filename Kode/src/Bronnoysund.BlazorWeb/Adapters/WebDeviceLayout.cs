// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Microsoft.JSInterop;

namespace Bronnoysund.BlazorWeb.Adapters;

/// <summary>
/// Blazor Server adapter for <see cref="IDeviceLayout"/>. Registered as scoped — Blazor Server
/// gives each circuit (browser tab) its own DI scope, and we want layout state per tab so two
/// tabs at different sizes don't trample each other.
/// </summary>
/// <remarks>
/// <para>
/// The adapter starts with <see cref="DeviceLayoutKind.Desktop"/> because the very first server
/// render happens before any JS has executed; once the page becomes interactive,
/// <see cref="DeviceLayoutInitializer"/> calls <see cref="InitializeAsync"/>, which subscribes
/// the JS shim and pushes the real value back via <see cref="OnLayoutChanged(string)"/>.
/// </para>
/// <para>
/// Resize events fire the same callback. The adapter raises <see cref="LayoutChanged"/> only
/// when the bucket actually changes, so subscribing components don't re-render on every pixel
/// of a drag.
/// </para>
/// </remarks>
public sealed class WebDeviceLayout : IDeviceLayout, IAsyncDisposable
{
    private DotNetObjectReference<WebDeviceLayout>? _dotnetRef;
    private IJSObjectReference? _unsubscribe;

    public DeviceLayoutKind Current { get; private set; } = DeviceLayoutKind.Desktop;

    public event EventHandler? LayoutChanged;

    public async Task InitializeAsync(IJSRuntime js, CancellationToken ct = default)
    {
        if (_dotnetRef is not null)
        {
            return;
        }

        _dotnetRef = DotNetObjectReference.Create(this);
        // The JS shim returns a function reference (its unsubscribe handle). We hold it for
        // disposal so that circuit-tear-down doesn't leak the resize listener.
        _unsubscribe = await js.InvokeAsync<IJSObjectReference>(
            "bronnoysundDeviceLayout.subscribe", ct, _dotnetRef).ConfigureAwait(false);
    }

    [JSInvokable]
    public Task OnLayoutChanged(string kindName)
    {
        var next = kindName switch
        {
            "Phone" => DeviceLayoutKind.Phone,
            "Tablet" => DeviceLayoutKind.Tablet,
            _ => DeviceLayoutKind.Desktop,
        };

        if (next != Current)
        {
            Current = next;
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_unsubscribe is not null)
        {
            try
            {
                // Best-effort: when the circuit is disconnected the JS runtime may already be
                // unavailable. Swallow that specific case rather than failing the disposal.
                await _unsubscribe.InvokeVoidAsync("dispose").ConfigureAwait(false);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            await _unsubscribe.DisposeAsync().ConfigureAwait(false);
            _unsubscribe = null;
        }

        _dotnetRef?.Dispose();
        _dotnetRef = null;
    }
}
