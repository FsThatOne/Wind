# Prototypes Index

Complete history of every prototype run for 《风止》. Each row is a verdict,
not just a "we tried this." This file exists so future decisions know what was
already learned (and avoid making the same mistake twice).

---

## Concept Prototypes

| Concept | Date | Path | Verdict | Report | Notes |
|---|---|---|---|---|---|
| Burst+Read Combat (Round 1) | 2026-06-02 | Removed 2026-06-22 | **PROCEED with refinements**（历史） | Report removed with stale prototype | 核心循环成立；战斗模型已被「行气战棋（观/动）」彻底 supersede，spike 文物清理 |
| Burst+Read Combat (Round 2) | 2026-06-02 | Removed 2026-06-22 | **PROCEED + 3 项新发现**（历史） | Report removed with stale prototype | 3 项核心修正方向都奏效；后被 Xingqi 战棋方向取代，文物清理 |

---

## Spike Prototypes

| Concept | Date | Path | Verdict | Report | Notes |
|---|---|---|---|---|---|
| Burst+Read Engine Feel | 2026-06-02 | Removed 2026-06-22 | **PROCEED**（历史） | Report removed with stale prototype | Feel 问题验证通过；同样因战斗模型 supersede 一并清理 |

---

## Vertical Slice Prototypes

| Concept | Date | Path | Verdict | Report | Notes |
|---|---|---|---|---|---|
| 风止 — Full Core Loop | 2026-06-10 | Removed 2026-06-17 | **PROCEED** historically; **retired** | Report removed with stale prototype | First-run slice validated an old Burst+Read loop. Deleted because it used the obsolete combat model and was no longer safe as QA/playtest evidence. |

---

## How to Read This File

- **PROCEED** —— 概念哲学验证通过，可投入正式 GDD 编写
- **PROCEED with refinements** —— 哲学通过但发现具体问题；GDD 阶段必须解决
- **PIVOT** —— 哲学有部分价值但方向需调整；下一轮 prototype 应继承 PIVOT-NOTE.md
- **KILL** —— 概念不成立；详情见 `GRAVEYARD.md`

每个 prototype 都是 throwaway。即使 PROCEED，**production code 也从零开始写**，prototype code 不被重构进 production。
