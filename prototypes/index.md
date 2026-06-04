# Prototypes Index

Complete history of every prototype run for 《风止》. Each row is a verdict,
not just a "we tried this." This file exists so future decisions know what was
already learned (and avoid making the same mistake twice).

---

## Concept Prototypes

| Concept | Date | Path | Verdict | Report | Notes |
|---|---|---|---|---|---|
| Burst+Read Combat (Round 1) | 2026-06-02 | Paper | **PROCEED with refinements** | [REPORT.md](./burst-read-combat-concept/REPORT.md) | 核心循环成立；6 项 build/balance 修订留待 GDD |
| Burst+Read Combat (Round 2) | 2026-06-02 | Paper | **PROCEED + 3 项新发现** | [rules-v0.2-diff.md](./burst-read-combat-concept/rules-v0.2-diff.md) · [play-log-v0.2.md](./burst-read-combat-concept/play-log-v0.2.md) | 3 项核心修正方向都奏效；新发现 3 项 design issues 留待战斗 GDD |

---

## Spike Prototypes

| Concept | Date | Path | Verdict | Report | Notes |
|---|---|---|---|---|---|
| Burst+Read Engine Feel | 2026-06-02 | Engine (Godot 4.6.3 + C#) | **PROCEED** | [REPORT.md Engine Spike Findings](./burst-read-combat-concept/REPORT.md) | 4/4 feel 问题通过；克制倍率需从 3:1 调至 ~2:1 |

---

## How to Read This File

- **PROCEED** —— 概念哲学验证通过，可投入正式 GDD 编写
- **PROCEED with refinements** —— 哲学通过但发现具体问题；GDD 阶段必须解决
- **PIVOT** —— 哲学有部分价值但方向需调整；下一轮 prototype 应继承 PIVOT-NOTE.md
- **KILL** —— 概念不成立；详情见 `GRAVEYARD.md`

每个 prototype 都是 throwaway。即使 PROCEED，**production code 也从零开始写**，prototype code 不被重构进 production。
