#!/usr/bin/env python3
"""Compile .dlg plain-text dialogue scripts into YAML for the FengZhi dialogue system.

Usage:
    python tools/dialogue_compiler.py path/to/dialogue.dlg [-o output.yaml]

If -o is omitted the YAML is printed to stdout.
"""
from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path

# ---------------------------------------------------------------------------
# Data model
# ---------------------------------------------------------------------------

@dataclass
class RawEvent:
    type: str
    params: dict[str, str | int]


@dataclass
class RawOption:
    text: str
    children: list[RawNode] = field(default_factory=list)
    events: list[RawEvent] = field(default_factory=list)


@dataclass
class RawNode:
    type: str  # narration | inner_monologue | speech | choice
    text: str = ""
    speaker: str | None = None
    options: list[RawOption] | None = None
    events: list[RawEvent] = field(default_factory=list)
    condition: str | None = None  # "day" | "night"


# ---------------------------------------------------------------------------
# Parser
# ---------------------------------------------------------------------------

_NODE_PATTERNS: list[tuple[re.Pattern[str], str, str | None]] = [
    (re.compile(r"^旁白[：:](.+)"), "narration", None),
    (re.compile(r"^内心[：:](.+)"), "inner_monologue", None),
    (re.compile(r"^对白[（(](.+?)[）)][：:](.+)"), "speech", "speaker"),
    (re.compile(r"^选择[：:](.+)"), "choice", None),
]

_CONDITION_RE = re.compile(r"^\[([日夜])\]\s*")
_EVENT_RE = re.compile(r"^>\s*(\S+)\s+(.*)")
_OPTION_RE = re.compile(r"^-\s+(.+)")
_ID_RE = re.compile(r"^#\s*(\S+)")


def _parse_event(line: str) -> RawEvent | None:
    m = _EVENT_RE.match(line)
    if not m:
        return None
    cmd = m.group(1)
    rest = m.group(2).strip()
    if cmd == "mindset":
        parts = rest.split()
        if len(parts) >= 2:
            return RawEvent("mindset_shift", {"axis": parts[0], "delta": int(parts[1])})
    elif cmd == "flag":
        parts = rest.split(maxsplit=1)
        if len(parts) >= 2:
            return RawEvent("quest_flag", {"key": parts[0], "value": parts[1]})
    return None


def _match_node(text: str) -> RawNode | None:
    for pattern, node_type, _ in _NODE_PATTERNS:
        m = pattern.match(text)
        if not m:
            continue
        if node_type == "speech":
            return RawNode(type="speech", speaker=m.group(1).strip(), text=m.group(2).strip())
        if node_type == "choice":
            return RawNode(type="choice", text=m.group(1).strip(), options=[])
        return RawNode(type=node_type, text=m.group(1).strip())
    return None


def parse_dlg(source: str) -> tuple[str, list[RawNode]]:
    lines = source.splitlines()
    dialogue_id = "untitled"
    nodes: list[RawNode] = []

    i = 0
    while i < len(lines):
        raw = lines[i]
        stripped = raw.strip()
        i += 1

        if not stripped or stripped == "---":
            continue

        id_match = _ID_RE.match(stripped)
        if id_match:
            dialogue_id = id_match.group(1)
            continue

        condition: str | None = None
        cm = _CONDITION_RE.match(stripped)
        if cm:
            condition = "day" if cm.group(1) == "日" else "night"
            stripped = stripped[cm.end():]

        ev = _parse_event(stripped)
        if ev is not None:
            _attach_event(nodes, ev)
            continue

        node = _match_node(stripped)
        if node is None:
            continue
        node.condition = condition

        if node.type == "choice" and node.options is not None:
            i = _parse_options(lines, i, node)

        nodes.append(node)

    return dialogue_id, nodes


def _attach_event(nodes: list[RawNode], ev: RawEvent) -> None:
    if not nodes:
        return
    last = nodes[-1]
    if last.type == "choice" and last.options:
        last.options[-1].events.append(ev)
    else:
        last.events.append(ev)


def _parse_options(lines: list[str], i: int, choice_node: RawNode) -> int:
    assert choice_node.options is not None

    while i < len(lines):
        raw = lines[i]
        stripped = raw.strip()

        if not stripped:
            i += 1
            continue

        om = _OPTION_RE.match(stripped)
        if om:
            choice_node.options.append(RawOption(text=om.group(1).strip()))
            i += 1
            i = _parse_option_body(lines, i, choice_node.options[-1])
            continue

        if _is_top_level_content(stripped):
            break

        i += 1

    return i


def _parse_option_body(lines: list[str], i: int, option: RawOption) -> int:
    while i < len(lines):
        raw = lines[i]
        stripped = raw.strip()

        if not stripped:
            i += 1
            continue

        if _OPTION_RE.match(stripped):
            break

        if _is_top_level_content(stripped):
            content = stripped
            cm = _CONDITION_RE.match(content)
            if cm:
                content = content[cm.end():]
            if not content.startswith(("  ", "\t")) and _match_node(content) and not raw.startswith(("    ", "\t\t")):
                break

        ev = _parse_event(stripped)
        if ev is not None:
            if option.children:
                option.children[-1].events.append(ev)
            else:
                option.events.append(ev)
            i += 1
            continue

        node = _match_node(stripped)
        if node:
            option.children.append(node)
            i += 1
            continue

        break

    return i


def _is_top_level_content(s: str) -> bool:
    test = s
    cm = _CONDITION_RE.match(test)
    if cm:
        test = test[cm.end():]
    return _match_node(test) is not None or _ID_RE.match(test) is not None


# ---------------------------------------------------------------------------
# Compiler
# ---------------------------------------------------------------------------

_TYPE_SHORT = {
    "narration": "narration",
    "inner_monologue": "inner",
    "speech": "speech",
    "choice": "choice",
}


@dataclass
class CompiledNode:
    id: str
    type: str
    text: str | None = None
    speaker: str | None = None
    prompt: str | None = None
    next: str | None = None
    fallback: str | None = None
    options: list[dict] | None = None
    events: list[dict] | None = None
    conditions: list[dict] | None = None


def compile_dialogue(dialogue_id: str, raw_nodes: list[RawNode]) -> dict:
    counters: dict[str, int] = {}
    all_compiled: list[CompiledNode] = []

    def next_id(node_type: str, prefix: str = "") -> str:
        short = _TYPE_SHORT.get(node_type, node_type)
        key = f"{prefix}{short}"
        counters[key] = counters.get(key, 0) + 1
        return f"{prefix}{short}_{counters[key]:02d}"

    def compile_events(events: list[RawEvent]) -> list[dict] | None:
        if not events:
            return None
        result = []
        for ev in events:
            d: dict = {"type": ev.type}
            d.update(ev.params)
            result.append(d)
        return result

    def compile_condition(cond: str) -> list[dict]:
        value = cond
        return [{"source": "flag", "key": "variant", "op": "eq", "value": value}]

    main_ids: list[str] = []
    pending_day_node: CompiledNode | None = None

    for raw in raw_nodes:
        node_id = next_id(raw.type)
        main_ids.append(node_id)

        cn = CompiledNode(id=node_id, type=raw.type)

        if raw.type == "choice":
            cn.prompt = raw.text
            cn.options = []
            if raw.options:
                for oi, opt in enumerate(raw.options):
                    opt_entry: dict = {"text": opt.text}
                    if opt.children:
                        branch_ids: list[str] = []
                        for child in opt.children:
                            child_id = next_id(child.type, f"opt{oi + 1}_")
                            branch_ids.append(child_id)
                            child_cn = CompiledNode(
                                id=child_id,
                                type=child.type,
                                text=child.text,
                                speaker=child.speaker if child.type == "speech" else None,
                                events=compile_events(child.events),
                            )
                            all_compiled.append(child_cn)
                        for j in range(len(branch_ids) - 1):
                            all_compiled_by_id(all_compiled, branch_ids[j]).next = branch_ids[j + 1]
                        all_compiled_by_id(all_compiled, branch_ids[-1]).next = "END"
                        opt_entry["next"] = branch_ids[0]
                    else:
                        opt_entry["next"] = "END"

                    opt_events = compile_events(opt.events)
                    if opt_events:
                        opt_entry["events"] = opt_events

                    cn.options.append(opt_entry)
        else:
            cn.text = raw.text
            if raw.type == "speech":
                cn.speaker = raw.speaker

        cn.events = compile_events(raw.events)

        if raw.condition == "day":
            cn.conditions = compile_condition("day")
            pending_day_node = cn
        elif raw.condition == "night":
            cn.conditions = compile_condition("night")
            if pending_day_node is not None:
                pending_day_node.fallback = cn.id
                pending_day_node = None

        all_compiled.append(cn)

    top_level = [c for c in all_compiled if c.id in main_ids]
    night_ids: set[str] = set()
    for j, tl in enumerate(top_level):
        if tl.fallback:
            night_ids.add(tl.fallback)

    for j in range(len(top_level) - 1):
        if top_level[j].next is not None:
            continue
        k = j + 1
        if top_level[j].id in night_ids or top_level[j].fallback:
            while k < len(top_level) and top_level[k].id in night_ids:
                k += 1
        top_level[j].next = top_level[k].id if k < len(top_level) else "END"
    if top_level and top_level[-1].next is None:
        top_level[-1].next = "END"

    for cn in all_compiled:
        if cn.type == "choice" and cn.next is None:
            cn.next = "END"

    entry = all_compiled[0].id if all_compiled else "narration_01"

    return {
        "id": dialogue_id,
        "version": 1,
        "entry_node": entry,
        "nodes": all_compiled,
    }


def all_compiled_by_id(nodes: list[CompiledNode], node_id: str) -> CompiledNode:
    for n in nodes:
        if n.id == node_id:
            return n
    raise ValueError(f"Node not found: {node_id}")


# ---------------------------------------------------------------------------
# YAML emitter
# ---------------------------------------------------------------------------

def _yaml_str(s: str) -> str:
    escaped = s.replace("\\", "\\\\").replace('"', '\\"')
    return f'"{escaped}"'


def emit_yaml(data: dict) -> str:
    lines: list[str] = []
    lines.append(f"id: {data['id']}")
    lines.append(f"version: {data['version']}")
    lines.append(f"entry_node: {data['entry_node']}")
    lines.append("nodes:")

    nodes: list[CompiledNode] = data["nodes"]
    for ni, node in enumerate(nodes):
        if ni > 0:
            lines.append("")
        lines.append(f"  - id: {node.id}")
        lines.append(f"    type: {node.type}")

        if node.speaker:
            lines.append(f"    speaker: {_yaml_str(node.speaker)}")

        if node.text and node.type != "choice":
            lines.append(f"    text: {_yaml_str(node.text)}")

        if node.prompt:
            lines.append(f"    prompt: {_yaml_str(node.prompt)}")

        if node.conditions:
            lines.append("    conditions:")
            for cond in node.conditions:
                lines.append(f"      - source: {cond['source']}")
                lines.append(f"        key: {cond['key']}")
                lines.append(f"        op: {cond['op']}")
                lines.append(f"        value: {cond['value']}")

        if node.options:
            lines.append("    options:")
            for opt in node.options:
                lines.append(f"      - text: {_yaml_str(opt['text'])}")
                lines.append(f"        next: {opt.get('next', 'END')}")
                if "events" in opt:
                    lines.append("        events:")
                    for ev in opt["events"]:
                        _emit_event(lines, ev, indent=10)

        if node.events:
            lines.append("    events:")
            for ev in node.events:
                _emit_event(lines, ev, indent=6)

        if node.fallback:
            lines.append(f"    fallback: {node.fallback}")

        if node.next and node.type != "choice":
            lines.append(f"    next: {node.next}")
        elif node.type == "choice" and node.next:
            lines.append(f"    next: {node.next}")

    lines.append("")
    return "\n".join(lines)


def _emit_event(lines: list[str], ev: dict, indent: int) -> None:
    pad = " " * indent
    lines.append(f"{pad}- type: {ev['type']}")
    if ev["type"] == "mindset_shift":
        lines.append(f"{pad}  axis: {ev['axis']}")
        lines.append(f"{pad}  delta: {ev['delta']}")
    elif ev["type"] == "quest_flag":
        lines.append(f"{pad}  key: {ev['key']}")
        lines.append(f"{pad}  value: {_yaml_str(str(ev['value']))}")


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(description="Compile .dlg dialogue scripts to YAML.")
    parser.add_argument("input", type=Path, help="Path to .dlg file")
    parser.add_argument("-o", "--output", type=Path, default=None, help="Output YAML path (default: stdout)")
    args = parser.parse_args()

    if not args.input.exists():
        print(f"Error: {args.input} not found", file=sys.stderr)
        sys.exit(1)

    source = args.input.read_text(encoding="utf-8")
    dialogue_id, raw_nodes = parse_dlg(source)

    if not raw_nodes:
        print("Error: no dialogue nodes found in input", file=sys.stderr)
        sys.exit(1)

    data = compile_dialogue(dialogue_id, raw_nodes)
    yaml_text = emit_yaml(data)

    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(yaml_text, encoding="utf-8")
        print(f"Written: {args.output}")
    else:
        print(yaml_text)


if __name__ == "__main__":
    main()
