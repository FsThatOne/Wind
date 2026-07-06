#!/usr/bin/env python3
"""
Detect repository root and recommended output-root for adk-readiness.

Given one or more target paths, compute:
- repo_root:     preferred git repo root (useful for resolving module-key
                 conflicts via relative path). When targets span multiple git
                 repos this falls back to the first target's git root.
- output_root:   directory where the top-level `.ai-readiness/` should be
                 placed, per 决策 12 rules:
                   1. explicit --output-root override wins
                   2. all targets share one git root → use that
                   3. common ancestor directory of all absolute target paths
                      (if ancestor is "/" fall back to first target's dir)

Usage:
    python3 detect_repo_root.py <path1> [<path2> ...]
    python3 detect_repo_root.py --output-root /tmp/myroot <path1> <path2>
"""

import argparse
import json
import os
import subprocess
import sys


def _git_toplevel(path):
    """Return absolute path to the git repo containing `path`, or None."""
    try:
        abs_path = os.path.abspath(path)
    except (OSError, TypeError):
        return None
    if not os.path.exists(abs_path):
        return None
    try:
        out = subprocess.run(
            ["git", "rev-parse", "--show-toplevel"],
            cwd=abs_path,
            capture_output=True,
            text=True,
            check=False,
        )
    except (OSError, subprocess.SubprocessError):
        return None
    if out.returncode != 0 or not out.stdout.strip():
        return None
    return os.path.normpath(out.stdout.strip())


def _common_ancestor(paths):
    """Longest common ancestor of a list of absolute paths; never '/'.

    Returns 'None' for empty input.
    """
    if not paths:
        return None
    abs_paths = [os.path.abspath(p) for p in paths]
    ca = os.path.commonpath(abs_paths)
    if ca in ("/", ""):
        # common ancestor is filesystem root — too broad; fall back to the
        # directory containing the first target (or the first target itself
        # if it's already a directory)
        first = abs_paths[0]
        return first if os.path.isdir(first) else os.path.dirname(first)
    return ca


def detect(target_paths, explicit_output_root=None):
    targets_norm = []
    git_roots = []
    for p in target_paths:
        abs_p = os.path.normpath(os.path.abspath(p))
        targets_norm.append(abs_p)
        git_roots.append(_git_toplevel(abs_p))

    # --- repo_root ---------------------------------------------------------
    # first target's git root wins when available; else None (caller falls
    # back to current working directory / skill-root heuristics)
    repo_root = next((gr for gr in git_roots if gr), None)

    # --- output_root -------------------------------------------------------
    if explicit_output_root:
        output_root = os.path.normpath(os.path.abspath(explicit_output_root))
        source = "explicit"
    else:
        unique_git = {gr for gr in git_roots if gr}
        if len(git_roots) >= 1 and len(unique_git) == 1:
            # All targets live under the same git repo (or git detection
            # failed uniformly). Use that single git root.
            output_root = next(iter(unique_git))
            source = "git-single"
        else:
            output_root = _common_ancestor(targets_norm)
            source = "common-ancestor"

    # --- is_monorepo_child? -----------------------------------------------
    # True when ANY target path is a strict subdir of output_root AND the
    # target basename != output_root basename (user is evaluating a sub-pkg).
    def _is_child(target, root):
        if root is None or target is None:
            return False
        try:
            rel = os.path.relpath(target, root)
        except ValueError:  # different drives on Windows
            return False
        return not rel.startswith("..") and rel not in (".", "")

    any_child_evaluation = any(_is_child(t, output_root) for t in targets_norm)

    return {
        "repo_root": repo_root,
        "output_root": output_root,
        "ai_readiness_dir": os.path.join(output_root, ".ai-readiness"),
        "source": source,
        "targets": targets_norm,
        "git_roots_per_target": git_roots,
        "any_child_evaluation": any_child_evaluation,
    }


def main():
    parser = argparse.ArgumentParser(
        description="Detect repo root and recommended output-root for adk-readiness"
    )
    parser.add_argument("paths", nargs="+", help="One or more target paths")
    parser.add_argument(
        "--output-root",
        default=None,
        help="Explicit output root override (skips auto-detection)",
    )
    args = parser.parse_args()

    result = detect(args.paths, explicit_output_root=args.output_root)
    json.dump(result, sys.stdout, indent=2, ensure_ascii=False)
    sys.stdout.write("\n")


if __name__ == "__main__":
    main()
