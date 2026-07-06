#!/usr/bin/env python3
"""
Tech stack detection for adk-readiness.

Detects the project technology stack based on file signals in a target directory.
Outputs JSON with stack info, confidence level, detected signals, and evaluator name
(read from the shared stack config).

Usage:
    python3 detect_stack.py <target_path>

Output JSON fields:
    stack          : str  - stack ID (mobile-android | ... | generic)
    confidence     : str  - high | medium | low
    platform       : str  - platform identifier
    display_name   : str  - zh human-readable name
    evaluator      : str  - evaluator skill name (None if no dedicated evaluator)
    signals        : list - signal texts for the winning stack
    all_signals    : list - all detected signals across all stacks
    target_path    : str  - absolute target path
"""

import json
import os
import sys

# Allow import from the same directory
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import stack_config


def detect_signals(target_path):
    """Scan target_path for tech stack signals.

    Returns a list of (stack_id, confidence, signal_text) tuples.
    confidence: high = 3pts, medium = 2pts, low = 1pt when aggregating.
    """
    signals = []

    # --- Mobile - iOS ------------------------------------------------------
    if _has_any_prefix(target_path, [".xcodeproj", ".xcworkspace"]):
        signals.append(("mobile-ios", "high", "xcodeproj/xcworkspace"))
    if _file_exists(target_path, "Podfile"):
        signals.append(("mobile-ios", "high", "Podfile"))
    if _file_exists(target_path, "Package.swift"):
        signals.append(("mobile-ios", "medium", "Package.swift"))
    # CocoaPods 组件（monorepo 子模块场景：Podfile/.xcodeproj 在宿主层，子模块只带 podspec）
    if _has_any_suffix(target_path, [".podspec", ".podspec.json"]):
        signals.append(("mobile-ios", "high", "*.podspec"))
    if _file_exists(target_path, "Info.plist"):
        signals.append(("mobile-ios", "medium", "Info.plist"))
    # 任意 .swift / .m / .h 源文件（顶层或一层子目录）
    if _count_files(target_path, [".swift", ".m", ".h"], max_depth=2) > 0:
        signals.append(("mobile-ios", "medium", "Swift/ObjC sources"))

    # --- Mobile - Android --------------------------------------------------
    if _file_exists(target_path, "build.gradle") or _file_exists(target_path, "build.gradle.kts"):
        signals.append(("mobile-android", "high", "build.gradle"))
    if _file_exists(target_path, "settings.gradle") or _file_exists(target_path, "settings.gradle.kts"):
        signals.append(("mobile-android", "high", "settings.gradle"))

    # --- Backend - GDP & Go ------------------------------------------------
    if _file_exists(target_path, "go.mod"):
        signals.append(("backend-go", "medium", "go.mod"))
    if _dir_exists(target_path, ".gdp"):
        signals.append(("backend-gdp", "high", ".gdp directory"))
    if _dir_exists(target_path, "service") and _dir_exists(target_path, "pkg"):
        signals.append(("backend-gdp", "medium", "service/ + pkg/ layout"))
    gdp_dirs = ["action", "service/domain", "service/dal", "service/dao"]
    gdp_count = sum(1 for d in gdp_dirs if _dir_exists(target_path, d))
    if gdp_count >= 2:
        signals.append(("backend-gdp", "high", f"GDP layout ({gdp_count} marker dirs)"))

    # Kitex / Hertz (Go ecosystem)
    go_mod_content = _read_file(target_path, "go.mod")
    if go_mod_content:
        eco = []
        if "kitex" in go_mod_content:
            eco.append("kitex")
        if "hertz" in go_mod_content:
            eco.append("hertz")
        if eco:
            signals.append(("backend-go", "medium", f"Go ecosystem: {', '.join(eco)}"))

    # --- Lynx --------------------------------------------------------------
    if _has_any_prefix(target_path, ["lynx.config.", "lynx.json"]):
        signals.append(("lynx", "high", "lynx config file"))

    # --- Web ---------------------------------------------------------------
    if _file_exists(target_path, "package.json"):
        pkg = _read_json(target_path, "package.json")
        has_react = False
        has_vue = False
        if pkg and isinstance(pkg, dict) and "dependencies" in pkg:
            deps = pkg["dependencies"] or {}
            if isinstance(deps, dict):
                if "react" in deps:
                    has_react = True
                if "vue" in deps:
                    has_vue = True

        web_sig = ["package.json"]
        if _has_any_prefix(target_path, ["vite.config."]):
            web_sig.append("vite")
        if _has_any_prefix(target_path, ["next.config."]):
            web_sig.append("next.js")
        if has_react:
            web_sig.append("react")
        if has_vue:
            web_sig.append("vue")

        confidence = "medium"
        if len(web_sig) >= 3:
            confidence = "high"
        signals.append(("web", confidence, f"Web signals: {', '.join(web_sig)}"))

    return signals


def _has_any_prefix(target_path, prefixes):
    try:
        for entry in os.listdir(target_path):
            for prefix in prefixes:
                if entry.startswith(prefix):
                    return True
    except (OSError, PermissionError):
        pass
    return False


def _has_any_suffix(target_path, suffixes):
    try:
        for entry in os.listdir(target_path):
            for suffix in suffixes:
                if entry.endswith(suffix):
                    return True
    except (OSError, PermissionError):
        pass
    return False


def _count_files(target_path, suffixes, max_depth=2):
    """Count files matching any suffix up to max_depth directories deep."""
    count = 0
    target_path = os.path.abspath(target_path)
    for root, dirs, files in os.walk(target_path):
        rel = os.path.relpath(root, target_path)
        # depth: "." = 0, "a" = 1, "a/b" = 2, …
        depth = 0 if rel == "." else rel.count(os.sep) + 1
        if depth > max_depth:
            dirs.clear()  # prune descent
            continue
        for f in files:
            for sfx in suffixes:
                if f.endswith(sfx):
                    count += 1
                    break
        if count > 0:
            # We only care about presence; early-exit to keep detection cheap
            return count
    return count


def _file_exists(target_path, filename):
    return os.path.isfile(os.path.join(target_path, filename))


def _dir_exists(target_path, dirname):
    return os.path.isdir(os.path.join(target_path, dirname))


def _read_file(target_path, filename):
    path = os.path.join(target_path, filename)
    try:
        with open(path, "r") as f:
            return f.read()
    except (OSError, PermissionError):
        return None


def _read_json(target_path, filename):
    content = _read_file(target_path, filename)
    if content:
        try:
            return json.loads(content)
        except json.JSONDecodeError:
            pass
    return None


def determine_stack(signals):
    """Pick the best matching stack from a list of signals.

    Returns (best_stack, overall_confidence, winning_signal_texts).
    If no signals, falls back to generic with low confidence.
    """
    if not signals:
        return "generic", "low", []

    score_w = {"high": 3, "medium": 2, "low": 1}
    stack_scores = {}
    stack_signals = {}
    for stack, conf, text in signals:
        stack_scores[stack] = stack_scores.get(stack, 0) + score_w.get(conf, 1)
        stack_signals.setdefault(stack, []).append(text)

    sorted_stacks = sorted(stack_scores.items(), key=lambda x: x[1], reverse=True)
    best_stack, best_score = sorted_stacks[0]

    if best_score >= 3:
        overall_confidence = "high"
    elif best_score >= 2:
        overall_confidence = "medium"
    else:
        overall_confidence = "low"

    # iOS + Android simultaneously: pick the stronger one; still high conf if both strong
    has_ios = any(s[0] == "mobile-ios" for s in signals)
    has_android = any(s[0] == "mobile-android" for s in signals)
    if has_ios and has_android:
        ios_s = stack_scores.get("mobile-ios", 0)
        android_s = stack_scores.get("mobile-android", 0)
        best_stack = "mobile-android" if android_s >= ios_s else "mobile-ios"
        overall_confidence = "high" if min(ios_s, android_s) >= 3 else "medium"

    # If GDP signals exist AND we picked backend-go due to score tie, prefer GDP
    if best_stack == "backend-go" and "backend-gdp" in stack_scores and stack_scores["backend-gdp"] >= 3:
        best_stack = "backend-gdp"
        overall_confidence = "medium" if stack_scores["backend-gdp"] < 4 else "high"

    return best_stack, overall_confidence, stack_signals.get(best_stack, [])


def main():
    if len(sys.argv) < 2:
        print(json.dumps({"error": "target_path required"}), file=sys.stderr)
        sys.exit(1)

    target_path = os.path.abspath(sys.argv[1])
    if not os.path.isdir(target_path):
        print(json.dumps({"error": f"target_path not a directory: {target_path}"}), file=sys.stderr)
        sys.exit(1)

    signals = detect_signals(target_path)
    stack, confidence, signal_texts = determine_stack(signals)

    result = {
        "stack": stack,
        "confidence": confidence,
        "platform": stack_config.platform(stack),
        "display_name": stack_config.display_name(stack, "zh"),
        "evaluator": stack_config.evaluator_skill(stack),
        "adapter": stack_config.adapter_type(stack),
        "signals": signal_texts,
        "all_signals": [
            {"stack": s[0], "confidence": s[1], "detail": s[2]} for s in signals
        ],
        "target_path": target_path,
    }

    print(json.dumps(result, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
