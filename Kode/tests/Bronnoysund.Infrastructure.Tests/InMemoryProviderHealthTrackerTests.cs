// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Application.Ports;
using Bronnoysund.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Infrastructure.Tests;

public class InMemoryProviderHealthTrackerTests
{
    [Fact]
    public void Snapshot_is_empty_when_nothing_recorded()
    {
        var sut = new InMemoryProviderHealthTracker();
        sut.Snapshot.Should().BeEmpty();
    }

    [Fact]
    public void RecordSuccess_stamps_LastSuccess_and_marks_country_healthy()
    {
        var sut = new InMemoryProviderHealthTracker();
        var before = DateTimeOffset.UtcNow;

        sut.RecordSuccess("NO");

        sut.Snapshot.Should().ContainKey("NO");
        var snap = sut.Snapshot["NO"];
        snap.LastSuccess.Should().NotBeNull().And.BeOnOrAfter(before);
        snap.LastFailure.Should().BeNull();
        snap.IsFailingNow.Should().BeFalse();
    }

    [Fact]
    public void RecordFailure_stamps_LastFailure_and_keeps_reason()
    {
        var sut = new InMemoryProviderHealthTracker();
        sut.RecordFailure("SE", "Bolagsverket: 401 Unauthorized");
        var snap = sut.Snapshot["SE"];
        snap.LastFailure.Should().NotBeNull();
        snap.LastFailureReason.Should().Be("Bolagsverket: 401 Unauthorized");
        snap.IsFailingNow.Should().BeTrue();
    }

    [Fact]
    public void Failure_then_success_flips_back_to_healthy()
    {
        var sut = new InMemoryProviderHealthTracker();
        sut.RecordFailure("DK", "timeout");
        Thread.Sleep(2); // Ensure later timestamp resolves strictly greater than earlier one.
        sut.RecordSuccess("DK");
        sut.Snapshot["DK"].IsFailingNow.Should().BeFalse();
    }

    [Fact]
    public void Success_then_failure_marks_failing_again()
    {
        var sut = new InMemoryProviderHealthTracker();
        sut.RecordSuccess("FI");
        Thread.Sleep(2); // Ensure later timestamp resolves strictly greater than earlier one.
        sut.RecordFailure("FI", "500");
        sut.Snapshot["FI"].IsFailingNow.Should().BeTrue();
    }

    [Fact]
    public void Country_codes_are_case_insensitive()
    {
        var sut = new InMemoryProviderHealthTracker();
        sut.RecordSuccess("NO");
        sut.RecordFailure("no", "downstream");
        // Same logical country — second write should land on the same entry.
        sut.Snapshot.Should().HaveCount(1);
        sut.Snapshot["NO"].IsFailingNow.Should().BeTrue();
    }
}
