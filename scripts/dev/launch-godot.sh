#!/usr/bin/env bash
# Launch Godot editor (or runtime) for a project.
#
# Workaround for upstream mcp_godot v0.1.1 bug on macOS: it spawns the Godot
# binary directly, but the Mono bundle exits immediately when invoked outside
# LaunchServices. We use `open -a` instead.
#
# Usage:
#   scripts/dev/launch-godot.sh [project] [--editor|--run]
#   scripts/dev/launch-godot.sh --list
#   scripts/dev/launch-godot.sh -h | --help
#
# project aliases:
#   feng-zhi             -> feng-zhi/                                      (default)
#   <path>               -> any directory containing project.godot
#
# Note: on macOS, `open -a` is idempotent for a given .app bundle — calling the
# script twice for the same project just activates the existing window rather
# than launching a second instance, so no dedup is needed.
#
# Exit codes:
#   0  launched ok
#   1  Godot.app not found
#   2  project path has no project.godot

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

GODOT_APP="${GODOT_BIN:-/Applications/Godot_mono.app}"

resolve_project() {
  case "$1" in
    feng-zhi)            echo "$REPO_ROOT/feng-zhi" ;;
    /*)                  echo "$1" ;;
    *)                   echo "$REPO_ROOT/$1" ;;
  esac
}

usage() {
  sed -n '2,/^$/p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
  exit 0
}

list_projects() {
  cat <<EOF
known project aliases:
  feng-zhi             -> $REPO_ROOT/feng-zhi

(any other path may be passed directly: scripts/dev/launch-godot.sh path/to/project)
EOF
  exit 0
}

PROJECT_ARG="feng-zhi"
MODE="--editor"

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)        usage ;;
    --list)           list_projects ;;
    --editor)         MODE="--editor"; shift ;;
    --run)            MODE="--run"; shift ;;
    -*)               echo "unknown flag: $1" >&2; exit 64 ;;
    *)                PROJECT_ARG="$1"; shift ;;
  esac
done

if [[ ! -d "$GODOT_APP" ]]; then
  echo "error: Godot.app not found at $GODOT_APP" >&2
  echo "       set GODOT_BIN=/path/to/Godot.app to override" >&2
  exit 1
fi

PROJECT_DIR="$(resolve_project "$PROJECT_ARG")"
if [[ ! -f "$PROJECT_DIR/project.godot" ]]; then
  echo "error: $PROJECT_DIR has no project.godot" >&2
  echo "       run '$0 --list' to see known aliases" >&2
  exit 2
fi

ARGS=(--path "$PROJECT_DIR")
if [[ "$MODE" == "--editor" ]]; then
  ARGS+=(--editor)
fi

echo "launching Godot ($MODE) for $PROJECT_DIR"
open -a "$GODOT_APP" --args "${ARGS[@]}"
