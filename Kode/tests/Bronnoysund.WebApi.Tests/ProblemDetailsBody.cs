// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.WebApi.Tests;

/// <summary>
/// Lower-case JSON shape (RFC 7807) that <see cref="Microsoft.AspNetCore.Http.Results.Problem"/> emits.
/// We declare it locally instead of taking a dependency on
/// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> so the test layer stays decoupled from MVC.
/// </summary>
#pragma warning disable IDE1006 // intentional lowercase to match RFC 7807 wire format
#pragma warning disable CA1707  // wire format uses lowercase
public sealed record ProblemDetailsBody(string? type, string? title, int? status, string? detail);
#pragma warning restore CA1707
#pragma warning restore IDE1006
