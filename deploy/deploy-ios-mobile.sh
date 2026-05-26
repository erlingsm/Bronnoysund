#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
#
# Local manual build of Bronnoysund.MauiMobile for iOS TestFlight / App
# Store. Mirrors .github/workflows/cd-ios-mobile.yml — IPA build runs,
# signing + altool upload are stubbed pending Apple Developer setup.
#
# Usage:
#   ./deploy/deploy-ios-mobile.sh           # build only
#   ./deploy/deploy-ios-mobile.sh --upload  # build + upload (when wired)

set -euo pipefail

readonly TFM="net10.0-ios"
readonly PROJECT="src/Bronnoysund.MauiMobile/Bronnoysund.MauiMobile.csproj"
readonly RID="ios-arm64"

UPLOAD=0
[[ "${1:-}" == "--upload" ]] && UPLOAD=1

if [[ "$(uname)" != "Darwin" ]]; then
    echo "ERROR: iOS builds require macOS + Xcode." >&2
    exit 1
fi

cd "$(dirname "$0")/../Kode"

echo "==> Ensuring MAUI iOS workload"
dotnet workload install maui-ios >/dev/null 2>&1 || true

echo "==> Publish .ipa (Release, $RID)"
dotnet publish "$PROJECT" \
    -c Release \
    -f "$TFM" \
    -p:RuntimeIdentifier="$RID" \
    -p:ArchiveOnBuild=true \
    -o ./.dist/ios

if [[ $UPLOAD -eq 0 ]]; then
    echo "==> Build only. Output: Kode/.dist/ios/"
    echo "    Pass --upload once signing + ASC API key are wired."
    exit 0
fi

cat >&2 <<'STUB'
==> Upload step is stubbed.
    To complete TestFlight upload locally:
      xcrun altool --upload-app --type ios --file ./.dist/ios/*.ipa \
        --apiKey <key-id> --apiIssuer <issuer-uuid>
    TestFlight processing is automatic — the build appears in App Store
    Connect within ~10 min. See cd-ios-mobile.yml for required secrets.
STUB
exit 0
