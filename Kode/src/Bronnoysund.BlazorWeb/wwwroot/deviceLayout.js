// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
//
// Tiny shim for the WebDeviceLayout adapter. We could in theory return the viewport size and
// let .NET do the bucketing, but doing the bucketing here keeps the IDeviceLayout contract
// stable and means future tweaks to the breakpoints only touch this file.
//
// Breakpoints mirror Plan 17:
//   <  768  -> Phone
//   768–1024 -> Tablet
//   > 1024  -> Desktop
//
// The Blazor side calls `init` once after the first interactive render. We do the initial
// measurement, push it back via DotNetObjectReference.invokeMethodAsync('OnLayoutChanged', ...)
// and subscribe to `resize` so Stage Manager / freeform / browser-drag changes flow through.

window.bronnoysundDeviceLayout = (function () {
    function classify(width) {
        if (width < 768) return 'Phone';
        if (width <= 1024) return 'Tablet';
        return 'Desktop';
    }

    function current() {
        return classify(window.innerWidth);
    }

    function subscribe(dotnetRef) {
        let last = current();
        dotnetRef.invokeMethodAsync('OnLayoutChanged', last);

        const handler = () => {
            const next = current();
            if (next !== last) {
                last = next;
                dotnetRef.invokeMethodAsync('OnLayoutChanged', next);
            }
        };
        window.addEventListener('resize', handler);

        // Return an object (not a bare function) so .NET can hold it via
        // IJSObjectReference and call .dispose() back on disposal.
        return {
            dispose: function () { window.removeEventListener('resize', handler); }
        };
    }

    return { current, subscribe };
})();
