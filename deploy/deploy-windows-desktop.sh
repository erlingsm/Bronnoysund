#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
#
# Local manual MSIX build of Bronnoysund.MauiDesktop for Microsoft Store.
# Mirrors .github/workflows/cd-windows-desktop.yml, which is disabled by
# default — no Partner Center account yet. This script is also a stub:
# it runs on Windows (or on macOS/Linux with --pwsh forwarding), produces
# an unsigned MSIX, and tells you what to wire next.

set -euo pipefail

readonly TFM="net10.0-windows10.0.19041.0"
readonly PROJECT="src/Bronnoysund.MauiDesktop/Bronnoysund.MauiDesktop.csproj"

cd "$(dirname "$0")/../Kode"

if [[ "$(uname -s)" != "MINGW"* && "$(uname -s)" != "CYGWIN"* && "$(uname -s)" != "MSYS"* ]]; then
    cat >&2 <<'NOTE'
NOTE: Microsoft Store MSIX builds require Windows. This script is the
      bash-side documentation; the workflow cd-windows-desktop.yml runs
      on the windows-2022 GitHub runner. Run from a Windows shell:

          dotnet publish src/Bronnoysund.MauiDesktop ^
              -c Release -f net10.0-windows10.0.19041.0 ^
              -p:RuntimeIdentifier=win10-x64 ^
              -p:WindowsPackageType=MSIX ^
              -o .\.dist\windows

      Microsoft Partner Center account is also required before any
      upload step works.
NOTE
    exit 0
fi

dotnet workload install maui-windows >/dev/null 2>&1 || true

dotnet publish "$PROJECT" \
    -c Release \
    -f "$TFM" \
    -p:RuntimeIdentifier=win10-x64 \
    -p:WindowsPackageType=MSIX \
    -p:GenerateAppxPackageOnBuild=true \
    -o ./.dist/windows

echo "==> MSIX built. Output: Kode/.dist/windows/"
echo "    Submission via Partner Center is stubbed — see cd-windows-desktop.yml."
