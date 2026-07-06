#!/usr/bin/env python3
"""Validate an SDT learning repository.

This script intentionally uses only the Python standard library so it can run
inside business repositories without extra setup.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


VALID_STATUSES = {"candidate", "active", "trusted", "needs-review", "deprecated"}
REQUIRED_INDEXES = [
    "hot.md",
    "by-domain.md",
    "by-platform.md",
    "by-signal.md",
    "external-sources.md",
    "pending-decisions.md",
]
REQUIRED_EXPERIENCE_DIRS = ["articles", "concepts", "topics", "indexes"]
PREFERENCE_FILE = ".sdt_preference.yaml"


@dataclass
class Issue:
    severity: str
    path: str
    message: str


class Reporter:
    def __init__(self) -> None:
        self.issues: list[Issue] = []

    def add(self, severity: str, path: Path | str, message: str) -> None:
        self.issues.append(Issue(severity, str(path), message))

    def has_errors(self) -> bool:
        return any(issue.severity == "ERROR" for issue in self.issues)

    def print(self) -> None:
        if not self.issues:
            print("OK: SDT learning repository validation passed.")
            return
        for severity in ("ERROR", "WARNING"):
            group = [issue for issue in self.issues if issue.severity == severity]
            if not group:
                continue
            print(f"{severity}: {len(group)}")
            for issue in group:
                print(f"- {issue.path}: {issue.message}")


def issues_as_dicts(issues: list[Issue]) -> list[dict[str, str]]:
    return [
        {"severity": issue.severity, "path": issue.path, "message": issue.message}
        for issue in issues
    ]


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


def read_preference(repo_root: Path, reporter: Reporter) -> dict:
    pref_path = repo_root / PREFERENCE_FILE
    config: dict = {"sdt_learning": {}, "external_sources": {}}
    if not pref_path.exists():
        reporter.add("ERROR", pref_path, f"missing {PREFERENCE_FILE}")
        return config
    try:
        lines = pref_path.read_text(encoding="utf-8").splitlines()
    except OSError as exc:
        reporter.add("ERROR", pref_path, f"failed to read preference file: {exc}")
        return config

    current_top: str | None = None
    in_external_sources = False
    current_source: str | None = None

    for line_no, raw_line in enumerate(lines, start=1):
        if not raw_line.strip() or raw_line.lstrip().startswith("#"):
            continue
        indent = len(raw_line) - len(raw_line.lstrip(" "))
        line = raw_line.strip()
        if ":" not in line:
            reporter.add("ERROR", pref_path, f"line {line_no}: expected YAML key/value")
            continue
        key, raw_value = line.split(":", 1)
        key = key.strip()
        value = parse_scalar(raw_value)

        if indent == 0:
            current_top = key
            in_external_sources = False
            current_source = None
            if current_top != "sdt_learning":
                reporter.add("WARNING", pref_path, f"line {line_no}: unknown top-level key '{key}'")
            continue

        if current_top != "sdt_learning":
            continue

        if indent == 2:
            current_source = None
            if key == "external_sources" and not value:
                in_external_sources = True
                continue
            in_external_sources = False
            config["sdt_learning"][key] = value
            continue

        if in_external_sources and indent == 4:
            current_source = key
            config["external_sources"].setdefault(current_source, {})
            continue

        if in_external_sources and indent == 6 and current_source:
            config["external_sources"][current_source][key] = value
            continue

        reporter.add("WARNING", pref_path, f"line {line_no}: unsupported YAML nesting")

    if not config["sdt_learning"]:
        reporter.add("ERROR", pref_path, "missing sdt_learning section")
    return config


def resolve_learning_root(
    repo_root: Path,
    args: argparse.Namespace,
    config: dict,
    reporter: Reporter,
) -> Path | None:
    if args.learning_root:
        return (repo_root / args.learning_root).resolve()
    if not config.get("sdt_learning"):
        return None
    raw_root = str(config["sdt_learning"].get("learning_root", "")).strip()
    if not raw_root:
        reporter.add("ERROR", repo_root / PREFERENCE_FILE, "missing sdt_learning.learning_root")
        return None
    return (repo_root / raw_root).resolve()


def line_count(path: Path) -> int:
    try:
        return len(path.read_text(encoding="utf-8").splitlines())
    except UnicodeDecodeError:
        return len(path.read_text(errors="ignore").splitlines())


def validate_structure(root: Path, reporter: Reporter) -> None:
    if not root.exists():
        reporter.add("ERROR", root, "learning_root does not exist")
        return
    clippings = root / "Clippings"
    experience = root / "experience"
    if not clippings.is_dir():
        reporter.add("ERROR", clippings, "missing Clippings directory")
    if not experience.is_dir():
        reporter.add("ERROR", experience, "missing experience directory")
        return
    for dirname in REQUIRED_EXPERIENCE_DIRS:
        path = experience / dirname
        if not path.is_dir():
            reporter.add("ERROR", path, "missing required experience subdirectory")
    for filename in ["INDEX.md", "log.md", "maintenance.json"]:
        path = experience / filename
        if not path.is_file():
            reporter.add("ERROR", path, "missing required experience file")
    maintenance = experience / "maintenance.json"
    if maintenance.is_file():
        try:
            data = json.loads(maintenance.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            reporter.add("ERROR", maintenance, f"invalid maintenance.json: {exc}")
        else:
            if not isinstance(data, dict):
                reporter.add("ERROR", maintenance, "maintenance.json must contain a JSON object")
    indexes = experience / "indexes"
    for filename in REQUIRED_INDEXES:
        path = indexes / filename
        if not path.is_file():
            reporter.add("ERROR", path, "missing required index file")


def validate_index_sizes(root: Path, reporter: Reporter) -> None:
    index = root / "experience" / "INDEX.md"
    if index.exists():
        count = line_count(index)
        if count > 300:
            reporter.add("ERROR", index, f"INDEX.md has {count} lines; 300 is a hard limit")
        elif count > 150:
            reporter.add("WARNING", index, f"INDEX.md has {count} lines; migrate content to indexes/")
        text = index.read_text(encoding="utf-8", errors="ignore")
        if re.search(r"\|\s*(经验页|Path|路径)\s*\|", text):
            reporter.add("ERROR", index, "INDEX.md appears to contain full entry tables")
    indexes_dir = root / "experience" / "indexes"
    if indexes_dir.is_dir():
        for path in indexes_dir.rglob("*.md"):
            count = line_count(path)
            if count > 500:
                reporter.add("WARNING", path, f"index shard has {count} lines; consider splitting it")


def extract_field(text: str, name: str) -> str | None:
    match = re.search(rf"^\s*-\s*{re.escape(name)}\s*:\s*(.*)$", text, re.MULTILINE)
    if not match:
        return None
    return match.group(1).strip()


def validate_experience_pages(root: Path, reporter: Reporter) -> None:
    exp = root / "experience"
    for base in ["articles", "concepts", "topics"]:
        directory = exp / base
        if not directory.is_dir():
            continue
        for path in directory.rglob("*.md"):
            text = path.read_text(encoding="utf-8", errors="ignore")
            if "## 元数据" not in text:
                reporter.add("WARNING", path, "missing ## 元数据 section")
            if "## 证据" not in text:
                reporter.add("WARNING", path, "missing ## 证据 section")
            if "## 新鲜度" not in text:
                reporter.add("WARNING", path, "missing ## 新鲜度 section")
            if re.search(r"^\s*-\s*confidence\s*:", text, re.MULTILINE):
                reporter.add("ERROR", path, "uses deprecated confidence field; use status")
            status = extract_field(text, "status")
            if status is None:
                reporter.add("WARNING", path, "missing status metadata")
            elif status not in VALID_STATUSES:
                reporter.add("ERROR", path, f"invalid status '{status}'")
            if "source_path:" in text:
                for field in ("source_section", "source_commit"):
                    value = extract_field(text, field)
                    if value is None or not value:
                        reporter.add("WARNING", path, f"external source missing {field}")


def is_git_repo(path: Path) -> bool:
    return (path / ".git").exists() or (path / ".git").is_file()


def validate_external_sources(repo_root: Path, config: dict, reporter: Reporter) -> None:
    for label, source in config.get("external_sources", {}).items():
        raw_path = str(source.get("path", "")).strip()
        if not raw_path:
            reporter.add("ERROR", PREFERENCE_FILE, f"external source '{label}' missing path")
            continue
        path = (repo_root / raw_path).resolve()
        if not path.exists():
            reporter.add("ERROR", path, f"external source '{label}' path does not exist")
        if str(source.get("type", "")).strip() == "submodule" and path.exists() and not is_git_repo(path):
            reporter.add("WARNING", path, f"external source '{label}' is marked submodule but is not initialized")
        read_only = str(source.get("read_only", "true")).strip().lower()
        write_policy = str(source.get("write_policy", "read-only")).strip()
        if read_only not in {"true", "false"}:
            reporter.add("ERROR", PREFERENCE_FILE, f"external source '{label}'.read_only must be true or false")
        if read_only == "false" and write_policy == "read-only":
            reporter.add("WARNING", PREFERENCE_FILE, f"external source '{label}' allows writes but write_policy is read-only")


def validate_usage_policy(config: dict, reporter: Reporter) -> None:
    if not config.get("sdt_learning"):
        return
    section = config["sdt_learning"]
    record = str(section.get("record_query_usage", "confirm")).strip()
    if record not in {"never", "confirm", "always"}:
        reporter.add("ERROR", PREFERENCE_FILE, "record_query_usage must be never, confirm, or always")
    if record == "always":
        reporter.add("WARNING", PREFERENCE_FILE, "record_query_usage=always is not recommended for horizontal rollout")
    write_policy = str(section.get("default_write_policy", "learning-root-only")).strip()
    if write_policy != "learning-root-only":
        reporter.add("WARNING", PREFERENCE_FILE, "default_write_policy should be learning-root-only")


def is_bootstrapable_issue(issue: Issue) -> bool:
    """Return true when Bootstrap is expected to repair the reported issue."""
    message = issue.message
    return (
        message == f"missing {PREFERENCE_FILE}"
        or message == "missing sdt_learning section"
        or message == "missing sdt_learning.learning_root"
        or message == "learning_root does not exist"
        or message.startswith("missing Clippings directory")
        or message.startswith("missing experience directory")
        or message.startswith("missing required experience subdirectory")
        or message.startswith("missing required experience file")
        or message.startswith("missing required index file")
    )


def print_initialized_result(repo_root: Path, learning_root: Path | None, reporter: Reporter, as_json: bool) -> None:
    errors = [issue for issue in reporter.issues if issue.severity == "ERROR"]
    bootstrap_required = bool(errors) and all(is_bootstrapable_issue(issue) for issue in errors)
    initialized = not errors
    payload = {
        "initialized": initialized,
        "bootstrap_required": bootstrap_required,
        "repo_root": str(repo_root),
        "learning_root": str(learning_root) if learning_root else None,
        "issues": issues_as_dicts(reporter.issues),
    }
    if as_json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return
    if initialized:
        print("OK: SDT learning repository is initialized.")
    elif bootstrap_required:
        print("NOT_INITIALIZED: Bootstrap is required.")
        reporter.print()
    else:
        print("ERROR: SDT learning repository initialization check failed.")
        reporter.print()


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate SDT learning repository")
    parser.add_argument("--repo-root", default=None, help="Git repository root. Defaults to git rev-parse.")
    parser.add_argument("--learning-root", default=None, help="Override learning root path relative to repo root.")
    parser.add_argument(
        "--check",
        choices=["full", "initialized"],
        default="full",
        help="Validation scope. 'initialized' only checks Query preflight structure.",
    )
    parser.add_argument("--json", action="store_true", help="Print machine-readable JSON output.")
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve() if args.repo_root else repo_root_from_git(Path.cwd())
    reporter = Reporter()
    config = read_preference(repo_root, reporter)
    validate_usage_policy(config, reporter)
    learning_root = resolve_learning_root(repo_root, args, config, reporter)
    if learning_root:
        validate_structure(learning_root, reporter)

    if args.check == "initialized":
        print_initialized_result(repo_root, learning_root, reporter, args.json)
        return 0 if not reporter.has_errors() else 1

    validate_external_sources(repo_root, config, reporter)
    if learning_root:
        validate_index_sizes(learning_root, reporter)
        validate_experience_pages(learning_root, reporter)

    if args.json:
        payload = {
            "ok": not reporter.has_errors(),
            "repo_root": str(repo_root),
            "learning_root": str(learning_root) if learning_root else None,
            "issues": issues_as_dicts(reporter.issues),
        }
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 1 if reporter.has_errors() else 0

    reporter.print()
    return 1 if reporter.has_errors() else 0


if __name__ == "__main__":
    sys.exit(main())
