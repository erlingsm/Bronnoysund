// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

using Xunit;

// WebApplicationFactory<Program> resolves the host via HostFactoryResolver, which patches
// the entry assembly's Main and listens for IHostApplicationLifetime callbacks. Running
// multiple factory instances in parallel against the same Program assembly races on the
// static hooking infrastructure, which surfaces as "The server has not been started" or
// "The entry point exited without ever building an IHost". Serialising the test assembly
// is the recommended workaround documented in the AspNet team's testing guidance.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
