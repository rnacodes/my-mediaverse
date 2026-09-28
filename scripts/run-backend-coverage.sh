#!/bin/bash
# =============================================================================
# Backend Code Coverage
# =============================================================================
# Runs the backend tests with coverage collection and builds an HTML report.
#
# USAGE (from the repository root):
#   ./scripts/run-backend-coverage.sh                # unit tests only
#   ./scripts/run-backend-coverage.sh --integration  # unit + integration (needs Docker)
#
# OUTPUT:
#   coverage/backend/index.html    full report, browsable by project and class
#   coverage/backend/Summary.txt   plain-text summary
#
# The report tool is restored from dotnet-tools.json, and what gets measured is
# controlled by tests/coverlet.runsettings.
# =============================================================================

set -euo pipefail

INCLUDE_INTEGRATION=false
if [ "${1:-}" = "--integration" ]; then
    INCLUDE_INTEGRATION=true
fi

REPORT_DIR="coverage/backend"
RESULTS_DIR="$REPORT_DIR/raw"
SETTINGS="tests/coverlet.runsettings"

if [ ! -f "$SETTINGS" ]; then
    echo "Run this script from the repository root." >&2
    exit 1
fi

rm -rf "$REPORT_DIR"
dotnet tool restore

TEST_FAILED=false

run_tests() {
    local project="$1"
    local name="$2"

    echo ""
    echo "=== $name ==="
    if ! dotnet test "$project" --settings "$SETTINGS" --collect:"XPlat Code Coverage" \
        --results-directory "$RESULTS_DIR/$name" --nologo --verbosity minimal; then
        TEST_FAILED=true
    fi
}

run_tests "tests/MyMediaVerse.UnitTests/MyMediaVerse.UnitTests.csproj" "unit"

if [ "$INCLUDE_INTEGRATION" = true ]; then
    run_tests "tests/MyMediaVerse.IntegrationTests/MyMediaVerse.IntegrationTests.csproj" "integration"
fi

echo ""
echo "=== Building report ==="
dotnet reportgenerator \
    "-reports:$RESULTS_DIR/**/coverage.cobertura.xml" \
    "-targetdir:$REPORT_DIR" \
    "-reporttypes:Html;TextSummary" \
    "-verbosity:Warning"

echo ""
head -n 20 "$REPORT_DIR/Summary.txt"
echo ""
echo "Full report: $REPORT_DIR/index.html"

if [ "$TEST_FAILED" = true ]; then
    echo "Some tests failed, so the coverage numbers above are incomplete." >&2
    exit 1
fi
