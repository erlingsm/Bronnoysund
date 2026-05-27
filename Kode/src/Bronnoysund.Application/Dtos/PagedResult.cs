// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

namespace Bronnoysund.Application.Dtos;

/// <summary>
/// Generic paged response from any source registry. <see cref="Page"/> is 0-based to match
/// Brreg's HAL response and Kiota's generated client; UI converts to 1-based for display.
/// <see cref="TotalElements"/> reflects the source registry's full result count so UI can
/// show "X of Y" and offer narrow-down hints when the page is the tip of a long tail.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalElements,
    int TotalPages);

/// <summary>
/// Request for a single page. Defaults match the MudBlazor pagination convention
/// (page 0, size 25). Callers should keep page size aligned with the
/// page-size-selector options in <c>Lookup.razor</c> + browse pages.
/// </summary>
public sealed record PagedRequest(int Page = 0, int PageSize = 25);
