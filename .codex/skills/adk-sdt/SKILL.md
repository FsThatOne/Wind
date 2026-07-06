---
name: adk-sdt
description: "Use when the user wants to run the SDT testing workflow but has not specified a stage. Routes to adk-sdt-ff, adk-sdt-clarify, or adk-sdt-implement based on feature directory state and user intent."
---

# ADK SDT

This skill is the SDT entrypoint and shared resource package. It should route the user to the right SDT stage without
exposing internal skill selection details.

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding.

## Runtime Assets

Resolve the installed `adk-sdt` skill's resource directory with the host's skill mechanism, then set `SDT_ASSET_ROOT` to its `resources` directory. Do not look for these assets in plugin source directories or legacy core resource directories.

If host lookup cannot resolve `adk-sdt`, fall back to `.claude/skills/adk-sdt/resources`, then `.cursor/skills/adk-sdt/resources` under the project root.

The stage skills must use this same asset root for scripts, profiles, and templates.

## Stage Skill Resolution

Select and hand off to stage skills by skill name through the host's skill mechanism.

If host lookup cannot resolve a skill by name, fall back to reading `<skillName>/SKILL.md` under the project-root `.claude/skills`, then `.cursor/skills`.

Stages:

- `adk-sdt-ff`: generate test cases and `test/task.md`
- `adk-sdt-clarify`: review and update generated test cases
- `adk-sdt-implement`: execute `test/task.md` and generate report

If a selected stage skill is not invocable as a tool, use the host-provided skill lookup/read mechanism and execute that workflow directly.

## Experience Query Before User Questions

Before this entrypoint asks the user any structured or free-form question, it **MUST** first query `adk-sdt-learning` with the current `$ARGUMENTS`, resolved feature candidates, available files, and the question it is about to ask.

- Resolve `adk-sdt-learning` first using the host skill lookup rules. If the skill itself is not installed, skip experience lookup and proceed with the original question.
- If `adk-sdt-learning` is installed but the learning repository is not initialized, run its Bootstrap operation automatically once, then retry the Query.
- If Bootstrap fails, stop and ask the user to fix learning repository initialization.
- If the experience query confidently resolves the ambiguity, use that answer and do not ask the user.
- If the experience query provides partial guidance, include the relevant guidance when asking the user.
- If no relevant experience is found, proceed with the original question.
- Do not expose internal skill routing details; only surface the business/test guidance that affects the user's choice.

## Routing Workflow

1. Inspect `$ARGUMENTS`:
   - If it explicitly mentions `ff`, `fast forward`, `generate case`, or `生成用例`, route to `adk-sdt-ff`.
   - If it explicitly mentions `clarify`, `review case`, `adjust case`, `澄清`, `修改用例`, or `确认用例`, route to `adk-sdt-clarify`.
   - If it explicitly mentions `implement`, `execute`, `run test`, `rerun`, `retest`, `执行`, `跑测试`, or `复测`, route to `adk-sdt-implement`.
2. If no explicit stage is found, resolve `FEATURE_DIR`:
   - Run `node "$SDT_ASSET_ROOT/scripts/check-prerequisites.js" --paths-only --json` from repo root.
   - If `$ARGUMENTS` provides a feature directory, append `--feature-dir "<EXPLICIT_FEATURE_DIR>"`.
   - If resolution fails or multiple feature directories require selection, first run **Experience Query Before User Questions**. Only use the host-compatible structured question tool when experience cannot determine the target feature.
3. Inspect feature test files:
   - If `FEATURE_DIR/test/case.md` is missing, route to `adk-sdt-ff`.
   - If `FEATURE_DIR/test/case.md` exists and `FEATURE_DIR/test/task.md` is missing, route to `adk-sdt-ff`.
   - If both files exist and `$ARGUMENTS` is empty or asks for validation/review, route to `adk-sdt-clarify`.
   - If both files exist and there is clear execute/rerun intent, route to `adk-sdt-implement`.
4. If state and intent remain ambiguous, first run **Experience Query Before User Questions**. Only ask one structured question if experience cannot resolve the next step:
   - "What would you like to do next in SDT?"
   - Options: Generate/update test cases (`adk-sdt-ff`), Clarify/review test cases (`adk-sdt-clarify`), Execute tests (`adk-sdt-implement`).

## Execution Rules

- Preserve `$ARGUMENTS` when handing off to the selected stage.
- Do not display internal profile or skill mapping details unless the user explicitly asks for debugging information.
- Do not copy resources into stage skill directories. Stage skills must read shared assets from `adk-sdt/resources`.
- If `adk-sdt/resources` is missing in a standalone installation, stop with an actionable message asking the user to install or sync the `adk-sdt` skill package.
