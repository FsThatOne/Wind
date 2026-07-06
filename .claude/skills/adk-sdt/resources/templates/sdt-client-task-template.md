<!--
【语言适配规则 — 必须在生成 task.md 时执行】
当 `preferred_language` 为 `en` 时，必须将本模板中所有中文标题、描述、说明文字翻译为英文后再写入 task.md。
包括但不限于：标题（如"测试任务执行计划"→"Test Execution Plan"）、Phase 名称、Task 描述、表格列头、说明文字、标记说明等。
占位符（{{...}}）和代码块保持不变。仅翻译自然语言文本，不改变模板结构。
当 `preferred_language` 为 `zh` 时，保持中文原文不变。
-->

# 🧪 测试任务执行计划: {{FEATURE_NAME}}

---

## 📋 Input

| 参数            | 值                |
|:---------------|:------------------|
| ios_commit_id     | {{IOS_SOURCE_COMMIT}}    |
| android_commit_id     | {{ANDROID_SOURCE_COMMIT}}    |
| bits_link   | {{BITS_LINK}}  |

> 注：`bits_link` 占位符 `{{BITS_LINK}}` 的值从上下文获取，对应上游会话变量 `BITS_CASE_URL`，允许为空字符串。

---

## 🧩 Resolved Skills

> SDT 已根据 domain / profile 解析出本次执行所需的技能。下表的值**由 SDT 动态注入**；后续步骤（T002、T003 及 Input/Env 的采集指引等）引用"能力标识"时，**必须**通过本表换算成实际技能名再调用，**不得**自行猜测。

| 能力标识           | 已解析的技能（实际调用）          |
|:------------------|:----------------------------------|
| `EXEC_CASE_GENERATE`     | `{{EXEC_CASE_GENERATE_SKILL}}`          |
| `EXEC_CASE_EXECUTE`     | `{{EXEC_CASE_EXECUTE_SKILL}}`          |
| `REPORT_GENERATE`  | `{{REPORT_GENERATE_SKILL}}`       |

> 若上表中仍出现 `{{...}}` 形式的占位符（未被替换），说明 SDT 注入失败。请停止执行，并重新运行 `/qa-sdt-tasks`，或检查 `plugins/qa/core/resources/sdt-skill-profiles.json`。

---

## 📌 执行标记说明

| 标记 | 含义 |
| :--- | :--- |
| `[ ]` | 未执行 |
| `[x]` | 已完成 |
| `[P]` | 可并行执行（不同文件、无依赖） |

---

# 🚀 Phase 1: Advance Preparation

> 目标：完成测试前相关准备工作

---

## T001: 确认拉包意向

| 项目 | 内容 |
| :--- | :--- |
| 状态 | `[ ]` |
| 执行方式 | **必须使用 `AskUserQuestion` 工具**向用户确认以下两个问题 |

**需要确认的问题：**

1. **是否需要拉取客户端测试包？** — 即是否执行 T002

**用户选择与后续流程映射：**

| 用户选择 | 执行范围 | 说明 |
| :--- | :--- | :--- |
| 需要 | T002 | 执行步骤 |
| 不需要 | 无 | 跳过步骤 |


## T002: 获取测试包链接

| 项目 | 内容 |
| :--- | :--- |
| 状态 | `[ ]` |
| 执行方式 | 使用 `qg_transfer` mcp 执行查询，对 `app_id` ∈ {`1180`, `1233`} 与 `platform` ∈ {`IOS`, `ANDROID`} 的组合**按平台条件性调用**：<br>1. **平台开关**：若 `ios_commit_id` 为空（未提供 / 空字符串 / `TBD`），则跳过所有 `platform=IOS` 的组合，对应行「测试包链接」留空、「备注」填 "ios_commit_id 为空，跳过查询"；`android_commit_id` 同理。<br>2. **查询调用**：仅对 `commit_id` 非空的平台调用 mcp，每次传入对应的 `commit_id`（`platform=IOS` 用 `ios_commit_id`，`platform=ANDROID` 用 `android_commit_id`）、`platform` 与当前 `app_id`，取返回结果的第一条数据中的 `link` 作为该组合对应的测试包链接。<br>3. **失败兜底**：在需要执行的组合范围内**必须每个都调用**，任何一种查询失败（无结果 / 报错）都在对应行的「备注」里记录原因，不得以其它组合的结果顶替。<br>4. **mcp 不可用**：如果没有找到 `qg_transfer` mcp 服务器，整个 T001 直接跳过，所有行「测试包链接」留空、「备注」列统一注明 "mcp 不可用，已跳过"。 |
| 必须记录 | 4 种 `app_id` × `platform` 组合对应的测试包链接分别填入下方表格；被跳过的组合「测试包链接」必须留空，并在「备注」中说明跳过原因 |

**测试包链接记录：**

| app_id | platform | 测试包链接 | 备注 |
| :--- | :--- | :--- | :--- |
| 1180 | IOS | | |
| 1180 | ANDROID | | |
| 1233 | IOS | | |
| 1233 | ANDROID | | |


## T003: 同步bits测试用例

| 项目 | 内容 |
| :--- | :--- |
| 状态 | `[ ]` |
| 执行方式 | 若 `bits_link` 非空，则调用技能 `prd2case`，将对应的 bits 测试用例同步到当前目录下的 `case.md`；若 `bits_link` 为空字符串，则跳过本步骤，并在状态后备注 "bits_link 为空，跳过同步"。 |

---

> ✅ **Checkpoint：T002 标记为 [x] 后，方可进入 Phase 2**

---

# 🧪 Phase 2: Tests

> 目标：执行UI自动化测试
> ⚠️ **本阶段阻塞 Phase 3**
> ⚠️ **即使本地已有上次执行结果，也必须重新执行**

**前置条件检查：**

| Task | 状态 |
| :--- | :--- |
| T001 | `[ ]` |

---

## T004: 生成UI自动化 case 代码

| 项目 | 内容 |
| :--- | :--- |
| 状态 | `[ ]` |
| 前置条件 | `{SDT_DIR}/test/client/case.md` 存在 |
| 调用技能 | 能力标识 `EXEC_CASE_GENERATE`, 如果`EXEC_CASE_GENERATE`为空字符串，跳过本步骤 |

> ⚠️ **必须**：本步骤只调用能力标识为 `EXEC_CASE_GENERATE` 的技能执行自动化 case 代码生成，不执行用例。若该占位符未被替换，说明 SDT 注入失败，**不得**自行猜测技能名，请停止并重新运行 `/qa-sdt-tasks`。

**执行步骤：**

1. 解析 `{SDT_DIR}/test/client/case.md`，提取测试用例定义。
2. **调用能力标识为 `EXEC_CASE_GENERATE` 的技能**：按 `Skills (host compatibility)` 规则，优先按名调用，基于 `case.md` 中的用例定义生成可执行的自动化case代码。

---

## T005: 执行UI自动化测试

| 项目 | 内容 |
| :--- | :--- |
| 状态 | `[ ]` |
| 调用技能 | 能力标识 `EXEC_CASE_EXECUTE` |
| 执行方式 | 调用能力标识为 `EXEC_CASE_EXECUTE` 的技能执行全量用例, 并轮询任务状态直到任务执行完成 |

> ⚠️ **必须**：本步骤调用能力标识为 `EXEC_CASE_EXECUTE` 的技能执行自动化用例。若该占位符未被替换，说明 SDT 注入失败，**不得**自行猜测技能名，请停止并重新运行 `/qa-sdt-tasks`。

**执行结果记录：**

| 维度 | 结果 |
| :--- | :--- |
| 总用例数 | |
| 通过数 | |
| 失败数 | |
| 跳过数 | |
| 整体状态 | success / failed |
| 结果链接 | |

**失败用例明细（如有）：**

| 用例 ID | 用例名称 | 失败原因 | 是否已知问题 |
| :--- | :--- | :--- | :--- |
| | | | |

---

> ✅ **Checkpoint：T005 标记为 [x] 后，方可进入 Phase 3**

---

# 📊 Phase 3: Report

> 目标：汇总测试结果，输出风险结论

**前置条件检查：**

| Task | 状态 |
| :--- | :--- |
| T002 | `[ ]` |

---

## T006: 生成测试报告并归档

| 项目 | 内容 |
| :--- | :--- |
| 状态 | `[ ]` |
| 调用技能 | 能力标识 `REPORT_GENERATE` |
| 执行方式 | 调用能力标识为 `REPORT_GENERATE` 的技能，汇总 Phase 2 结果，生成测试报告 |

> ⚠️ **必须**：本步骤调用能力标识为 `REPORT_GENERATE` 的技能，生成测试报告。若占位符未被替换，说明 SDT 注入失败，**不得**自行猜测技能名，请停止并重新运行 `/qa-sdt-tasks`。

**报告内容：**

| 维度 | 结果 |
| :--- | :--- |
| ✅ 总通过率 | |
| ❌ 失败用例列表 | |
| 🐞 缺陷链接 | |
| ⚠️ 风险评估（是否可上线） | |

---

# 🔗 Pipeline 依赖关系

```
Phase 1: Advance Preparation (T001 → T002 → T003)
         ↓
Phase 2: Tests       (T004 → T005)
         ↓
Phase 3: Report      (T006)
```

<!--
【硬规则】
1. 下列占位符必须全部替换为最终值（非 "待填/TBD/同上"）。`{{IOS_SOURCE_COMMIT}}`、`{{ANDROID_SOURCE_COMMIT}}`、`{{BITS_LINK}}` **均允许为空字符串**，无需向用户确认。
2. 凡写「AskUserQuestion」处必须实际调用工具，不得代用户假设或静默跳过。
3. T001 按平台条件性调用 `qg_transfer` mcp：仅对 `ios_commit_id` / `android_commit_id` 非空的平台调用对应的 (`app_id` × `platform`) 组合；`commit_id` 为空的平台对应行「测试包链接」必须留空、「备注」需说明跳过原因。在需要执行的组合范围内不得遗漏。

【占位符清单 — 逐项完成后才可进入 Phase 1】（建议按序自检并口头确认已齐）
  [ ] {{IOS_SOURCE_COMMIT}}（允许为空字符串，无需向用户确认）
  [ ] {{ANDROID_SOURCE_COMMIT}}（允许为空字符串，无需向用户确认）
  [ ] {{BITS_LINK}}（允许为空字符串，无需向用户确认）

【进入 Phase 1 前最后一步 — 门禁】
口头或内心复述：IOS_SOURCE_COMMIT、ANDROID_SOURCE_COMMIT、BITS_LINK 均已被赋值（可为空字符串，无需用户确认，但占位符必须已被替换、不得保留 `{{...}}` 字面量）；T001 将仅对 `commit_id` 非空的平台执行 (`app_id` × `platform`) 组合调用，空 `commit_id` 对应行的「测试包链接」留空。
任一不满足则继续 AskUserQuestion，不得开始T001。
-->
