// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Infrastructure.Denmark;
using Bronnoysund.Infrastructure.OpenCorporates;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

/// <summary>
/// Pins the per-country defaults that have empirical "do NOT change without verifying
/// against live endpoints" semantics. Two-prior reviews (iter-2 and iter-3) caught
/// regressions where a default was flipped to a sensible-looking but unreachable URL or
/// header scheme; these tests would have caught those slips immediately.
/// </summary>
public class InternationalOptionsDefaultsTests
{
    [Fact]
    public void DenmarkOptions_BaseUrl_IsHttp_NotHttps()
    {
        // distribution.virk.dk's three IPv4s time out on port 443 (verified 2026-05-29);
        // port 80 returns 200 OK. Switching the default to https:// has been attempted
        // twice (fd00284 + the silent-Edit-fail in 69ebc4b) and both times took down
        // every Danish lookup. Pin the http:// default so a third attempt fails CI.
        var defaults = new DenmarkOptions();
        defaults.BaseUrl.Should().StartWith("http://", because:
            "distribution.virk.dk does not serve TLS on port 443. Override in App Config " +
            "only if Erhvervsstyrelsen actually ships HTTPS in the future.");
    }

    [Fact]
    public void OpenCorporatesOptions_BaseUrl_IsHttps()
    {
        // api.opencorporates.com supports TLS and rejects HTTP.
        new OpenCorporatesOptions().BaseUrl.Should().StartWith("https://");
    }
}
