#!/usr/bin/env zsh
# cu-006 Godot integration test runner.
#
# Usage:
#   ./scripts/run_godot_tests.sh smoke   # Stage 1 smoke
#   ./scripts/run_godot_tests.sh cu006   # Stage 4 AC tests
#
# Exit code = number of failed tests (0 = all PASS).

set -euo pipefail

GODOT="${GODOT:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"
SCRIPT_DIR="$(cd "$(dirname "${0}")" && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"

SUITE="${1:-smoke}"
case "${SUITE}" in
  smoke)
    SCENE="res://scenes/tests/_smoke/SmokeTest.tscn"
    ;;
  cu006)
    SCENE="res://scenes/tests/cu006/CombatUiDecisiveGodotIntegrationTest.tscn"
    ;;
  *)
    echo "Unknown suite: ${SUITE} (expected: smoke | cu006)" >&2
    exit 2
    ;;
esac

if [[ ! -x "${GODOT}" ]]; then
  echo "Godot binary not found at ${GODOT}" >&2
  exit 2
fi

echo ">>> Running ${SUITE} suite (${SCENE}) under Godot 4.7-stable headless..."
"${GODOT}" --path "${PROJECT_DIR}" --headless "${SCENE}"
RC=$?
echo ">>> ${SUITE} suite exited with code ${RC}"
exit ${RC}
