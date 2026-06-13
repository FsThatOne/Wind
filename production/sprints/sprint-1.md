# Sprint 1 — 武学组合系统

> **Start**: 2026-06-11
> **End**: 2026-06-25
> **Goal**: 实现武学数据层全部核心契约，让战斗系统在下个 sprint 可以稳定读取招式、克制、冷却和装备信息。

---

## Capacity

- 总工作日: 10 天 (2 周)
- Buffer (20%): 2 天
- 可用: 8 天

---

## Tasks

### Must Have (Critical Path)

| ID | Task | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-----------|-------------|-------------------|
| ma-001 | 武学 YAML 数据模型与克制矩阵加载 | 1 | — | YAML 可加载、校验通过、克制矩阵可查询；错误信息含文件路径+行号 |
| ma-002 | 招式成长路径、残卷合成与批注版替换 | 1.5 | ma-001 | 残片→拓本→完本→批注 全路径可走通；合成规则有测试 |
| ma-003 | 招式伤害、完整度、境界缩放与批注倍率公式 | 1 | ma-001, ma-002 | 公式为纯函数、测试覆盖边界值和极端输入 |
| ma-004 | 特殊触发条件与特殊效果解析契约 | 1 | ma-001, ma-002 | 条件链可组合、效果可扩展、至少 5 种核心效果有测试 |
| ma-005 | 角色武学配置槽、体系覆盖警告与同伴锁定 | 1.5 | ma-001, ma-002 | 6招式+1心法+1轻功槽验证通过；同伴锁定规则生效 |

### Should Have

| ID | Task | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-----------|-------------|-------------------|
| ma-006 | 心法装备门槛、被动加成与专属招式 | 1 | ma-001, ma-003, ma-005 | 心法切换对招式面板和属性加成生效 |
| ma-007 | 轻功装备位与战棋移动契约 | 1 | ma-001, ma-005 | 移动范围计算可查询、轻功装备位独立于招式槽 |

### Nice to Have

| ID | Task | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-----------|-------------|-------------------|
| ma-008 | 武学管理与战斗招式面板呈现规则 | 1 | 全部 P0+P1 | UI 规则接口定义完成、可供 Presentation 层调用 |

---

## Carryover from Previous Sprint

无（第一个 Sprint）。

---

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| ma-001 部分已有实现与 GDD 不完全匹配 | Medium | Low | 对比现有代码和 story acceptance criteria，按需重构 |
| 特殊效果类型数量过多导致 ma-004 超时 | Low | Medium | MVP 先实现 5 种核心效果，余下留后续 sprint |
| 招式公式平衡性问题延误 ma-003 | Low | Low | 先实现公式结构正确性，平衡调参留后续迭代 |

---

## Dependencies on External Factors

- 无外部依赖。所有数据和逻辑完全本地。

---

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] All Logic/Integration stories have passing unit tests in `tests/`
- [ ] No S1 or S2 bugs in delivered features
- [ ] 武学从 YAML 加载 → 角色装备 → 战斗系统查询的全路径验证通过
- [ ] Code reviewed and merged

---

> **Scope check:** 本 sprint 严格对齐 `production/epics/martial-arts-system/EPIC.md` 定义的 8 个 stories，无额外 scope。

> ⚠️ **No QA Plan**: 本 sprint 启动时未创建 QA plan。建议在最后一个 story 实现前运行 `/qa-plan sprint`。Production → Polish gate 要求 QA sign-off report。
