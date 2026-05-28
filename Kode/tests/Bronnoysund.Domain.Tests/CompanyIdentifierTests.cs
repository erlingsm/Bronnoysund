// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Bronnoysund.Domain;
using FluentAssertions;
using Xunit;

namespace Bronnoysund.Domain.Tests;

public class CompanyIdentifierTests
{
    [Fact]
    public void OrganizationNumber_IsACompanyIdentifier()
    {
        var org = OrganizationNumber.Create("919300388");

        org.Should().BeAssignableTo<CompanyIdentifier>();
        org.CountryCode.Should().Be("NO");
        org.Value.Should().Be("919300388");
    }

    [Fact]
    public void TwoOrganizationNumbers_WithSameValue_AreEqualAsCompanyIdentifiers()
    {
        CompanyIdentifier a = OrganizationNumber.Create("919300388");
        CompanyIdentifier b = OrganizationNumber.Create("919-300-388");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void DifferentSubtypes_AreNeverEqual_EvenWithSameValue()
    {
        // A future ForeignTestIdentifier with the same digit string must NOT collide with NO.
        CompanyIdentifier no = OrganizationNumber.Create("919300388");
        CompanyIdentifier foreign = new TestForeignIdentifier("FI", "919300388");

        no.Should().NotBe(foreign);
    }

    private sealed record TestForeignIdentifier(string Country, string IdValue)
        : CompanyIdentifier(Country, IdValue);
}
