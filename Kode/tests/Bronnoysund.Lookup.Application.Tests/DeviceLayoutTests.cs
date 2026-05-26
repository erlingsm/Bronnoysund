// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Lookup.Application;
using Bronnoysund.Lookup.Application.Ports;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Bronnoysund.Lookup.Application.Tests;

public class DeviceLayoutTests
{
    [Fact]
    public void Fallback_resolves_to_desktop_when_no_host_adapter_registered()
    {
        var services = new ServiceCollection();
        services.AddBronnoysundApplication();

        using var provider = services.BuildServiceProvider();
        var layout = provider.GetRequiredService<IDeviceLayout>();

        layout.Current.Should().Be(DeviceLayoutKind.Desktop);
    }

    [Fact]
    public void Host_registration_wins_over_application_fallback()
    {
        var services = new ServiceCollection();
        services.AddBronnoysundApplication();

        // Host registers its adapter AFTER AddBronnoysundApplication — last-wins for
        // IEnumerable resolution and for the default GetRequiredService path.
        services.AddSingleton<IDeviceLayout>(new StubDeviceLayout(DeviceLayoutKind.Tablet));

        using var provider = services.BuildServiceProvider();
        var layout = provider.GetRequiredService<IDeviceLayout>();

        layout.Current.Should().Be(DeviceLayoutKind.Tablet);
    }

    [Fact]
    public void Fallback_layout_changed_event_never_fires()
    {
        var services = new ServiceCollection();
        services.AddBronnoysundApplication();

        using var provider = services.BuildServiceProvider();
        var layout = provider.GetRequiredService<IDeviceLayout>();

        var fired = false;
        layout.LayoutChanged += (_, _) => fired = true;

        // The fallback has no mechanism to change at runtime; we just assert the contract
        // is wired up (subscription doesn't throw) and that no spurious event arrives.
        fired.Should().BeFalse();
    }

    private sealed class StubDeviceLayout(DeviceLayoutKind kind) : IDeviceLayout
    {
        public DeviceLayoutKind Current { get; } = kind;
#pragma warning disable CS0067
        public event EventHandler? LayoutChanged;
#pragma warning restore CS0067
    }
}
