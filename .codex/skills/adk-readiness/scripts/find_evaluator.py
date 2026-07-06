#!/usr/bin/env python3
"""
Evaluator lookup for adk-readiness.

Does NOT scan the filesystem for skills. Its job is purely to translate a
stack id (or explicit evaluator skill name) into:
  - the evaluator skill name to invoke via the Skill() mechanism
  - the adapter type for per-stack parameter adaptation
  - install guidance in case the evaluator is missing

Skill discovery is delegated to the agent: it first tries to invoke the
returned evaluator_skill via Skill(). If unavailable it prompts the user
with the install guidance and/or offers the fallback path.

Usage:
    python3 find_evaluator.py <stack_id>
    python3 find_evaluator.py --skill-name <evaluator_skill_name>

Output JSON:
{
    "stack": "mobile-android" | null,
    "evaluator_skill": "tiktok-mobile-ai-friendliness" | null,
    "adapter": "mobile" | "gdp" | "generic" | "fallback",
    "has_evaluator": true/false,
    "install": null | {"method": "...", "command": "...", "hint": "..."},
    "display_name": "Mobile / Android"
}
"""

import argparse
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import stack_config  # noqa: E402


def _stack_lookup(stack_id, language="zh"):
    """Build a lookup result from a stack id (known or unknown)."""
    return {
        "stack": stack_id,
        "evaluator_skill": stack_config.evaluator_skill(stack_id),
        "adapter": stack_config.adapter_type(stack_id),
        "has_evaluator": stack_config.evaluator_skill(stack_id) is not None,
        "install": stack_config.install_info(stack_id),
        "display_name": stack_config.display_name(stack_id, language),
    }


def _skill_name_lookup(skill_name, language="zh"):
    """Resolve a direct evaluator skill name.

    If we can map it back to a known stack id, we carry that metadata
    along. Otherwise the result has stack=null and adapter=generic; the
    agent should invoke the skill directly and adapt the output contract
    as best it can.
    """
    matched_stack = None
    for sid in stack_config.all_stack_ids():
        if stack_config.evaluator_skill(sid) == skill_name:
            matched_stack = sid
            break

    if matched_stack:
        result = _stack_lookup(matched_stack, language)
        return result

    return {
        "stack": None,
        "evaluator_skill": skill_name,
        "adapter": "generic",
        "has_evaluator": True,
        "install": {
            "method": "manual",
            "command": f"skills add {skill_name}",
            "hint": f"通过 skill CLI 或 ttadk plugin 安装 {skill_name}",
        },
        "display_name": skill_name,
    }


def main():
    parser = argparse.ArgumentParser(description="Evaluator skill lookup for adk-readiness")
    parser.add_argument(
        "stack",
        nargs="?",
        default=None,
        help="Stack id (mobile-android / backend-gdp / ...). Ignored when --skill-name is set.",
    )
    parser.add_argument(
        "--skill-name",
        help="Direct evaluator skill name (overrides stack-based lookup).",
    )
    parser.add_argument("--language", default="zh", help="display language for labels: zh|en")

    args = parser.parse_args()
    language = args.language if args.language in ("zh", "en") else "zh"

    if args.skill_name:
        result = _skill_name_lookup(args.skill_name, language)
    elif args.stack:
        result = _stack_lookup(args.stack, language)
    else:
        print(json.dumps({"error": "either <stack> or --skill-name is required"}), file=sys.stderr)
        sys.exit(1)

    print(json.dumps(result, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
