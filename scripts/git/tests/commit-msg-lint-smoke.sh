#!/usr/bin/env bash
# scripts/git/tests/commit-msg-lint-smoke.sh
# ----------------------------------------------------------------------------
# Smoke test for scripts/git/commit-msg-lint.sh
#
# Runs 8 acceptance/rejection cases (per qa-plan-sprint-6 §S6-Commit-Lint)
# plus 3 edge cases (warn but accept).
#
# Exit code = number of failed cases (0 = all pass).
# Cross-platform: bash + grep -E + mktemp; no jq / python / Node.
# ----------------------------------------------------------------------------

set -u

repo_root="$(cd "$(dirname "$0")/../../.." && pwd)"
lint="$repo_root/scripts/git/commit-msg-lint.sh"

if [ ! -f "$lint" ]; then
    echo "FATAL: lint script not found at $lint" >&2
    exit 99
fi

pass=0
fail=0
total=0

# run_case <expect-exit> <expect-warn:0|1> <message> <description>
run_case() {
    local expect_exit="$1"
    local expect_warn="$2"
    local msg="$3"
    local desc="$4"
    total=$((total + 1))

    local err_file
    err_file=$(mktemp)
    bash "$lint" -m "$msg" 2>"$err_file"
    local actual_exit=$?

    local err_content
    err_content=$(cat "$err_file")
    rm -f "$err_file"

    local has_warn=0
    if printf '%s' "$err_content" | grep -E -q '^\[warn\]'; then
        has_warn=1
    fi

    local ok=1
    if [ "$actual_exit" != "$expect_exit" ]; then
        ok=0
    fi
    if [ "$expect_warn" = "1" ] && [ "$has_warn" != "1" ]; then
        ok=0
    fi
    if [ "$expect_warn" = "0" ] && [ "$expect_exit" = "0" ] && [ "$has_warn" = "1" ]; then
        ok=0
    fi

    if [ "$ok" = "1" ]; then
        pass=$((pass + 1))
        printf '  [PASS] %s\n' "$desc"
    else
        fail=$((fail + 1))
        printf '  [FAIL] %s\n' "$desc"
        printf '         expect_exit=%s actual_exit=%s expect_warn=%s has_warn=%s\n' \
            "$expect_exit" "$actual_exit" "$expect_warn" "$has_warn"
        printf '         stderr: %s\n' "$err_content"
    fi
}

# run_case_stdin <expect-exit> <message> <description>
# 测试 stdin 入参路径
run_case_stdin() {
    local expect_exit="$1"
    local msg="$2"
    local desc="$3"
    total=$((total + 1))

    local err_file
    err_file=$(mktemp)
    printf '%s' "$msg" | bash "$lint" 2>"$err_file"
    local actual_exit=$?
    rm -f "$err_file"

    if [ "$actual_exit" = "$expect_exit" ]; then
        pass=$((pass + 1))
        printf '  [PASS] %s\n' "$desc"
    else
        fail=$((fail + 1))
        printf '  [FAIL] %s expect_exit=%s actual_exit=%s\n' "$desc" "$expect_exit" "$actual_exit"
    fi
}

# run_case_file <expect-exit> <message> <description>
# 测试 file path 入参路径
run_case_file() {
    local expect_exit="$1"
    local msg="$2"
    local desc="$3"
    total=$((total + 1))

    local tmp
    tmp=$(mktemp)
    printf '%s' "$msg" > "$tmp"
    bash "$lint" "$tmp" >/dev/null 2>&1
    local actual_exit=$?
    rm -f "$tmp"

    if [ "$actual_exit" = "$expect_exit" ]; then
        pass=$((pass + 1))
        printf '  [PASS] %s\n' "$desc"
    else
        fail=$((fail + 1))
        printf '  [FAIL] %s expect_exit=%s actual_exit=%s\n' "$desc" "$expect_exit" "$actual_exit"
    fi
}

echo "─── 8 cases (qa-plan-sprint-6 §S6-Commit-Lint) ───"

# 1: accept conventional with scope + TR
run_case 0 0 "feat(combat-ui): cu-006 godot integration (TR-combat-ui-006)" \
    "case 1 — feat scope + TR (accept, no warn)"

# 2: accept Chinese scope + Chinese description
run_case 0 0 "feat(战斗UI): cu-006 godot 集成" \
    "case 2 — Chinese scope + Chinese desc (accept)"

# 3: accept missing scope
run_case 0 0 "fix: correct timescale stack" \
    "case 3 — missing scope (accept)"

# 4: accept docs with Chinese desc
run_case 0 0 "docs(adr-0011): 实机集成段落补全" \
    "case 4 — docs scope + Chinese desc (accept)"

# 5: reject blacklisted pattern
run_case 1 0 "阶段性提交" \
    "case 5 — '阶段性提交' (reject)"

# 6: reject placeholder
run_case 1 0 "init(test):" \
    "case 6 — 'init(test):' (reject)"

# 7: reject empty
run_case 1 0 "" \
    "case 7 — empty (reject)"

# 8: reject emoji-only
run_case 1 0 "🚀" \
    "case 8 — emoji only (reject)"

echo "─── 3 edge cases (warn but accept) ───"

# E1: multi-line body (subject conformant, body free-form)
run_case 0 0 "$(printf 'feat(x): subject\n\nbody line 1\nbody line 2')" \
    "edge 1 — multi-line body (accept, no warn)"

# E2: subject > 72 chars
long_subj="feat(x): "
i=0
while [ $i -lt 80 ]; do long_subj="${long_subj}a"; i=$((i + 1)); done
run_case 0 1 "$long_subj" \
    "edge 2 — subject > 72 chars (accept + warn)"

# E3: subject ends with period
run_case 0 1 "feat(x): hi." \
    "edge 3 — trailing period (accept + warn)"

echo "─── input mode coverage ───"

run_case_stdin 0 "feat(x): from stdin" "stdin input mode (accept)"
run_case_file 0 "feat(x): from file" "file path input mode (accept)"
run_case_stdin 1 "阶段性提交" "stdin input mode (reject)"

echo
printf 'PASS %d/%d\n' "$pass" "$total"

exit "$fail"
