---
name: adk-readiness-fallback
description: "Fallback AI readiness assessment for generic projects. Used when tech stack cannot be detected or the corresponding evaluator is not installed. Provides a lightweight baseline scan. Results are for reference only; install a stack-specific evaluator for accurate results."
---

# AI Readiness 通用 Fallback 评估

> **通用模式** — 本评估是轻量级基线扫描，结果仅供参考。
> 建议安装对应技术栈的评估 skill 以获得准确、深入的评估结果。

## 输入

```text
$ARGUMENTS
```

Parse the argument string for these keys (space or newline separated, key=value format):

| key        | type   | required | description |
|------------|--------|----------|-------------|
| targetPath | string | yes      | Absolute path of the project directory to assess |
| outputDir  | string | yes      | Absolute path where outputs must be written |
| mode       | string | no       | `quick` / `deep`.  Fallback only supports quick; default `quick` |
| language   | string | no       | `zh` / `en`.  Default `zh` |

## Scope

The fallback evaluator performs basic file-level scans only. It does NOT do any
code-level analysis.

Dimensions checked:
1. **Project metadata** — README, LICENSE, overall project layout
2. **AI context files** — `CLAUDE.md`, `.cursorrules`, `AGENTS.md`, `.cursor/rules/`
3. **Documentation basics** — presence of a `docs/` directory or equivalent
4. **Testing basics** — presence of test files / test directories
5. **CI / CD** — presence of CI configuration
6. **Code organization** — clear top-level directory structure

It explicitly does NOT assess: code complexity, naming quality, architecture
soundness, dependency quality, or any other dimension requiring code
understanding.

---

## Execution

### Step 1 — Scan project structure

Read the top-level contents of `targetPath` and check for the presence of each
item below.  Record each check as either passed (文件存在) or failed.

| # | Check | Look for |
|---|-------|----------|
| 1 | README | `README.md`, `README`, `README.*` |
| 2 | AI context files | `CLAUDE.md`, `.cursorrules`, `AGENTS.md`, `.cursor/rules/` |
| 3 | Documentation | `docs/` or an equivalent markdown-heavy directory |
| 4 | Testing | `tests/`, `test/`, `__tests__/`, files matching `*_test.*`, files matching `*.spec.*` |
| 5 | CI / CD | `.github/`, `.gitlab-ci.yml`, `Jenkinsfile`, `Makefile`, `.drone.yml`, `.ci/`, `.buildkite/` |
| 6 | Project manifest | `package.json`, `go.mod`, `pyproject.toml`, `Cargo.toml`, `pom.xml`, `Podfile`, `build.gradle*`, `settings.gradle*` |

### Step 2 — Scoring & level

Each of the 6 dimensions is worth **1 point**; the raw total ranges from 0 to 6.

| Total (0–6) | Level | Chinese summary |
|-------------|-------|-----------------|
| 0–1         | L1    | 基础配置缺失严重 |
| 2–3         | L2    | 有基本项目结构 |
| 4–5         | L3    | 项目组织较完善 |
| 6           | L4    | 基础配置齐全 |

IMPORTANT: Do NOT convert this 0–6 score to a 100-point scale.  The
`_evaluator_result.json` must report the **raw score (0–6)** and the
corresponding level.  Entry pages are expected to display this as-is.

### Step 3 — Generate the Markdown report

Write `report.md` to `outputDir`.  Structure:

```
# AI Readiness 评估报告（通用模式）

> **通用模式** — 本报告为轻量级基线扫描，结果仅供参考。
> 建议安装对应技术栈的评估 skill 以获得深入、准确的评估。

## 基本信息
- 评估时间：<ISO timestamp>
- 目标路径：<targetPath>
- 评估范围：通用 fallback 文件级扫描

## 评估结果
- 总分：X / 6
- 等级：LX — <level description>

## 检查详情
| 维度 | 状态 | 说明 |
|------|------|------|
| README | 通过 / 未通过 | <一句话说明> |
| AI 上下文 | 通过 / 未通过 | ... |
| 文档 | 通过 / 未通过 | ... |
| 测试 | 通过 / 未通过 | ... |
| CI/CD | 通过 / 未通过 | ... |
| 项目结构 | 通过 / 未通过 | ... |

## 建议
1. 为项目添加 README 说明仓库用途与启动方式
2. 添加 CLAUDE.md 或 .cursorrules 帮助 AI 理解项目规范
3. ...（根据未通过项给出 1-3 条具体、可执行的建议）

## 下一步
- 安装对应技术栈评估器以获得准确评估
- 执行完通用模式后可尝试 `--stack <stack-id>` 强制指定技术栈
```

If `language=en` is specified, produce a fully English report with the same
structure and map the level labels: L1 = Missing basics, L2 = Basic structure
present, L3 = Well organized, L4 = Fully configured.

### Step 4 — Write `_evaluator_result.json`

Write to `outputDir/_evaluator_result.json`:

```json
{
  "evaluator": "generic-fallback",
  "version": "0.1.0",
  "mode": "quick",
  "timestamp": "<ISO timestamp>",
  "score": <0..6 raw integer>,
  "level": "L1" | "L2" | "L3" | "L4",
  "summary": "通用模式评估：基础配置 X/6 项通过。建议安装专业评估器获得深入结果。",
  "reportPath": "report.md",
  "dataPath": null
}
```

Reminder: `score` is the raw integer 0..6, NOT a percentage.

---

## Outputs

All outputs must be written to `outputDir` and nowhere else:
- `outputDir/report.md` — main report
- `outputDir/_evaluator_result.json` — standard evaluator result JSON
