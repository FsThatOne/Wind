#!/usr/bin/env bash
# scripts/git/commit-msg-lint.sh
# ----------------------------------------------------------------------------
# 手动可跑的 commit message linter（不依赖 git hook、不依赖 jq/python/Node）。
#
# 用法（三种入参）:
#   1) 文件:     bash scripts/git/commit-msg-lint.sh <path-to-msg-file>
#   2) 字符串:   bash scripts/git/commit-msg-lint.sh -m "feat(x): hi"
#   3) stdin:   echo "..." | bash scripts/git/commit-msg-lint.sh
#
# Exit code:
#   0  pass（含 warning）
#   1  reject
#   2  用法错误
#
# 跨平台: bash + grep -E + mktemp（POSIX）；不使用 grep -P。
# 权威规范: docs/git-workflow.md
# ----------------------------------------------------------------------------

set -u

usage() {
    cat >&2 <<'EOF'
Usage:
  commit-msg-lint.sh <path-to-msg-file>
  commit-msg-lint.sh -m "<commit message>"
  echo "<commit message>" | commit-msg-lint.sh
EOF
    exit 2
}

# ---------- 1. 读取 commit message ----------
msg=""
if [ "$#" -ge 2 ] && [ "$1" = "-m" ]; then
    msg="$2"
elif [ "$#" -eq 1 ]; then
    if [ "$1" = "-h" ] || [ "$1" = "--help" ]; then
        usage
    fi
    if [ ! -f "$1" ]; then
        echo "[reject] file not found: $1" >&2
        exit 2
    fi
    msg=$(cat "$1")
elif [ "$#" -eq 0 ]; then
    if [ -t 0 ]; then
        # stdin 是 tty（无管道输入）
        usage
    fi
    msg=$(cat)
else
    usage
fi

# ---------- 2. 取 subject（首行，去除 \r） ----------
subject=$(printf '%s' "$msg" | head -n 1 | tr -d '\r')

# trim 前后空白
trimmed=$(printf '%s' "$subject" | sed -E 's/^[[:space:]]+//; s/[[:space:]]+$//')

reject() {
    local reason="$1"
    local snippet
    snippet=$(printf '%s' "$subject" | cut -c1-80)
    echo "[reject] ${reason}: ${snippet}" >&2
    exit 1
}

warn() {
    echo "[warn] $1" >&2
}

# ---------- 3. 拒绝规则 ----------

# R1: 空 subject
if [ -z "$trimmed" ]; then
    reject "empty subject"
fi

# R2: 已知 bad pattern（先于 emoji-only 检查，确保 reason 精准）
if printf '%s' "$trimmed" | grep -E -q '^阶段性提交$'; then
    reject "blacklisted pattern '阶段性提交'"
fi
if printf '%s' "$trimmed" | grep -E -q '^init\(test\):[[:space:]]*$'; then
    reject "blacklisted placeholder 'init(test):'"
fi

# R3: 仅 emoji / 纯非 ASCII 标点 / 不含任何 ASCII 可打印字符
#     合法 conventional subject 必含 ASCII（type 关键字 + ': '），所以"无 ASCII 可打印字符"
#     即可作为纯 emoji / 纯非法符号的 fallback 拦截。
#     用 LC_ALL=C 让 [[:print:]] 仅匹配 ASCII 可见字符（避免 macOS grep 多字节误判）。
if ! printf '%s' "$trimmed" | LC_ALL=C grep -E -q '[[:print:]]'; then
    reject "subject contains no ASCII printable chars (emoji-only or non-conforming)"
fi

# R4: 必须匹配 conventional commits 格式
#     ^(type)(\(scope\))?: <subject 非空>
#
#     type 白名单: feat / fix / docs / test / chore / refactor / perf / build / ci
type_re='^(feat|fix|docs|test|chore|refactor|perf|build|ci)(\([^)]+\))?:[[:space:]]+.+'
if ! printf '%s' "$trimmed" | grep -E -q "$type_re"; then
    reject "subject does not match '<type>(<scope>): <subject>' (allowed types: feat|fix|docs|test|chore|refactor|perf|build|ci)"
fi

# subject 在 ': ' 之后必须非空
desc_part=$(printf '%s' "$trimmed" | sed -E 's/^[a-z]+(\([^)]+\))?:[[:space:]]*//')
if [ -z "$desc_part" ]; then
    reject "missing description after ':'"
fi

# ---------- 4. 警告规则（不阻塞） ----------

# W1: subject > 72 char（用 awk 计算字符长度，跨平台）
sub_len=$(printf '%s' "$trimmed" | awk '{print length($0)}')
if [ "${sub_len:-0}" -gt 72 ]; then
    warn "subject length ${sub_len} > 72 — consider shortening"
fi

# W2: subject 末尾是中英文句号
if printf '%s' "$trimmed" | grep -E -q '[.。]$'; then
    warn "subject ends with period — conventional commits prefer no trailing punctuation"
fi

exit 0
