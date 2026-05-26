#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
#
# Local manual build of Bronnoysund.MauiDesktop for Mac App Store. Mirrors
# .github/workflows/cd-mac-desktop.yml. Only the unsigned .app build is
# wired today; signing + altool upload are stubbed exactly like the
# workflow until Apple Developer signing material is in place.
#
# Usage:
#   ./deploy/deploy-mac-desktop.sh           # build only (today)
#   ./deploy/deploy-mac-desktop.sh --upload  # build + sign + upload (when wired)

set -euo pipefail

readonly TFM="net10.0-maccatalyst"
readonly PROJECT="src/Bronnoysund.MauiDesktop/Bronnoysund.MauiDesktop.csproj"
readonly RID="maccatalyst-arm64"

UPLOAD=0
[[ "${1:-}" == "--upload" ]] && UPLOAD=1

if [[ "$(uname)" != "Darwin" ]]; then
    echo "ERROR: Mac App Store builds require macOS." >&2
    exit 1
fi

cd "$(dirname "$0")/../Kode"

echo "==> Ensuring MAUI workload"
dotnet workload install maui >/dev/null 2>&1 || true

echo "==> Publish (Release, $RID)"
dotnet publish "$PROJECT" \
    -c Release \
    -f "$TFM" \
    -p:RuntimeIdentifier="$RID" \
    -p:CreatePackage=true \
    -o ./.dist/mac

if [[ $UPLOAD -eq 0 ]]; then
    echo "==> Build only. Output: Kode/.dist/mac/"
    echo "    Pass --upload once signing + ASC API key are wired."
    exit 0
fi

cat >&2 <<'STUB'
==> Upload step is stubbed.
    To complete the Mac App Store path locally:
      1. xcrun notarytool submit --keychain-profile <profile> ./.dist/mac/*.pkg
      2. xcrun altool --upload-app --type macos --file ./.dist/mac/*.pkg \
           --apiKey <key-id> --apiIssuer <issuer-uuid>
    See cd-mac-desktop.yml for the full set of secrets / TODOs.
STUB
exit 0
