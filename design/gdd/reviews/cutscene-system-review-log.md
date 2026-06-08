# Review Log: CG / 演出系统 (cutscene-system.md)

## Review — 2026-06-07 — Verdict: MAJOR REVISION NEEDED
Scope signal: L
Specialists: game-designer, systems-designer, narrative-director, qa-lead, ux-designer
Blocking items: 10 | Recommended: 13
Summary: GDD 存在哲学级矛盾（"纯播放器"无法承载转念事件等交互叙事需求）和致命技术缺陷（WAIT_INPUT + FULL Lock 死锁）。作为 Player Fantasy "世界为我停下来"的系统，完全缺失 transition_in 设计、同优先级裁决规则和链式间隔配置。AC 可测试性不足（3 条不通过 + 9 条缺失）。
Prior verdict resolved: First review

### Blocking Items Summary
1. WAIT_INPUT + FULL Lock = 死锁
2. "纯播放器"与叙事交互的结构矛盾（缺 PLAYER_TRIGGER/PLAYER_CHOICE）
3. transition_in 设计完全缺失（Player Fantasy 核心体验盲区）
4. 同优先级冲突无裁决规则（E2 FIFO 与 E6 优先级矛盾）
5. E6 "或丢弃"决策条件未定义
6. on_complete 重入无合约
7. LOADING 超时后状态转移未定义
8. 链式间隔不可配置（500ms 固定破坏顿悟→突破连续性）
9. Tier 4 微演出隐藏全部 HUD（NONE lock 下不合理）
10. NG+ first_view_unskippable 违反玩家预期

### Key Design Decisions Needed
- "纯播放器" + interactive_step 扩展协议的具体范围
- 同 Tier 冲突的 narrative_weight 字段设计
- chain_gap 可配置 + transition_type 枚举
- Tier 3 NG+ 跳过策略
- OQ3 环境自适应：Tier 3-4 硬需求 vs Tier 1-2 软需求
