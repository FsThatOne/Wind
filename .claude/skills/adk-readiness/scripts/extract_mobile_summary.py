#!/usr/bin/env python3
"""
Extract evaluator summary from a Mobile AI friendliness Markdown report.

Parses the report produced by `tiktok-mobile-ai-friendliness` into the
standard EvaluatorResult JSON contract.

Usage:
    python3 extract_mobile_summary.py \
      --report <report.md> \
      --output <output-dir>/_evaluator_result.json \
      --platform <android|ios> \
      --mode <quick|deep>
"""

import argparse
import json
import os
import re
import sys
from datetime import datetime


def parse_report(report_path, platform, mode):
    with open(report_path, "r") as f:
        content = f.read()

    result = {
        "evaluator": f"mobile-{platform}",
        "version": "1.0.0",
        "mode": mode,
        "timestamp": datetime.now().isoformat(),
        "score": None,
        "level": None,
        "summary": "",
        "reportPath": os.path.basename(report_path),
        "dataPath": None,
    }

    # --- Score extraction ---------------------------------------------------
    # Try a range of patterns. Note: we NO LONGER auto-normalize scores that
    # look like they might be out of 10. The evaluator output is expected to
    # be out of 100; if a pattern captures something outside that range we
    # keep it as-is.
    score_patterns = [
        r"总分[：:]\s*\*{0,2}(\d+(?:\.\d+)?)\*{0,2}",
        r"总得分[：:]\s*\*{0,2}(\d+(?:\.\d+)?)\*{0,2}",
        r"综合得分[：:]\s*\*{0,2}(\d+(?:\.\d+)?)\*{0,2}",
        r"Score[：:]\s*\*{0,2}(\d+(?:\.\d+)?)\*{0,2}",
        r"\*{0,2}(\d+(?:\.\d+)?)\s*分\*{0,2}",
        r"综合得分.*?(\d+(?:\.\d+)?)\s*/\s*100",
    ]

    for pat in score_patterns:
        m = re.search(pat, content)
        if m:
            try:
                result["score"] = round(float(m.group(1)), 1)
                break
            except ValueError:
                continue

    # --- Level extraction ---------------------------------------------------
    level_patterns = [
        r"等级[：:]\s*([A-E][+-]?)",
        r"Level[：:]\s*([A-E][+-]?)",
        r"等级[：:]\s*(L[1-5])",
        r"Level[：:]\s*(L[1-5])",
        r"\*\*(L[1-5])\*\*",
    ]
    for pat in level_patterns:
        m = re.search(pat, content)
        if m:
            result["level"] = m.group(1)
            break

    # --- Summary extraction -------------------------------------------------
    summary_patterns = [
        r"##\s*摘要\n+(.*?)\n##",
        r"##\s*概览\n+(.*?)\n##",
        r"##\s*评估概览\n+(.*?)\n##",
        r"##\s*Summary\n+(.*?)\n##",
    ]
    for pat in summary_patterns:
        m = re.search(pat, content, re.DOTALL)
        if m:
            summary = m.group(1).strip()
            summary = re.sub(r"[#*`]", "", summary)
            summary = re.sub(r"\s+", " ", summary)
            if len(summary) > 200:
                summary = summary[:197] + "..."
            result["summary"] = summary
            break

    if not result["summary"]:
        score_txt = f"{result['score']}分" if result["score"] is not None else "评估完成"
        result["summary"] = f"{platform.title()} 模块 AI 友好度{score_txt}。详见完整报告。"

    # --- Timestamp from report ---------------------------------------------
    time_patterns = [
        r"评估时间[：:]\s*(\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}(?::\d{2})?)",
        r"Date[：:]\s*(\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}(?::\d{2})?)",
        r"生成时间[：:]\s*(\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}(?::\d{2})?)",
    ]
    for pat in time_patterns:
        m = re.search(pat, content)
        if not m:
            continue
        ts = m.group(1)
        try:
            if "T" in ts:
                dt = datetime.fromisoformat(ts)
            elif " " in ts:
                fmt = "%Y-%m-%d %H:%M:%S" if ts.count(":") == 2 else "%Y-%m-%d %H:%M"
                dt = datetime.strptime(ts, fmt)
            else:
                dt = datetime.strptime(ts, "%Y-%m-%d")
            result["timestamp"] = dt.isoformat()
        except ValueError:
            pass
        break

    return result


def main():
    parser = argparse.ArgumentParser(description="Extract summary from Mobile AI report")
    parser.add_argument("--report", required=True, help="Path to ai_friendly_report.md")
    parser.add_argument("--output", required=True, help="Output JSON path")
    parser.add_argument("--platform", default="android", help="Platform: android / ios")
    parser.add_argument("--mode", default="deep", help="Evaluation mode: quick / deep")
    parser.add_argument("--version", default="1.0.0", help="Evaluator version")

    args = parser.parse_args()

    if not os.path.isfile(args.report):
        print(f"Error: report not found: {args.report}", file=sys.stderr)
        sys.exit(1)

    result = parse_report(args.report, args.platform, args.mode)
    result["version"] = args.version

    os.makedirs(os.path.dirname(os.path.abspath(args.output)), exist_ok=True)
    with open(args.output, "w") as f:
        json.dump(result, f, indent=2, ensure_ascii=False)

    print(f"Generated: {args.output}")
    print(f"  Score:   {result['score']}")
    print(f"  Level:   {result['level']}")
    print(f"  Summary: {(result['summary'] or '')[:60]}...")


if __name__ == "__main__":
    main()
