# Retrospective: Sprint 5 — Combat UI Interaction & Navigation

**Period**: 2026-06-14 -- 2026-06-28（实际收尾 2026-06-18，提前 10 天）
**Generated**: 2026-06-18
**Stage**: Production
**Sprint Goal**: Complete the Combat UI player-decision loop by implementing the move selection panel, counter/decisive prompts, dual-focus navigation, and the QA/tooling fixes needed for reliable Presentation sprint validation.

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|---------|--------|-------|
| Must Have Tasks | 3 (cu-004 / cu-005 / cu-008) + 2 preflight (S5-Preflight / S5-SmokeLog) | 3 + 2 完成 | 0 |
| Should Have Tasks | 1 (cu-007) — cu-006 计划 defer | 2 完成（cu-006 mid-sprint 拉回） | +1 |
| Nice to Have | EI-Debt-Triage 0.5d | 未拉入 | 0 |
| Completion Rate | -- | 100% (5/5 sprint-status stories) | -- |
| Story Effort Days | 5.0 estimate-days | ≈4.0 estimate-days actual（cu-006 计划外回收） | -1.0 |
| Bugs Found / Fixed | -- | 0 / 0 | -- |
| Unplanned Tasks Added | -- | cu-006 mid-sprint 拉回；harness 扩展（cu-006/007 panel）；harness highlight evidence | +3 |
| Commits | -- | 4 sprint-scope commits（cu-004 / cu-005 + 2 阶段提交） | cu-006 / cu-007 / harness 工作树未 commit |

QA / 验证：
- Foundation 全 suite 95/95 CombatUi 通过（87 cu-004..008 + 8 cu-006）
- `dotnet build FengZhi.slnx` 0 错（4 旧 warning 与本 sprint 无关）
- harness build 0 错 0 警告
- 未跑 `/smoke-check sprint`（Definition of Done 中 "Smoke check passed" 与 "QA sign-off report" 仍未生成 — 见 Action Items #1）

---

## Velocity Trend

| Sprint | Planned | Completed | Rate |
|--------|---------|-----------|------|
| Sprint 3 | 8 | 8 | 100% |
| Sprint 4 | 5 | 5 | 100% |
| Sprint 5 (current) | 5 | 5（含 1 mid-sprint pull） | 100% |

**Trend**: Stable at 100% — 第三轮连续 full delivery；Sprint 5 是首次出现 mid-sprint scope 扩张（cu-006 从 defer 重新拉回并完成），表明 Foundation 层的契约式 UI 工作复杂度低估。

---

## What Went Well

- **player-decision loop 完整闭环**：cu-004（招式选择面板）→ cu-005（反制/决胜提示）→ cu-008（双焦点/手柄）三条 Must Have 在 sprint 第一天集中合入（commits eaed3b3 / f141e0e），cu-007 协同与回合警戒于 2026-06-18 落地，cu-006 一击决胜导演 Foundation 层同日落地。
- **契约式开发收益**：cu-006 7-phase 演出在 0 Godot 节点情况下用纯 C# Foundation 层 + xUnit 8 fact 把 7 个 AC + 通用回滚全锁定；ADR-0009 / ADR-0011 的 director / TimeScale / Camera / CinematicLock 数值契约首次跑通，Godot 实机集成可在下一 sprint 单独并发。
- **`SynergyDeclaredEvent` + decisive lifecycle 3 events** 一致沿用同一 BattleEventBus 模式（cu-007 设立、cu-006 复用），UI 不自判收益，订阅方挂载即可。
- **harness 化 evidence 采集**：sprint5-combat-ui-harness 引入 cu-007 / cu-006 独立 panel + fixture 数据集，自带 BattleEventBus + adapter / director，evidence 截图无需启动完整战斗就能复现。cu-007 4 张证据已就位。
- **Sprint 4 action item #1（`--log-file` smoke 约定）已落地为 S5-SmokeLog**，并被 cu-* 演练继承。
- **scope-check 决策机制生效**：2026-06-18 `/scope-check combat-ui` 把 cu-006 主动 defer，再凭 capacity 数据回拉，避免了 willy-nilly scope creep。

---

## What Went Poorly

- **工作树仍未 commit**（这是 Sprint 4 action #5 carried-over）：截至本 retro，cu-006 / cu-007 / harness 扩展、evidence MD、tech-debt 更新、sprint-status / active.md 全在 staging zone（11 modified + 9 untracked）。Sprint 4 已经把"commit or shelve"列为 action item，本 sprint 仍重复发生。
- **Definition of Done 未完整闭合**：sprint plan 列了 "Smoke check passed (`/smoke-check sprint`)"、"QA sign-off report (`/team-qa sprint`)"、"production/qa/qa-plan-sprint-5-*.md"，但只看到 `qa-plan-sprint-5-2026-06-14.md` 在仓库里；smoke / team-qa 两份产物本 sprint 没有跑。retrospective 提前 10 天触发，部分原因正是这些 gate 被跳过。
- **Visual/Feel evidence 缺口积累**：cu-004（move panel）+ cu-005（counter/decisive prompts）+ cu-008（手柄硬件）3 个 Visual stories 的 evidence 仍是 deferred；cu-007 4 张证据已补齐但 cu-006 实机录屏 deferred 到下一 sprint。即"自动化绿"≠"Visual story 真的 done"。
- **commit message 不规范**：`阶段性提交`（203c99e）这种 stage commit 出现在 sprint 主干，违反 commit 信息规范，会让 changelog/audit 链路退化。
- **actual effort tracking 仍参差**：Sprint 4 action #4（每 story 记录实际 effort）在 cu-004/005/008 的 session log 中没有体现 hour-level 数据；cu-006 / cu-007 同样只能推断 estimate-day。
- **harness panel 累积**：sprint5-combat-ui-harness 现在挂了 cu-007 + cu-006 两个独立 panel，与 move-selection pipeline 三套并存，长期需评估是迁正式 dev tool 还是收口（已计入 tech debt）。

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|----------|------------|------------|
| cu-006 Engine Risk（TimeScale + Tween + Camera + CinematicLock 自锁未知） | sprint 早期 | 2026-06-18 `/scope-check` 把 cu-006 拆成 Foundation 契约 + Godot 实机两层；前者本 sprint 完成，后者明确 defer | 高风险 visual story 默认拆契约/集成两层；Foundation 先到位的成本远低于一次性拼到 Godot |
| AnimationCommand.cs MoveType 类型缺失（CS0246） | < 5 分钟 | 加 `using FengZhi.Foundation.CharacterData;` | 跨 namespace record struct 时确保 build error 路径有 fact 覆盖（已被 cu-006 8 fact 第一次构建发现） |
| sprint plan vs sprint-status.yaml 双源真相 | 整 sprint | sprint-status.yaml 始终作为 SoT；plan 中 cu-006 deferred 标注与 yaml 中 done 状态本 retro 才显式核对 | 让 `/scope-check` 与 `/dev-story` 同步更新两边，或把 plan 表格自动从 yaml 投射 |

---

## Estimation Accuracy

| Task | Estimated | Actual | Variance | Likely Cause |
|------|-----------|--------|----------|--------------|
| S5-Preflight | 0.5d | ≈0.25d | -50% | TR-ID registry 已结构化，placeholder 替换批量化 |
| S5-SmokeLog | 0.5d | ≈0.25d | -50% | Sprint 4 已留有 conditional approval 备忘 |
| cu-004 招式选择面板 | 1.0d | ≈1.0d | 0 | 范围与 estimate 吻合 |
| cu-005 反制/决胜提示 | 0.75d | ≈0.75d | 0 | 范围与 estimate 吻合 |
| cu-008 双焦点/手柄 | 0.75d | ≈0.75d | 0 | 键盘 fallback 已覆盖；手柄硬件验证 deferred |
| cu-007 协同/回合警戒 | 0.5d | ≈0.5d | 0 | 仅订阅事件；无新数值规则 |
| cu-006 一击决胜导演 | 1.0d | ≈1.0d (Foundation only) | 0（按 Foundation 契约切片估）；Godot 实机额外 ~0.75d defer | scope-check 把高风险层切走后，剩余部分按预期完成 |

**Overall estimation accuracy**: 7/7 任务在 ±20% 范围内。两个 preflight 任务低估自身复杂度的下界，提示后续 preflight 类工作可统一 0.25d 起估。

---

## Carryover Analysis

| Task | Original Sprint | Times Carried | Reason | Action |
|------|----------------|---------------|--------|--------|
| Godot 4.7-stable 实机集成（Engine.TimeScale + Tween Always + Camera2D + InputMap 拦截） | Sprint 5 cu-006 切片 | 0 (newly deferred) | scope-check 显式拆契约层与集成层 | 列入下一 sprint Must Have；与 ICombatService Facade 同 PR 落地 |
| cu-004 / cu-005 / cu-008 UI walkthrough 截图 | Sprint 5 (Visual) | 0 (deferred at completion) | 解放主开发流，但 Visual 不算真的 done | 列入下一 sprint 明确 Visual evidence 条目；或在 Sprint close-out 单 patch sprint 收尾 |
| cu-008 真实手柄硬件验证 | Sprint 5 cu-008 | 0 (无硬件) | 没有物理手柄 | 等手柄到位后 hotfix；当前以键盘 fallback 守约 |
| 体系专属动画美术资源（gang/rou/qiao） | Sprint 5 cu-006 | 0 (deferred) | 美术资源不在本 sprint scope | 列入美术 sprint，contract 已就位 |
| Sprint 4 工作树未提交习惯 | Sprint 4 → Sprint 5 | 2 | sprint 末尾收尾压力下 commit 滑漏 | **本 retro 强制立刻 commit**（见 Action #1） |

---

## Technical Debt Status

- 当前 tech-debt-register.md 共 25 条（Sprint 4 retrospective 时 9 条 → +16 条 cu-* 相关）
  - cu-007 加 5 条（2 OUT OF SCOPE deviation + 3 INFO suggestion）
  - cu-006 加 6 条（2 OUT OF SCOPE deviation + 1 Deferred 实机集成 + 3 code review NOTE）
  - cu-004 / cu-005 / cu-008 各 1 条（Visual evidence deferred）
- TODO/FIXME/HACK 源码 token：未单独扫描（与 Sprint 4 一致），但 Visual evidence 缺口与 deferred 集成是更显式的 IOU。
- Trend: Growing (intentional, all flagged) — 增长来自两类：① 契约层先到位、集成层 deferred 的拆分策略；② Visual evidence 真实跑实机才能补全。两类都登记完整，无暗债。

---

## Previous Action Items Follow-Up (from Sprint 4)

| Action Item | Status | Notes |
|-------------|--------|-------|
| #1 Godot headless `--log-file` 约定 | Done | S5-SmokeLog 落定本 sprint smoke 约定；后续保持 |
| #2 Story readiness 元数据完整性 | Done | cu-004..008 所有 story 在 `/dev-story` 前补齐 Estimate / Out of Scope / Control Manifest / Engine Notes / Performance Notes / TR-ID（S5-Preflight 完成） |
| #3 Triage Exploration tech debt | Not Started | EI-Debt-Triage 未拉入本 sprint；本 sprint 全力推 cu-* — 仍是 Action Item |
| #4 每 story 记录 actual effort | Partial | cu-* session log 含 estimate-day 与 fact 数；hour-level 数据仍未常态化 |
| #5 sprint 结束前 commit/shelve 工作树 | **Recurring failure** | 本 sprint 同样 11 modified + 9 untracked 在 staging zone — Action Item 升级 |

---

## Action Items for Next Iteration

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|----------|----------|
| 1 | **立刻 commit Sprint 5 工作树**：cu-006 + cu-007 + harness 扩展 + evidence + tech-debt + sprint-status + active.md。按 `/story-done` 各自的 suggested commit 命令执行；不可继续 carry over | Dev | **High** | 在 `/sprint-plan new` 之前 |
| 2 | **[Done 2026-06-22]** 运行 `/smoke-check sprint` + `/team-qa sprint` 补齐 Sprint 5 Definition of Done（smoke 与 QA sign-off 文档），把"自动化绿"升级到"sprint 真的 done"。— 闭合证据：[smoke-2026-06-22-sprint-5.md](../qa/smoke-2026-06-22-sprint-5.md) verdict PASS + [qa-signoff-sprint-5-2026-06-22.md](../qa/qa-signoff-sprint-5-2026-06-22.md) verdict APPROVED WITH CONDITIONS（驱动 story：Sprint 6 S6-Sprint5-DoD） | QA / Dev | High | 启动下一 sprint 之前 |
| 3 | 把 cu-006 Godot 4.7-stable 实机集成（Engine.TimeScale + Tween Always + Camera2D + InputMap + ICombatService Facade）作为下一 sprint Must Have；同时拉入 cu-004 / cu-005 / cu-008 / cu-006 的 Visual evidence 截图条目作为 Visual sub-tasks | Producer / Dev | High | `/sprint-plan new` 时纳入 |
| 4 | 引入 commit message lint：禁止 `阶段性提交` / `init(test):` 这类无信号 commit 出现在 sprint 主干（推荐 conventional commits + scope，参照 cu-004/005 的 `feat: ...(TR-...)` 模板） | Dev | Medium | 下一 sprint 第一个 PR |
| 5 | 把 hour-level actual effort 写进每条 `/story-done` session log（接续 Sprint 4 #4，本 sprint 仍 partial） | Producer / Dev | Medium | 下一 sprint 每个 story |

---

## Process Improvements

- **scope-check 拆层化**：本 sprint cu-006 用 "Foundation 契约 + Godot 实机" 拆层成功；建议把这种拆分作为高 Engine Risk Visual story 的默认起手式，而不是等 sprint 末尾才拆。
- **commit gate**：在 `/story-done` skill 末尾加一个 reminder（"commit or shelve before next sprint"），把 carryover #5 升级为 hard gate；当前是 soft suggestion。
- **Definition of Done 自动校验**：sprint plan 的 "Smoke check passed / QA sign-off / Manual evidence" 三条不要靠人记，集成到 `/sprint-status` close-out 校验或 retrospective 触发条件中。

---

## Summary

Sprint 5 是 Combat UI player-decision loop 的高完成度 sprint：5/5 stories（含一个 mid-sprint scope-check 回拉的 cu-006 Foundation 切片）全部 AC pass，自动化覆盖 95/95，提前 10 天结束。但完成度的高水位线掩盖了三件应该担心的事：① Sprint 4 已经标记的"工作树不及时 commit"在本 sprint 第二次发生；② Definition of Done 中的 smoke/QA sign-off/Visual evidence 三类产物未补齐就触发 retro；③ Visual stories 的"自动化通过"与"实机评定通过"被混用为同一个 done。下一 sprint 的第一步不是新功能，而是 commit 当前工作树 + 跑 smoke / team-qa + 把 Godot 实机集成与 Visual evidence 列为明确条目。
