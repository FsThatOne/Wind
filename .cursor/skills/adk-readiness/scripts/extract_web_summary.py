#!/usr/bin/env python3
"""
Extract evaluator summary from a Web frontend AI friendliness report.

Parses the Markdown report produced by `ai-friendly-evaluate` into the standard
EvaluatorResult JSON contract. Output format mirrors that of extract_gdp_summary.py
because the two evaluators share nearly the same report structure.

Usage:
    python3 extract_web_summary.py \
      --report <report.md> \
      --output <output-dir>/_evaluator_result.json \
      --mode <quick|deep>
"""

import argparse
import json
import os
import re
import sys
from datetime import datetime


def parse_report(report_path, mode):
    with open(report_path, "r") as f:
        content = f.read()

    result = {
        "evaluator": "web",
        "version": "1.0.0",
        "mode": mode,
        "timestamp": datetime.now().isoformat(),
        "score": None,
        "level": None,
        "summary": "",
        "reportPath": os.path.basename(report_path),
        "dataPath": None,
    }

    # --- Score --------------------------------------------------------------
    # "Corrected Total Score: **XX.X**" (English)  "修正后总分：**XX.X**" (Chinese)
    score_patterns = [
        r"Corrected Total Score[：:]\s*\*{0,2}(\d+(?:\.\d+)?)\*{0,2}",
        r"修正后总分[：:]\s*\*{0,2}(\d+(?:\.\d+)?)\*{0,2}",
        r"最终得分[：:]\s*\*{0,2}(\d+(?:\.\d+)?)\*{0,2}",
        r"总分[：:]\s*\*{0,2}(\d+(?:\.\d+)?)\*{0,2}",
        r"Total Score[：:]\s*\*{0,2}(\d+(?:\.\d+)?)\*{0,2}",
    ]
    for pat in score_patterns:
        m = re.search(pat, content)
        if m:
            try:
                result["score"] = round(float(m.group(1)), 1)
                break
            except ValueError:
                continue

    # --- Level / grade ------------------------------------------------------
    # "Final Grade: **{A|B|C|D}[+-]?**" / "最终等级：**{A|..}**"
    level_patterns = [
        r"Final Grade[：:]\s*\*{0,2}([A-D][+-]?)\*{0,2}",
        r"最终等级[：:]\s*\*{0,2}([A-D][+-]?)\*{0,2}",
        r"等级[：:]\s*\*{0,2}([A-D][+-]?)\*{0,2}",
        r"Grade[：:]\s*\*{0,2}([A-D][+-]?)\*{0,2}",
    ]
    for pat in level_patterns:
        m = re.search(pat, content)
        if m:
            result["level"] = m.group(1)
            break

    # --- Executive summary --------------------------------------------------
    summary_patterns = [
        r"##\s*Executive Summary\n+(.*?)\n##\s",
        r"##\s*执行摘要\n+(.*?)\n##\s",
        r"##\s*评估摘要\n+(.*?)\n##\s",
    ]
    for pat in summary_patterns:
        m = re.search(pat, content, re.DOTALL)
        if m:
            summary = m.group(1).strip()
            summary = re.sub(r"[#*`>]", "", summary)
            summary = re.sub(r"\s+", " ", summary)
            if len(summary) > 200:
                summary = summary[:197] + "..."
            result["summary"] = summary
            break

    # Fallback: build from key shortboards
    if not result["summary"]:
        weak_patterns = [
            r"Key Shortboards[：:]\s*(\S+(?:\s*/\s*\S+)*)",
            r"关键短板[：:]\s*(.+)",
            r"主要问题[：:]\s*(.+)",
        ]
        weak = ""
        for pat in weak_patterns:
            m = re.search(pat, content)
            if m:
                weak = m.group(1).strip()
                break

        score_txt = (
            f"{result['score']}分" if result["score"] is not None else "评估完成"
        )
        lvl_txt = f"（{result['level']}级）" if result["level"] else ""
        weak_txt = f"主要短板：{weak}" if weak else ""
        result["summary"] = f"Web 前端项目 AI 友好度{score_txt}{lvl_txt}。{weak_txt}".strip()

    # --- Timestamp ----------------------------------------------------------
    time_patterns = [
        r"Evaluation Date[：:]\s*(\d{4}-\d{2}-\d{2})",
        r"评估日期[：:]\s*(\d{4}-\d{2}-\d{2})",
        r"生成时间[：:]\s*(\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}(?::\d{2})?)",
    ]
    for pat in time_patterns:
        m = re.search(pat, content)
        if not m:
            continue
        ts = m.group(1)
        try:
            if "T" in ts or " " in ts:
                dt = datetime.fromisoformat(ts.replace(" ", "T"))
            else:
                dt = datetime.strptime(ts, "%Y-%m-%d")
            result["timestamp"] = dt.isoformat()
        except ValueError:
            pass
        break

    return result


def main():
    parser = argparse.ArgumentParser(description="Extract summary from Web AI report")
    parser.add_argument("--report", required=True, help="Path to evaluation-report.md")
    parser.add_argument("--output", required=True, help="Output JSON path")
    parser.add_argument("--mode", default="deep", help="Evaluation mode: quick / deep")
    parser.add_argument("--version", default="1.0.0", help="Evaluator version")

    args = parser.parse_args()

    if not os.path.isfile(args.report):
        print(f"Error: report not found: {args.report}", file=sys.stderr)
        sys.exit(1)

    result = parse_report(args.report, args.mode)
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
