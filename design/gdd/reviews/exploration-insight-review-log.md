# Review Log: 探索/洞察（exploration-insight.md）

---

## Review — 2026-06-07 — Verdict: NEEDS REVISION → Post-Review Revision Applied

Scope signal: S
Specialists: game-designer, level-designer（lean review）
Blocking items: 2 | Recommended: 3
Summary: GDD 整体结构完整，场景交互三分类清晰，洞察检定流程与 dialogue-system 保持一致。发现 2 个阻塞项：(1) `Condition[]` 格式未引用 dialogue-system.md §7 条件类型标准；(2) `DiscoveryReward` 数据结构未展开字段定义（不同 discovery_type 的 reward 参数各不相同）。两项已在同一会话中修复——Condition 添加了格式引用和 8 种条件类型列表，DiscoveryReward 展开为 tagged-union 结构。3 条推荐项（场景洞察与战斗洞察区分说明、二周目机制决策 OQ3、反向依赖确认）留作后续迭代。
Prior verdict resolved: First review — revisions applied in same session
