#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
#
# Local manual build of Bronnoysund.MauiMobile for Google Play. Mirrors
# .github/workflows/cd-android-mobile.yml — AAB build runs, signing +
# Play upload are stubbed pending Play Console secrets.
#
# Usage:
#   ./deploy/deploy-android-mobile.sh                  # unsigned debug AAB
#   ./deploy/deploy-android-mobile.sh --signed         # signed release AAB
#   ./deploy/deploy-android-mobile.sh --signed --upload --track internal
#
# Signing material expected at $HOME/.bronnoysund/android/ when --signed:
#   keystore       (the .keystore / .jks file)
#   keystore.pass  (one-line text file with keystore password)
#   key.alias      (one-line text file with upload key alias)
#   key.pass       (one-line text file with key password)

set -euo pipefail

readonly TFM="net10.0-android"
readonly PROJECT="src/Bronnoysund.MauiMobile/Bronnoysund.MauiMobile.csproj"
readonly SIGN_DIR="$HOME/.bronnoysund/android"

SIGNED=0
UPLOAD=0
TRACK="internal"
while [[ $# -gt 0 ]]; do
    case "$1" in
        --signed)  SIGNED=1; shift;;
        --upload)  UPLOAD=1; shift;;
        --track)   TRACK="$2"; shift 2;;
        *) echo "Unknown arg: $1" >&2; exit 1;;
    esac
done

cd "$(dirname "$0")/../Kode"

echo "==> Ensuring MAUI Android workload"
dotnet workload install maui-android >/dev/null 2>&1 || true

if [[ $SIGNED -eq 0 ]]; then
    echo "==> Publish unsigned AAB (Release)"
    dotnet publish "$PROJECT" \
        -c Release \
        -f "$TFM" \
        -p:AndroidPackageFormat=aab \
        -o ./.dist/android
    echo "==> Build only. Output: Kode/.dist/android/"
    exit 0
fi

for f in keystore keystore.pass key.alias key.pass; do
    [[ -f "$SIGN_DIR/$f" ]] || { echo "ERROR: missing $SIGN_DIR/$f" >&2; exit 1; }
done

KS_PASS=$(<"$SIGN_DIR/keystore.pass")
KEY_ALIAS=$(<"$SIGN_DIR/key.alias")
KEY_PASS=$(<"$SIGN_DIR/key.pass")

echo "==> Publish signed AAB (Release)"
dotnet publish "$PROJECT" \
    -c Release \
    -f "$TFM" \
    -p:AndroidPackageFormat=aab \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore="$SIGN_DIR/keystore" \
    -p:AndroidSigningStorePass="$KS_PASS" \
    -p:AndroidSigningKeyAlias="$KEY_ALIAS" \
    -p:AndroidSigningKeyPass="$KEY_PASS" \
    -o ./.dist/android

if [[ $UPLOAD -eq 0 ]]; then
    echo "==> Signed AAB built. Pass --upload --track <internal|alpha|beta|production> to ship."
    exit 0
fi

cat >&2 <<STUB
==> Upload step is stubbed.
    To complete Play Console upload locally, install fastlane and run:
      fastlane supply --aab ./.dist/android/*.aab --track $TRACK \
        --package_name no.roasystemutvikling.bronnoysund.mobile \
        --json_key \$HOME/.bronnoysund/android/play-sa.json
STUB
exit 0
