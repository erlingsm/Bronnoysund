#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
#
# Regenerer Brreg-klient fra live OpenAPI-spec.
#
# Bruk:
#   ./regenerate.sh           # full regenerering
#   ./regenerate.sh --check   # bare verifiser at committed kode matcher live spec
#                             (brukes i CI for å oppdage drift)

set -euo pipefail

readonly KIOTA_VERSION="1.31.1"
readonly SPEC_URL="https://data.brreg.no/enhetsregisteret/api/dokumentasjon/no/openapi.json"
readonly OUTPUT_DIR="Generated"
readonly NAMESPACE="Bronnoysund.Infrastructure.Brreg.Generated"
readonly CLIENT_CLASS="BrregClient"

cd "$(dirname "$0")"

echo "==> Verifiserer Kiota CLI ($KIOTA_VERSION)"
if ! command -v kiota >/dev/null 2>&1; then
    echo "    Installerer Kiota CLI globalt..."
    dotnet tool install --global Microsoft.OpenApi.Kiota --version "$KIOTA_VERSION"
elif ! kiota --version 2>&1 | grep -q "$KIOTA_VERSION"; then
    echo "    Oppdaterer Kiota til $KIOTA_VERSION..."
    dotnet tool update --global Microsoft.OpenApi.Kiota --version "$KIOTA_VERSION"
fi

CHECK_MODE=0
[[ "${1:-}" == "--check" ]] && CHECK_MODE=1

if [[ $CHECK_MODE -eq 1 ]]; then
    echo "==> Drift-check (sammenligner live spec mot committed kiota-lock.json)"
    TMP=$(mktemp -d)
    trap 'rm -rf "$TMP"' EXIT
    kiota generate \
        -d "$SPEC_URL" \
        -l CSharp \
        -c "$CLIENT_CLASS" \
        -n "$NAMESPACE" \
        -o "$TMP/Generated" >/dev/null
    if diff -q "$TMP/Generated/kiota-lock.json" "$OUTPUT_DIR/kiota-lock.json" >/dev/null 2>&1; then
        echo "    OK: committed kode er i sync med live spec"
        exit 0
    else
        echo "    DRIFT: Brreg har endret spec'en siden siste regenerering" >&2
        diff "$OUTPUT_DIR/kiota-lock.json" "$TMP/Generated/kiota-lock.json" || true
        echo ""
        echo "    Kjør ./regenerate.sh (uten --check) for å oppdatere." >&2
        exit 1
    fi
fi

echo "==> Sletter eksisterende generert kode"
rm -rf "$OUTPUT_DIR"

echo "==> Genererer ny klient fra $SPEC_URL"
kiota generate \
    -d "$SPEC_URL" \
    -l CSharp \
    -c "$CLIENT_CLASS" \
    -n "$NAMESPACE" \
    -o "$OUTPUT_DIR"

FILES=$(find "$OUTPUT_DIR" -name '*.cs' | wc -l | tr -d ' ')
echo "==> $FILES .cs-filer generert"

echo ""
echo "Påminnelse: sjekk inn $OUTPUT_DIR/ + kiota-lock.json"
echo "    cd ../../.. && git status"
echo ""
echo "Hvis kompilering feiler eller adapter-tester ryker, sjekk Plan 53"
echo "(graceful fallback) — Brreg kan ha fjernet eller endret felter."
