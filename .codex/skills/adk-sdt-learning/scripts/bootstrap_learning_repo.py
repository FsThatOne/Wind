#!/usr/bin/env python3
"""Bootstrap an SDT learning repository.

This script is intentionally idempotent: it creates missing configuration,
directories, and index files, but never overwrites user-maintained content.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


PREFERENCE_FILE = ".sdt_preference.yaml"
DEFAULT_LEARNING_ROOT = ".experience"

REQUIRED_DIRS = [
    "Clippings",
    "experience",
    "experience/indexes",
    "experience/articles",
    "experience/concepts",
    "experience/topics",
]

DEFAULT_PREFERENCE = """sdt_learning:
  learning_root: {learning_root}
  record_query_usage: confirm
  query_usage_confirmation: true
  default_write_policy: learning-root-only
"""

DEFAULT_FILES = {
    "experience/INDEX.md": """# SDT 经验索引入口

## 使用规则

- 先读本文件判断应进入哪个分片索引。
- 再读取 1 个最相关的 `indexes/*.md`。
- 最后只读取 2-5 个经验页。
- 经验命中后仍必须验证适用条件、证据和当前 spec/code。

## 高频入口

见 `indexes/hot.md`。

## 分片索引

- 按业务域：`indexes/by-domain.md`
- 按平台：`indexes/by-platform.md`
- 按信号类型：`indexes/by-signal.md`
- 外部知识源：`indexes/external-sources.md`
- 高频经验：`indexes/hot.md`
- 待裁决项：`indexes/pending-decisions.md`
""",
    "experience/indexes/hot.md": """# 高频经验索引

| 经验页 | 关键词 | 最近使用 | 状态 | 备注 |
| --- | --- | --- | --- | --- |
""",
    "experience/indexes/by-domain.md": """# 按业务域索引

| 业务域/PSM/产品线 | 关键词 | 入口页面 | 状态 |
| --- | --- | --- | --- |
""",
    "experience/indexes/by-platform.md": """# 按平台索引

| 平台 | 关键词 | 入口页面 | 状态 |
| --- | --- | --- | --- |
""",
    "experience/indexes/by-signal.md": """# 按信号类型索引

| 信号类型 | 关键词 | 入口页面 | 状态 |
| --- | --- | --- | --- |
""",
    "experience/indexes/external-sources.md": """# 外部知识源索引

| 知识源 | 路径 | 类型 | 关键词 | 读取策略 | 写入策略 |
| --- | --- | --- | --- | --- | --- |
""",
    "experience/indexes/pending-decisions.md": """# 待裁决项

| 事项 | 上下文 | Owner | 开始时间 | 关联页面 |
| --- | --- | --- | --- | --- |
""",
    "experience/log.md": """# SDT 经验日志

| 日期 | 操作 | 来源 | 更新页面 | 备注 |
| --- | --- | --- | --- | --- |
""",
    "experience/maintenance.json": """{
  "last_linted_at": null,
  "last_lint_result": null
}
""",
}


@dataclass
class Issue:
    severity: str
    path: str
    message: str


class BootstrapResult:
    def __init__(self) -> None:
        self.created: list[str] = []
        self.existing: list[str] = []
        self.issues: list[Issue] = []

    def add_issue(self, severity: str, path: Path | str, message: str) -> None:
        self.issues.append(Issue(severity, str(path), message))

    def has_errors(self) -> bool:
        return any(issue.severity == "ERROR" for issue in self.issues)


def repo_root_from_git(cwd: Path) -> Path:
    try:
        output = subprocess.check_output(
            ["git", "rev-parse", "--show-toplevel"],
            cwd=str(cwd),
            text=True,
            stderr=subprocess.DEVNULL,
        ).strip()
        return Path(output)
    except Exception:
        return cwd


def strip_inline_comment(value: str) -> str:
    if "#" not in value:
        return value.strip()
    return value.split("#", 1)[0].strip()


def parse_scalar(value: str) -> str:
    value = strip_inline_comment(value)
    if len(value) >= 2 and value[0] == value[-1] and value[0] in {"'", '"'}:
        return value[1:-1]
    return value


def read_learning_root(pref_path: Path, result: BootstrapResult) -> str | None:
    try:
        lines = pref_path.read_text(encoding="utf-8").splitlines()
    except OSError as exc:
        result.add_issue("ERROR", pref_path, f"failed to read preference file: {exc}")
        return None

    current_top: str | None = None
    learning_root: str | None = None
    has_sdt_learning = False

    for line_no, raw_line in enumerate(lines, start=1):
        if not raw_line.strip() or raw_line.lstrip().startswith("#"):
            continue
        indent = len(raw_line) - len(raw_line.lstrip(" "))
        line = raw_line.strip()
        if ":" not in line:
            result.add_issue("ERROR", pref_path, f"line {line_no}: expected YAML key/value")
            continue
        key, raw_value = line.split(":", 1)
        key = key.strip()
        value = parse_scalar(raw_value)

        if indent == 0:
            current_top = key
            has_sdt_learning = has_sdt_learning or key == "sdt_learning"
            continue
        if current_top == "sdt_learning" and indent == 2 and key == "learning_root":
            learning_root = value

    if not has_sdt_learning:
        result.add_issue("ERROR", pref_path, "existing preference file is missing sdt_learning section")
        return None
    if not learning_root:
        result.add_issue("ERROR", pref_path, "existing sdt_learning section is missing learning_root")
        return None
    return learning_root


def ensure_directory(path: Path, result: BootstrapResult) -> None:
    if path.exists():
        if path.is_dir():
            result.existing.append(str(path))
            return
        result.add_issue("ERROR", path, "path exists but is not a directory")
        return
    try:
        path.mkdir(parents=True, exist_ok=True)
        result.created.append(str(path))
    except OSError as exc:
        result.add_issue("ERROR", path, f"failed to create directory: {exc}")


def ensure_file(path: Path, content: str, result: BootstrapResult) -> None:
    if path.exists():
        if path.is_file():
            result.existing.append(str(path))
            return
        result.add_issue("ERROR", path, "path exists but is not a file")
        return
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        result.created.append(str(path))
    except OSError as exc:
        result.add_issue("ERROR", path, f"failed to create file: {exc}")


def issues_as_dicts(issues: list[Issue]) -> list[dict[str, str]]:
    return [
        {"severity": issue.severity, "path": issue.path, "message": issue.message}
        for issue in issues
    ]


def print_result(repo_root: Path, learning_root: Path | None, result: BootstrapResult, as_json: bool) -> None:
    payload = {
        "ok": not result.has_errors(),
        "repo_root": str(repo_root),
        "learning_root": str(learning_root) if learning_root else None,
        "created": result.created,
        "existing": result.existing,
        "issues": issues_as_dicts(result.issues),
    }
    if as_json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return
    if result.has_errors():
        print("ERROR: SDT learning repository bootstrap failed.")
        for issue in result.issues:
            print(f"- {issue.severity}: {issue.path}: {issue.message}")
        return
    print("OK: SDT learning repository bootstrap completed.")
    if result.created:
        print("Created:")
        for path in result.created:
            print(f"- {path}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Bootstrap SDT learning repository")
    parser.add_argument("--repo-root", default=None, help="Git repository root. Defaults to git rev-parse.")
    parser.add_argument("--learning-root", default=None, help="Learning root to use when creating a new preference file.")
    parser.add_argument("--json", action="store_true", help="Print machine-readable JSON output.")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve() if args.repo_root else repo_root_from_git(Path.cwd())
    result = BootstrapResult()
    pref_path = repo_root / PREFERENCE_FILE

    if pref_path.exists():
        learning_root_value = read_learning_root(pref_path, result)
        if args.learning_root and learning_root_value and args.learning_root != learning_root_value:
            result.add_issue(
                "ERROR",
                pref_path,
                f"--learning-root '{args.learning_root}' does not match existing '{learning_root_value}'",
            )
    else:
        learning_root_value = args.learning_root or DEFAULT_LEARNING_ROOT
        ensure_file(pref_path, DEFAULT_PREFERENCE.format(learning_root=learning_root_value), result)

    learning_root = (repo_root / learning_root_value).resolve() if learning_root_value else None
    if learning_root and not result.has_errors():
        for dirname in REQUIRED_DIRS:
            ensure_directory(learning_root / dirname, result)
        for relative_path, content in DEFAULT_FILES.items():
            ensure_file(learning_root / relative_path, content, result)

    print_result(repo_root, learning_root, result, args.json)
    return 1 if result.has_errors() else 0


if __name__ == "__main__":
    sys.exit(main())
