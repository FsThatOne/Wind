#!/usr/bin/env python3
"""
History report management for adk-readiness.

Archives latest.md / latest.json / per-module _evaluator_result.json into
history/ with date-based naming, then applies a tiered retention policy.

Retention policy:
- Last 7 days: 1 per day
- 8-30 days: 1 per 3 days
- 31-90 days: 1 per week
- 91-365 days: 1 per month
- 1-2 years: 1 per quarter

Max 50 history entries.

Usage:
    python3 manage_history.py archive --output-dir <.ai-readiness dir>
                                     [--module-key <key1>] [--module-key <key2>]
    python3 manage_history.py list    --output-dir <.ai-readiness dir>
    python3 manage_history.py cleanup --output-dir <.ai-readiness dir>

Pass --module-key for each evaluated module so their _evaluator_result.json
snapshots are archived alongside latest.md / latest.json.
"""

import argparse
import json
import os
import re
import shutil
import sys
from datetime import datetime, timedelta

HISTORY_DIR = "history"
MAX_ENTRIES = 50
DETAILS_DIR = "details"


def get_history_dir(output_dir):
    return os.path.join(output_dir, HISTORY_DIR)


def _resolve_timestamp(latest_json_path):
    timestamp = datetime.now()
    if os.path.isfile(latest_json_path):
        try:
            with open(latest_json_path, "r") as f:
                data = json.load(f)
            ts = data.get("timestamp")
            if ts:
                timestamp = datetime.fromisoformat(str(ts).replace("Z", "+00:00"))
        except (json.JSONDecodeError, ValueError, OSError):
            pass
    return timestamp


def archive(output_dir, module_keys=None):
    """Archive latest entry plus per-module result JSONs to history/."""
    latest_md = os.path.join(output_dir, "latest.md")
    latest_json = os.path.join(output_dir, "latest.json")

    if not os.path.isfile(latest_md):
        print(f"Warning: latest.md not found at {latest_md}", file=sys.stderr)
        return False

    history_dir = get_history_dir(output_dir)
    os.makedirs(history_dir, exist_ok=True)

    timestamp = _resolve_timestamp(latest_json)
    date_str = timestamp.strftime("%Y-%m-%d")

    # Find a unique filename for today (supports multiple runs in one day)
    counter = 1
    stem = date_str
    history_md = os.path.join(history_dir, f"{stem}.md")
    while os.path.exists(history_md):
        counter += 1
        stem = f"{date_str}-{counter}"
        history_md = os.path.join(history_dir, f"{stem}.md")

    shutil.copy2(latest_md, history_md)
    print(f"Archived: {history_md}")

    latest_json_path = os.path.join(output_dir, "latest.json")
    if os.path.isfile(latest_json_path):
        shutil.copy2(latest_json_path, history_md.replace(".md", ".json"))

    # Snapshot per-module _evaluator_result.json files (key meta-data, no full reports)
    # --module-key list provided explicitly → use it; else auto-scan details/*
    details_root = os.path.join(output_dir, DETAILS_DIR)
    if not module_keys:
        scanned = []
        if os.path.isdir(details_root):
            for entry in sorted(os.listdir(details_root)):
                sub = os.path.join(details_root, entry)
                if os.path.isdir(sub) and os.path.isfile(
                    os.path.join(sub, "_evaluator_result.json")
                ):
                    scanned.append(entry)
        module_keys = scanned
    for mkey in module_keys:
        src = os.path.join(details_root, mkey, "_evaluator_result.json")
        if os.path.isfile(src):
            dst = os.path.join(history_dir, f"{stem}--{mkey}--result.json")
            shutil.copy2(src, dst)
            print(f"Archived: {dst}")

    # Apply retention policy
    cleanup(output_dir)
    return True


def _parse_history_entries(history_dir):
    """Parse history files, grouped by stem (date + possible counter)."""
    entries = []
    if not os.path.isdir(history_dir):
        return entries

    # Three file patterns per archive run:
    #   <stem>.md                  main entry page
    #   <stem>.json                structured meta
    #   <stem>--<module>--result.json  per-module evaluator snapshot
    md_pattern = re.compile(r"^(\d{4}-\d{2}-\d{2})(-\d+)?\.md$")

    for fname in sorted(os.listdir(history_dir)):
        m = md_pattern.match(fname)
        if not m:
            continue
        date_str = m.group(1)
        counter_suffix = m.group(2) or ""
        try:
            date = datetime.strptime(date_str, "%Y-%m-%d")
        except ValueError:
            continue

        stem = date_str + counter_suffix
        entries.append({
            "date": date,
            "date_str": date_str,
            "stem": stem,
            "md_path": os.path.join(history_dir, fname),
        })

    entries.sort(key=lambda e: e["date"], reverse=True)
    return entries


def _collect_related_files(history_dir, stem):
    """Return list of files under history_dir that belong to the same stem."""
    related = []
    prefix = stem
    try:
        for fname in os.listdir(history_dir):
            # matches <stem>.md, <stem>.json, <stem>--<module>--result.json
            if fname == f"{prefix}.md" or fname == f"{prefix}.json" or fname.startswith(
                f"{prefix}--"
            ):
                related.append(os.path.join(history_dir, fname))
    except OSError:
        pass
    return related


def cleanup(output_dir):
    """Apply retention policy; remove excess entries (all file types per stem)."""
    history_dir = get_history_dir(output_dir)
    if not os.path.isdir(history_dir):
        return

    now = datetime.now()
    entries = _parse_history_entries(history_dir)
    if not entries:
        return

    # Group by calendar day: for retention calc we keep the newest run per day
    daily = {}
    for e in entries:
        if e["date_str"] not in daily:
            daily[e["date_str"]] = e
    daily_sorted = sorted(daily.values(), key=lambda e: e["date"], reverse=True)

    keep_dates = set()
    month_kept = set()  # year-month keys for 91-365d monthly tier
    quarter_kept = set()  # year-Qn keys for 1-2y quarterly tier

    for e in daily_sorted:
        age_days = (now - e["date"]).days
        date_str = e["date_str"]

        if age_days <= 7:
            keep_dates.add(date_str)
        elif age_days <= 30:
            # keep every 3rd day, offset from start of the window
            day_of_window = age_days - 7  # 1..23
            if day_of_window % 3 == 0:
                keep_dates.add(date_str)
        elif age_days <= 90:
            # weekly: keep 1 entry per ISO-week bucket; use newest per week
            iso_year, iso_week, _ = e["date"].isocalendar()
            week_key = (iso_year, iso_week)
            # since we iterate newest first, the first time we see a week wins
            if (age_days - 30) % 7 == 0 or week_key not in [
                datetime.strptime(d, "%Y-%m-%d").isocalendar()[:2] for d in keep_dates
            ]:
                keep_dates.add(date_str)
        elif age_days <= 365:
            month_key = e["date"].strftime("%Y-%m")
            if month_key not in month_kept:
                keep_dates.add(date_str)
                month_kept.add(month_key)
        elif age_days <= 730:
            quarter = (e["date"].month - 1) // 3
            quarter_key = f"{e['date'].year}-Q{quarter + 1}"
            if quarter_key not in quarter_kept:
                keep_dates.add(date_str)
                quarter_kept.add(quarter_key)
        # older than 2 years: discard

    # Cap total entries (newest first within keep set)
    keep_list = sorted(keep_dates, reverse=True)
    if len(keep_list) > MAX_ENTRIES:
        keep_list = keep_list[:MAX_ENTRIES]
        keep_dates = set(keep_list)

    # Map date_str -> stems: a single day may have multiple stems (multiple runs)
    stems_by_date = {}
    for e in entries:
        stems_by_date.setdefault(e["date_str"], []).append(e["stem"])

    deleted = 0
    for date_str, stems in stems_by_date.items():
        if date_str in keep_dates:
            # Keep only the newest stem within the day (highest counter suffix if any)
            keep_stem = max(stems)  # lexicographic works because -2 < -10 would be wrong
            # Re-sort properly using counter suffix
            def _counter(stem):
                parts = stem.rsplit("-", 1)
                if len(parts) == 2 and parts[1].isdigit():
                    return int(parts[1])
                return 0

            keep_stem = max(stems, key=_counter)
            for stem in stems:
                if stem != keep_stem:
                    for f in _collect_related_files(history_dir, stem):
                        try:
                            os.remove(f)
                            deleted += 1
                        except OSError:
                            pass
        else:
            for stem in stems:
                for f in _collect_related_files(history_dir, stem):
                    try:
                        os.remove(f)
                        deleted += 1
                    except OSError:
                        pass

    if deleted > 0:
        print(f"Cleanup: removed {deleted} old history files")

    remaining = _parse_history_entries(history_dir)
    print(f"History entries: {len(remaining)} reports")


def list_history(output_dir):
    history_dir = get_history_dir(output_dir)
    entries = _parse_history_entries(history_dir)
    if not entries:
        print("No history entries found.")
        return
    print(f"History reports: {len(entries)}\n")
    now = datetime.now()
    for e in entries:
        age_days = (now - e["date"]).days
        age_str = f"{age_days}d ago" if age_days > 0 else "today"
        print(f"  {e['stem']:14s}  ({age_str:>8s})  {e['md_path']}")


def main():
    parser = argparse.ArgumentParser(
        description="Manage adk-readiness history",
        allow_abbrev=True,
    )
    parser.add_argument(
        "command",
        choices=["archive", "list", "cleanup"],
        help="Action to perform",
    )
    parser.add_argument(
        "--output-dir",
        required=True,
        help=".ai-readiness directory path",
    )
    parser.add_argument(
        "--module-key",
        action="append",
        default=[],
        help="Module key whose _evaluator_result.json should be snapshotted. Repeatable.",
    )

    args = parser.parse_args()

    if args.command == "archive":
        archive(args.output_dir, module_keys=args.module_key)
    elif args.command == "list":
        list_history(args.output_dir)
    elif args.command == "cleanup":
        cleanup(args.output_dir)


if __name__ == "__main__":
    main()
