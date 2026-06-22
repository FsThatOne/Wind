# Gate Check: Technical Setup → Pre-Production

> **Date**: 2026-06-22
> **Checked by**: gate-check skill (lean mode)
> **Trigger**: ADR-0019 → ADR-0020 渲染方向变更（2026-06-22）后的回炉审计 (retroactive audit)
> **Project Stage**: 仍为 `Production` —— 本次为回炉审计，不推进 stage
> **Verdict**: ✅ **CONCERNS**（Producer NOT READY 经 skill §Pre-Production 显式 Vertical Slice 规则下调；用户已显式接受 override）

---

## Scope of This Gate

ADR-0019（伪 2.5D《逸剑风云决》方向）于 2026-06-22 被 ADR-0020（纯 2D《大侠立志传》方向）取代。
由于这是一次"foundation-level"的方向变更，本次回炉审计 **Technical Setup → Pre-Production** 门：
检查在新方向下，Pre-Production 的进入条件是否仍然满足、是否有需要在切片 sprint 收口的隐患。

项目实质处于 Production，本次不会修改 `production/stage.txt`。

---

## Required Artifacts: 13 / 13 present

| # | Artifact | Status | 备注 |
|---|---|---|---|
| 1 | Engine chosen (Godot 4.7-stable) | ✅ | CLAUDE.md / `.claude/docs/technical-preferences.md` 一致 |
| 2 | `.claude/docs/technical-preferences.md` populated | ✅ | 引擎、性能预算、命名规范完整 |
| 3 | `design/art/art-bible.md` Sections 1–4 present | ✅ | Visual Identity Summary / Reference Board / Color Palette / Art Style 都在；13 节，Status: Draft |
| 4 | ≥3 ADRs covering Foundation-layer | ✅ | 20 ADRs（19 Accepted + 1 Superseded）；Foundation 6/6 |
| 5 | `docs/engine-reference/godot/` | ✅ | VERSION.md 已 pin 4.7-stable（2026-06-20） |
| 6 | `tests/unit/` + `tests/integration/` | ✅ | 113+ 测试文件 |
| 7 | `.github/workflows/tests.yml` | ✅ | 存在 — 但内容 stale（见 C1） |
| 8 | Example test file exists | ✅ | 大量已就位 |
| 9 | `docs/architecture/architecture.md` | ✅ | Draft — Pending TD Sign-off |
| 10 | `docs/architecture/traceability-index.md` | ✅ | 2026-06-22 更新；0 ❌ Gap |
| 11 | `/architecture-review` run | ✅ | `architecture-review-2026-06-22.md` 验证 PASS WITH MINOR CONCERNS |
| 12 | `design/accessibility-requirements.md` | ✅ | Standard (WCAG 2.1 AA) committed |
| 13 | `design/ux/interaction-patterns.md` | ✅ | v1.0，25 patterns |

---

## Quality Checks: 8 / 10 passing, 2 minor

| # | Check | Status | 备注 |
|---|---|---|---|
| 1 | ADRs cover core systems | ✅ | rendering / input / state / scene / save / dialogue / cutscene |
| 2 | Technical preferences populated | ✅ | 完整 |
| 3 | Accessibility tier defined | ✅ | Standard |
| 4 | ≥1 screen UX spec started | ✅ | main-menu / hud / pause-menu |
| 5 | All ADRs have Engine Compatibility | ✅ | 20 / 20 |
| 6 | All ADRs have GDD Requirements linkage | ⚠️ | 14 / 20 用正式 `## GDD Requirements Addressed` 标题；6 个 Feature/Presentation-layer ADR (0013–0018) 用 `### Requirements (from GDD #N)` —— 语义到位、标题格式偏差，非阻塞 |
| 7 | No deprecated API references | ✅ | 架构评审已确认 0 |
| 8 | HIGH RISK engine domains addressed | ⚠️ | cu-006 spike 验证 4 个 4.7 API（TimeScale / Tween / Camera2D / InputMap）；dual-focus (ADR-0002) 仍属"按引用验证"无独立 spike — 应在 combat-ui 实现 story 前补 |
| 9 | Foundation layer zero gaps | ✅ | 0 ❌；Partial 不算 gap |
| 10 | All ADRs agree on engine version | ✅ | 全部 4.7-stable |

---

## Director Panel Assessment（lean mode — 全部 spawn）

| Director | Verdict | 关键反馈 |
|---|---|---|
| Creative Director ([log](75428fff-d9f9-486b-a127-dada10919e3e)) | **CONCERNS** | 支柱/幻想/视觉锚点完好；但 ① 旧 VS 删除后核心幻想验证已失效（战斗模型 + 渲染双重失效）；② art-bible L71–72 残留 Read/Burst 旧战斗模型；③ art-bible L185 列 PointLight2D 为室内常规光源，**违 ADR-0020 D2**；④ systems-index.md #12 仍标 "Needs Revision" 与正文修复未对齐 |
| Technical Director ([log](db2965a5-7290-4b6b-b1b1-cb96f081dc4c)) | **CONCERNS** | 架构在 ADR-0020 下稳固；5 项 in-phase 关切：① CI tests.yml 仍 4.6.3（**最高优先**）；② architecture.md §12 ADR Roadmap stale；③ control-manifest Manifest Version 2026-06-10 未含 ADR-0019/0020；④ architecture.md 仍 Draft 状态；⑤ dual-focus spike 缺失 |
| Producer ([log](9e377d35-2c9b-466b-9bbb-aa761b0e8527)) | **NOT READY** (overridden) | ADR-0020 转向后无当前架构 + 纯 2D VS（旧 VS 2026-06-17 删除）；唯一 playtest 是 harness 级；下个 sprint 第一优先级 = 重建符合 ADR-0020 §Validation Criteria 的全循环 VS spike；MVP 8 系统逻辑层已达，Feature 6 + Presentation 4 Ready 故事须正式重切 scope |
| Art Director ([log](b546033c-5d71-49bf-a7d0-64e3993b91df)) | **CONCERNS** | art-bible Sections 1–4 够 Pre-Production；Lighting 表残留 PointLight2D 行（CD 也独立发现）；缺立绘/sprite 双轨设计对齐流程 |

**Director Panel 裁决说明**：
按 skill §4b "Any director returns NOT READY → 最少 FAIL（用户可显式 override）"。
但 skill §Pre-Production gate 显式 Vertical Slice 规则规定：
> "Slice was not built (skipped) → downgrade to **CONCERNS only, not FAIL**."
> 4 项 VS 相关 Required Artifacts 全部标为 "**recommended, not blocking**; if absent, surface as CONCERNS"。

Producer 提出的两条 blocker（VS 缺失、full-loop playtest 缺失）**正是 skill 显式规则下调为 CONCERNS 的两条**。用户已显式接受 override → 最终裁决为 **CONCERNS**。

---

## Chain-of-Verification — 5 questions checked

1. **VS 缺失到底算 FAIL 还是 CONCERNS？** [TOOL ACTION] 重读 skill §Pre-Production gate 显式 VS 规则，确认 VS 缺失 → CONCERNS（不是 FAIL）。
2. **art-bible L71–72 / L185 是否真的漂移？** [TOOL ACTION] Read 验证：L71 "战斗 (Read 阶段)"、L72 "战斗 (Burst 阶段)"、L185 "室内 | PointLight2D/灯笼贴图" —— **均确认存在**，与 ADR-0020 D2 直接冲突。
3. **是否有 Partial ADR 应被视为 Foundation gap？** Foundation 层 #1 / #10 / #11 / #12 中 #1 #11 为 Partial，但基础设施 ADR (0001/0003/0008) 已覆盖。Partial ≠ Gap。不升级。
4. **CI tests.yml stale 是否阻塞？** MEDIUM in-phase（一行修复）。当前 CI 在错误引擎跑 = 虚假信号；下 sprint VS 代码合入前必修，但非启动阻塞。
5. **GDD Requirements Addressed 标题缺失是否阻塞？** 6 个 ADR (0013–0018) 用 `### Requirements (from GDD #N)` 替代正式标题，但语义连接到位（明确指向 GDD 编号）。格式偏差，非阻塞。

**Chain-of-Verification: 5 questions checked — verdict unchanged (CONCERNS, with explicit user override of Producer NOT READY)**

---

## CONCERNS（按推荐处理顺序）

### 高优先（下 sprint 最值得做的两件事）

- **C-VS / C-PLAYTEST**：ADR-0020 转向后无当前架构 + 纯 2D Vertical Slice；唯一 playtest 是 harness 级
  - **Action**：下 sprint 第一优先级 = 重建符合 ADR-0020 §Validation Criteria 1–5 的全循环 VS spike：1 屏纯 2D 战棋场景 + 立绘↔战斗↔立绘全循环 + 1 招式 VFX + 1 章节 CanvasModulate 切换 + ≥5 个 sprite 验 1080p/720p 可读性；其上跑一次全循环 new-player-experience playtest，产出 `production/playtests/` 报告
  - **由**：CD + Producer 双独立指认

- **C1**：`.github/workflows/tests.yml` 仍 `godot-version: '4.6.3'` 与 4.7 pin 错配
  - **Action**：一行修复，VS sprint 代码合入前必修。当前 CI = 虚假信号
  - **由**：TD

### 中优先（art-bible / 文档同步）

- **C2**：`design/art/art-bible.md` 战斗模型残留 + ADR-0020 D2 直接冲突
  - L71–72：`战斗 (Read 阶段)` / `战斗 (Burst 阶段)` → 改为 `战斗 (观)` / `战斗 (动)`（沿用 HUD L249 口径）
  - L185：`室内 | PointLight2D/灯笼贴图` → 改为 `CanvasModulate + 烘焙阴影贴图`（PointLight2D 降为特例 PR）
  - **由**：CD + AD 双独立指认

- **C3**：`design/art/art-bible.md` Character Art Standards 缺立绘↔sprite 双轨设计对齐流程
  - ADR-0020 §Risk 表已识别该项为高概率风险；批量美术生产启动前补
  - **由**：AD

- **C4**：`design/gdd/systems-index.md` #12 (line 33) 仍标 `Needs Revision`，但 `map-scene-management.md` L542 已修
  - **Action**：改回 `Designed`
  - **由**：CD

- **C5**：`docs/architecture/architecture.md` §12 ADR Roadmap stale（仅列 001–010；缺 0011–0020）+ 状态仍 `Draft — Pending TD Sign-off`
  - **Action**：补 §12 + 在状态头记录 TD 本次 CONCERNS-accepted
  - **由**：TD

- **C6**：`docs/architecture/control-manifest.md` Manifest Version 2026-06-10，未含 ADR-0019 (Superseded) / ADR-0020
  - **Action**：在下 VS sprint story 嵌入新 manifest_version 之前 `/create-control-manifest update`
  - **由**：TD

### 低优先（in-phase 跟踪）

- **C7**：dual-focus (ADR-0002, 4.6 post-cutoff) 无独立 spike
  - **Action**：在 combat-ui 实现 story 启动前补 spike
  - **由**：TD

- **C8**：Feature 6 + Presentation 4 Ready 故事 + 美术内容量产未启动 — 需正式 milestone scope 重切决策
  - **Action**：建议把可发售脊柱锁定为 MVP + Vertical Slice（"第一章·江南"全循环），6 个 Alpha-tier Feature 系统显式标为"时间不足时可砍"
  - **由**：Producer

---

## Stage Update

**不更新** `production/stage.txt`。本次为 retroactive audit，项目实质阶段保持 `Production`。

---

## Verdict: ✅ CONCERNS

- 13 / 13 Required Artifacts present
- 8 / 10 Quality Checks passing（2 项为非阻塞格式偏差）
- 4 Director Panel：3 CONCERNS + 1 NOT READY（已按 skill 显式 VS 规则下调，用户显式接受 override）
- Chain-of-Verification 5 questions — verdict unchanged

**进入 Pre-Production 实质可行（实际上项目已在 Production 阶段执行），但下个 sprint 应优先把 C-VS + C-PLAYTEST + C1 三项处理掉**，否则 Production → Polish 门将继续受阻（这正是 2026-06-17 Polish gate FAIL 的核心原因之一）。

---

## Next Steps

1. **Sprint 7（建议）** = ADR-0020 全循环 VS 重建 + new-player playtest（覆盖 C-VS + C-PLAYTEST）
2. 并行修 C1（CI 4.7 升级，一行）+ C2/C3（art-bible 三处修订）+ C4/C5（systems-index、architecture.md）
3. VS spike 通过后再启动 Feature/Presentation 故事；先做 C8 scope 重切决策
4. Combat UI 实现 story 启动前补 C7（dual-focus spike）
5. VS PROCEED + playtest PASS 后，重跑 `/gate-check polish`（继续推进 Production → Polish 路径）

---

*Generated by `/gate-check` skill (lean mode), parallel director panel with explicit Producer override.*
