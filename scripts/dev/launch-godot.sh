#!/usr/bin/env bash
# Launch Godot editor (or runtime) for a project.
#
# Workaround for upstream mcp_godot v0.1.1 bug on macOS: it spawns the Godot
# binary directly, but the Mono bundle exits immediately when invoked outside
# LaunchServices. We use `open -a` instead.
#
# Usage:
#   scripts/dev/launch-godot.sh [project] [--editor|--run] [--no-check]
#   scripts/dev/launch-godot.sh --list
#   scripts/dev/launch-godot.sh -h | --help
#
# project aliases:
#   feng-zhi             -> feng-zhi/                                      (default)
#   sprint5-harness      -> prototypes/sprint5-combat-ui-harness/
#   burst-read-concept   -> prototypes/burst-read-combat-concept/engine/
#   <path>               -> any directory containing project.godot
#
# Exit codes:
#   0  launched ok
#   1  Godot.app not found
#   2  project path has no project.godot
#   3  same project already running (use --no-check to bypass)

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

GODOT_APP="${GODOT_BIN:-/Applications/Godot_mono.app}"

resolve_project() {
  case "$1" in
    feng-zhi)            echo "$REPO_ROOT/feng-zhi" ;;
    sprint5-harness)     echo "$REPO_ROOT/prototypes/sprint5-combat-ui-harness" ;;
    burst-read-concept)  echo "$REPO_ROOT/prototypes/burst-read-combat-concept/engine" ;;
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
  sprint5-harness      -> $REPO_ROOT/prototypes/sprint5-combat-ui-harness
  burst-read-concept   -> $REPO_ROOT/prototypes/burst-read-combat-concept/engine
EOF
  exit 0
}

PROJECT_ARG="feng-zhi"
MODE="--editor"
SKIP_CHECK=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)        usage ;;
    --list)           list_projects ;;
    --editor)         MODE="--editor"; shift ;;
    --run)            MODE="--run"; shift ;;
    --no-check)       SKIP_CHECK=1; shift ;;
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

if [[ "$SKIP_CHECK" -eq 0 ]]; then
  if pgrep -f "Godot.*MacOS/Godot.* --path $PROJECT_DIR" >/dev/null 2>&1; then
    echo "info: Godot already running for $PROJECT_DIR (use --no-check to launch another instance)" >&2
    exit 3
  fi
fi

ARGS=(--path "$PROJECT_DIR")
if [[ "$MODE" == "--editor" ]]; then
  ARGS+=(--editor)
fi

echo "launching Godot ($MODE) for $PROJECT_DIR"
open -a "$GODOT_APP" --args "${ARGS[@]}"
