# Session State — Vertical Slice Build

> **Concept**: 风止 (Wind Stops)
> **Phase**: **Completed — Phase 6 (Report)**
> **Started**: 2026-06-10
> **Completed**: 2026-06-10
> **Verdict**: PROCEED
> **Report**: deleted with retired `prototypes/fengzhi-vertical-slice` on 2026-06-17 because the slice used the obsolete combat model
> **Validation Question**: "玩家能否在 3-5 分钟内无引导体验 Burst+Read 战斗 + 心境选择 + 朦胧化反馈的完整循环？" → **Yes**

## Systems in Scope

1. 角色属性/功力 — 完整数据模型
2. 回合制战斗 (Burst+Read) — 完整流程
3. 武学组合 — 2 套武学 + 1 心法
4. 敌方 AI — 1 个基础 AI
5. 对话系统 — 1 段对话
6. 心境双轴 — 1 次位移
7. 战斗 UI — Intent + 资源条 + 一击决胜
8. 朦胧化 UI — 战后反馈面板

## Progress Log

### Day 1 (2026-06-10) — Completed ✅
- [x] 项目结构 + project.godot
- [x] 核心基础层（EventBus, GameManager, CharacterData, MartialMove）
- [x] 战斗系统骨架（CombatManager, EnemyAI）
- [x] 对话 + 心境 + UI（DialogueManager, DialogueUI, CombatUI, BlurredFeedbackUI）
- [x] 场景整合（Main.tscn）
- [x] Playtest + bug fixes（节点时序问题修复 x2）
- [x] REPORT.md 撰写 + prototypes/index.md 更新

## Next Session

- Epic/Story planning for Foundation + Core layers (`/create-epics layer:foundation`, `/create-epics layer:core`)
- Sprint planning using velocity data from slice
- Gate check for Production stage advancement

## Session Extract — /dev-story 2026-06-11
- Story: production/epics/martial-arts-system/stories/ma-001-martial-arts-yaml-schema.md — ma-001 武学 YAML 数据模型与克制矩阵加载
- Files changed: src/FengZhi.Foundation/MartialArts/MartialArtsDefinitions.cs, src/FengZhi.Foundation/MartialArts/MartialArtsConfigLoader.cs, assets/data/martial-arts/moves.yaml, assets/data/martial-arts/xinfa.yaml, assets/data/martial-arts/qinggong.yaml, assets/data/martial-arts/counter-matrix.yaml, tests/unit/martial-arts/martial_arts_yaml_schema_test.cs
- Test written: tests/unit/martial-arts/martial_arts_yaml_schema_test.cs
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/MartialArts/MartialArtsDefinitions.cs src/FengZhi.Foundation/MartialArts/MartialArtsConfigLoader.cs tests/unit/martial-arts/martial_arts_yaml_schema_test.cs then /story-done production/epics/martial-arts-system/stories/ma-001-martial-arts-yaml-schema.md

## Session Extract — /story-done 2026-06-12
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/martial-arts-system/stories/ma-001-martial-arts-yaml-schema.md — ma-001 武学 YAML 数据模型与克制矩阵加载
- Code review: CHANGES REQUIRED → 修复（doc comments、Theory 精确断言、非法枚举测试、_deserializer 命名）→ 1028/1028 测试通过
- Tech debt logged: 2 items (docs/tech-debt-register.md — 语义校验行号缺失、模型可变性)
- Next recommended: ma-002 招式成长与批注（production/epics/martial-arts-system/stories/ma-002-move-progression-and-annotations.md）；ma-003 伤害与缩放也已 Ready（依赖 ma-001 已解锁）

## Session Extract — /dev-story 2026-06-12
- Story: production/epics/item-system/stories/it-001-item-yaml-schema-and-loader.md — it-001 物品 YAML 数据模型与加载校验
- Files changed: src/FengZhi.Foundation/Items/ItemDefinitions.cs, src/FengZhi.Foundation/Items/ItemConfigLoader.cs, assets/data/items/consumables.yaml, assets/data/items/equipment-templates.yaml, assets/data/items/key-items.yaml, assets/data/items/recipes.yaml, assets/data/items/affix-pools.yaml, tests/unit/items/item_yaml_schema_test.cs
- Test written: tests/unit/items/item_yaml_schema_test.cs
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Items/ItemDefinitions.cs src/FengZhi.Foundation/Items/ItemConfigLoader.cs tests/unit/items/item_yaml_schema_test.cs then /story-done production/epics/item-system/stories/it-001-item-yaml-schema-and-loader.md

## Session Extract — /dev-story 2026-06-12
- Story: production/epics/item-system/stories/it-002-inventory-stacking-and-key-items.md — it-002 背包堆叠、拾取与关键物品约束
- Files changed: src/FengZhi.Foundation/Items/InventoryService.cs, tests/unit/items/inventory_stacking_test.cs
- Test written: tests/unit/items/inventory_stacking_test.cs
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Items/InventoryService.cs tests/unit/items/inventory_stacking_test.cs then /story-done production/epics/item-system/stories/it-002-inventory-stacking-and-key-items.md

## Session Extract — /dev-story 2026-06-12
- Story: production/epics/item-system/stories/it-003-equipment-generation-and-slots.md — it-003 装备品级、词条生成与五槽互斥
- Files changed: src/FengZhi.Foundation/Items/EquipmentService.cs, src/FengZhi.Foundation/Items/InventoryService.cs, tests/unit/items/equipment_generation_test.cs
- Test written: tests/unit/items/equipment_generation_test.cs
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Items/EquipmentService.cs src/FengZhi.Foundation/Items/InventoryService.cs tests/unit/items/equipment_generation_test.cs then /story-done production/epics/item-system/stories/it-003-equipment-generation-and-slots.md

## Session Extract — /dev-story 2026-06-12
- Story: production/epics/item-system/stories/it-004-combat-consumables-and-battle-bag.md — it-004 战斗消耗品与战斗背包契约
- Files changed: src/FengZhi.Foundation/Items/InventoryService.cs, tests/integration/items/combat_consumables_test.cs
- Test written: tests/integration/items/combat_consumables_test.cs
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Items/InventoryService.cs tests/integration/items/combat_consumables_test.cs then /story-done production/epics/item-system/stories/it-004-combat-consumables-and-battle-bag.md

## Session Extract — /dev-story 2026-06-12
- Story: production/epics/item-system/stories/it-005-crafting-alchemy-refine.md — it-005 炼丹、锻造与精炼规则
- Files changed: src/FengZhi.Foundation/Items/CraftingService.cs, src/FengZhi.Foundation/Items/EquipmentService.cs, src/FengZhi.Foundation/Items/InventoryService.cs, tests/unit/items/crafting_alchemy_refine_test.cs
- Test written: tests/unit/items/crafting_alchemy_refine_test.cs
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Items/CraftingService.cs src/FengZhi.Foundation/Items/EquipmentService.cs src/FengZhi.Foundation/Items/InventoryService.cs tests/unit/items/crafting_alchemy_refine_test.cs then /story-done production/epics/item-system/stories/it-005-crafting-alchemy-refine.md

## Session Extract — /dev-story 2026-06-12
- Story: production/epics/item-system/stories/it-006-martial-fragments-teaching.md — it-006 残卷秘籍自学与传授契约
- Files changed: src/FengZhi.Foundation/Items/MartialFragmentTeachingService.cs, src/FengZhi.Foundation/Items/InventoryService.cs, tests/integration/items/martial_fragments_teaching_test.cs
- Test written: tests/integration/items/martial_fragments_teaching_test.cs
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Items/MartialFragmentTeachingService.cs src/FengZhi.Foundation/Items/InventoryService.cs tests/integration/items/martial_fragments_teaching_test.cs then /story-done production/epics/item-system/stories/it-006-martial-fragments-teaching.md

## Session Extract — /dev-story 2026-06-12
- Story: production/epics/item-system/stories/it-007-economy-shop-and-auction.md — it-007 银两、商店、黑市与拍卖
- Files changed: src/FengZhi.Foundation/Items/EconomyService.cs, tests/unit/items/economy_shop_auction_test.cs
- Test written: tests/unit/items/economy_shop_auction_test.cs
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Items/EconomyService.cs tests/unit/items/economy_shop_auction_test.cs then /story-done production/epics/item-system/stories/it-007-economy-shop-and-auction.md

## Session Extract — /dev-story 2026-06-13
- Story: production/epics/item-system/stories/it-008-item-save-and-query-contracts.md — it-008 物品存档与跨系统查询契约
- Files changed: src/FengZhi.Foundation/Items/ItemSystemService.cs, src/FengZhi.Foundation/Items/InventoryService.cs, src/FengZhi.Foundation/Items/EquipmentService.cs, src/FengZhi.Foundation/Items/EconomyService.cs, tests/integration/items/item_save_query_contracts_test.cs
- Test written: tests/integration/items/item_save_query_contracts_test.cs
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Items/ItemSystemService.cs src/FengZhi.Foundation/Items/InventoryService.cs src/FengZhi.Foundation/Items/EquipmentService.cs src/FengZhi.Foundation/Items/EconomyService.cs tests/integration/items/item_save_query_contracts_test.cs then /story-done production/epics/item-system/stories/it-008-item-save-and-query-contracts.md

## Session Extract — /story-done 2026-06-13
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/item-system/stories/it-008-item-save-and-query-contracts.md — it-008 物品存档与跨系统查询契约
- Code review: CHANGES REQUIRED -> 修复黑市易物原子性与移除 `qualityOverride` 僵尸参数 -> APPROVED
- Test evidence: `ItemSaveQueryContractsTest` 6/6 passed; Foundation full suite 1104/1104 passed
- Tech debt logged: None
- Next recommended: Foundation/Core/Platform planned epics are Done; choose Feature/Persistence presentation planning or sprint QA close-out

## Session Extract — /dev-story 2026-06-13
- Story: production/epics/romance-system/stories/rs-001-romance-state-and-milestone-floor.md — rs-001 感情状态与里程碑地板钳位
- Files changed: src/FengZhi.Foundation/Romance/RomanceEvents.cs, src/FengZhi.Foundation/Romance/RomanceState.cs, src/FengZhi.Foundation/Romance/MilestoneRegistry.cs, src/FengZhi.Foundation/Romance/RomanceService.cs, src/FengZhi.Foundation/NpcState/NpcStateManager.cs, tests/unit/romance/romance_state_and_milestone_floor_test.cs
- Test written: tests/unit/romance/romance_state_and_milestone_floor_test.cs
- Test evidence: `RomanceStateAndMilestoneFloorTest` 12/12 passed; Foundation full suite 1116/1116 passed
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Romance/RomanceEvents.cs src/FengZhi.Foundation/Romance/RomanceState.cs src/FengZhi.Foundation/Romance/MilestoneRegistry.cs src/FengZhi.Foundation/Romance/RomanceService.cs src/FengZhi.Foundation/NpcState/NpcStateManager.cs tests/unit/romance/romance_state_and_milestone_floor_test.cs then /story-done production/epics/romance-system/stories/rs-001-romance-state-and-milestone-floor.md

## Session Extract — /story-done 2026-06-13
- Verdict: COMPLETE
- Story: production/epics/romance-system/stories/rs-001-romance-state-and-milestone-floor.md — rs-001 感情状态与里程碑地板钳位
- Code review: CHANGES REQUIRED -> 修复 raw attitude writer 绕过、RemoveFlag no-op 事件、真实 adapter 测试 -> APPROVED
- Test evidence: `RomanceStateAndMilestoneFloorTest` 15/15 passed; Foundation full suite 1120/1120 passed
- Tech debt logged: None
- Next recommended: rs-002 里程碑解锁顺序与诀别覆写 — production/epics/romance-system/stories/rs-002-milestone-unlock-and-force-break.md

## Session Extract — /dev-story 2026-06-13
- Story: production/epics/romance-system/stories/rs-002-milestone-unlock-and-force-break.md — rs-002 里程碑解锁顺序与诀别覆写
- Files changed: src/FengZhi.Foundation/Romance/RomanceEvents.cs, src/FengZhi.Foundation/Romance/RomanceState.cs, src/FengZhi.Foundation/Romance/MilestoneRegistry.cs, src/FengZhi.Foundation/Romance/RomanceService.cs, tests/unit/romance/romance_state_and_milestone_floor_test.cs, tests/unit/romance/milestone_unlock_and_force_break_test.cs
- Test written: tests/unit/romance/milestone_unlock_and_force_break_test.cs
- Test evidence: `MilestoneUnlockAndForceBreakTest` + `RomanceStateAndMilestoneFloorTest` 34/34 passed; Foundation full suite 1139/1139 passed
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Romance/RomanceEvents.cs src/FengZhi.Foundation/Romance/RomanceState.cs src/FengZhi.Foundation/Romance/MilestoneRegistry.cs src/FengZhi.Foundation/Romance/RomanceService.cs tests/unit/romance/romance_state_and_milestone_floor_test.cs tests/unit/romance/milestone_unlock_and_force_break_test.cs then /story-done production/epics/romance-system/stories/rs-002-milestone-unlock-and-force-break.md

## Session Extract — /story-done 2026-06-13
- Verdict: COMPLETE
- Story: production/epics/romance-system/stories/rs-002-milestone-unlock-and-force-break.md — rs-002 里程碑解锁顺序与诀别覆写
- Code review: CHANGES REQUIRED -> 修复 `force_break` partial-write risk 并补 regression test -> APPROVED
- Test evidence: `MilestoneUnlockAndForceBreakTest` 21/21 passed; Foundation full suite 1141/1141 passed
- Tech debt logged: None
- Next recommended: rs-003 结缘流程互斥与拒绝锁定 — production/epics/romance-system/stories/rs-003-bond-flow-exclusivity-and-decline.md

## Session Extract — /dev-story 2026-06-13
- Story: production/epics/romance-system/stories/rs-003-bond-flow-exclusivity-and-decline.md — rs-003 结缘流程互斥与拒绝锁定
- Files changed: src/FengZhi.Foundation/Romance/RomanceEvents.cs, src/FengZhi.Foundation/Romance/RomanceState.cs, src/FengZhi.Foundation/Romance/RomanceService.cs, tests/unit/romance/romance_state_and_milestone_floor_test.cs, tests/unit/romance/milestone_unlock_and_force_break_test.cs, tests/integration/romance/bond_flow_exclusivity_and_decline_test.cs, production/epics/romance-system/stories/rs-003-bond-flow-exclusivity-and-decline.md
- Test written: tests/integration/romance/bond_flow_exclusivity_and_decline_test.cs
- Test evidence: `BondFlowExclusivityAndDeclineTest` 6/6 passed; Romance filtered suite 42/42 passed; Foundation full suite 1147/1147 passed
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Romance/RomanceEvents.cs src/FengZhi.Foundation/Romance/RomanceState.cs src/FengZhi.Foundation/Romance/RomanceService.cs tests/unit/romance/romance_state_and_milestone_floor_test.cs tests/unit/romance/milestone_unlock_and_force_break_test.cs tests/integration/romance/bond_flow_exclusivity_and_decline_test.cs then /story-done production/epics/romance-system/stories/rs-003-bond-flow-exclusivity-and-decline.md

## Session Extract — /story-done 2026-06-13
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/romance-system/stories/rs-003-bond-flow-exclusivity-and-decline.md — rs-003 结缘流程互斥与拒绝锁定
- Code review: CHANGES REQUIRED -> 修复全局结缘状态归属、`ConfirmBond` 原子性、拒绝锁前置条件与 typed event 覆盖 -> APPROVED
- Test evidence: `BondFlowExclusivityAndDeclineTest` 10/10 passed; Romance filtered suite 46/46 passed; Foundation full suite 1151/1151 passed
- Tech debt logged: None
- Next recommended: rs-004 结局变体解析器 — production/epics/romance-system/stories/rs-004-ending-variant-resolver.md

## Session Extract — /dev-story 2026-06-13
- Story: production/epics/romance-system/stories/rs-004-ending-variant-resolver.md — rs-004 结局变体解析器
- Files changed: src/FengZhi.Foundation/Romance/EndingResolver.cs, tests/integration/romance/ending_variant_resolver_test.cs, production/epics/romance-system/stories/rs-004-ending-variant-resolver.md
- Test written: tests/integration/romance/ending_variant_resolver_test.cs
- Test evidence: `EndingVariantResolverTest` 11/11 passed; Romance filtered suite 57/57 passed; Foundation full suite 1162/1162 passed
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Romance/EndingResolver.cs tests/integration/romance/ending_variant_resolver_test.cs then /story-done production/epics/romance-system/stories/rs-004-ending-variant-resolver.md

## Session Extract — /story-done 2026-06-13
- Verdict: COMPLETE
- Story: production/epics/romance-system/stories/rs-004-ending-variant-resolver.md — rs-004 结局变体解析器
- Code review: CHANGES REQUIRED -> 修复 9 个 `MindsetZone` 到 5 个核心 `BaseEnding` script identity 的归一化，并补 16 变体总数覆盖 -> APPROVED
- Test evidence: `EndingVariantResolverTest` 13/13 passed; Romance filtered suite 59/59 passed; Foundation full suite 1164/1164 passed
- Tech debt logged: None
- Next recommended: rs-005 彗星存在感与传闻概率 — production/epics/romance-system/stories/rs-005-comet-presence-and-rumor-chance.md

## Session Extract — /dev-story 2026-06-13
- Story: production/epics/romance-system/stories/rs-005-comet-presence-and-rumor-chance.md — rs-005 彗星存在感与传闻概率
- Files changed: src/FengZhi.Foundation/Romance/CometPresenceTracker.cs, src/FengZhi.Foundation/Romance/RomanceState.cs, tests/unit/romance/comet_presence_and_rumor_chance_test.cs, production/epics/romance-system/stories/rs-005-comet-presence-and-rumor-chance.md
- Test written: tests/unit/romance/comet_presence_and_rumor_chance_test.cs
- Test evidence: `CometPresenceAndRumorChanceTest` 11/11 passed; Romance filtered suite 70/70 passed; Foundation full suite 1175/1175 passed
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Romance/CometPresenceTracker.cs src/FengZhi.Foundation/Romance/RomanceState.cs tests/unit/romance/comet_presence_and_rumor_chance_test.cs then /story-done production/epics/romance-system/stories/rs-005-comet-presence-and-rumor-chance.md

## Session Extract — /story-done 2026-06-13
- Verdict: COMPLETE
- Story: production/epics/romance-system/stories/rs-005-comet-presence-and-rumor-chance.md — rs-005 彗星存在感与传闻概率
- Code review: APPROVED WITH SUGGESTIONS -> 修复 DataRegistry 调参注册缺口 -> COMPLETE
- Test evidence: `CometPresenceAndRumorChanceTest` 15/15 passed; Romance filtered suite 74/74 passed; Foundation full suite 1179/1179 passed
- Tech debt logged: None
- Next recommended: rs-006 感情存档与联系计时续算 — production/epics/romance-system/stories/rs-006-romance-save-and-contact-continuation.md

## Session Extract — /dev-story 2026-06-13
- Story: production/epics/romance-system/stories/rs-006-romance-save-and-contact-continuation.md — rs-006 感情存档与联系计时续算
- Files changed: src/FengZhi.Foundation/Romance/CometPresenceTracker.cs, src/FengZhi.Foundation/Romance/RomanceState.cs, src/FengZhi.Foundation/Romance/RomancePersistenceAdapter.cs, tests/integration/romance/romance_save_and_contact_continuation_test.cs, production/epics/romance-system/stories/rs-006-romance-save-and-contact-continuation.md
- Test written: tests/integration/romance/romance_save_and_contact_continuation_test.cs
- Test evidence: `RomanceSaveAndContactContinuationTest` 4/4 passed; Romance filtered suite 78/78 passed; Foundation full suite 1183/1183 passed
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Romance/CometPresenceTracker.cs src/FengZhi.Foundation/Romance/RomanceState.cs src/FengZhi.Foundation/Romance/RomancePersistenceAdapter.cs tests/integration/romance/romance_save_and_contact_continuation_test.cs then /story-done production/epics/romance-system/stories/rs-006-romance-save-and-contact-continuation.md

## Session Extract — /code-review-fix 2026-06-13
- Story: production/epics/romance-system/stories/rs-006-romance-save-and-contact-continuation.md — rs-006 感情存档与联系计时续算
- Fixes: staged romance restore validation before mutation; bonded_heroine uniqueness validation; invalid bonded boolean safe failure; Sign/Letter last-contact write before counter increment; expanded new-run reset coverage
- Test evidence: `RomanceSaveAndContactContinuationTest` 8/8 passed; Romance filtered suite 82/82 passed; Foundation full suite 1187/1187 passed
- Blockers: None
- Next: re-run /code-review src/FengZhi.Foundation/Romance/CometPresenceTracker.cs src/FengZhi.Foundation/Romance/RomanceState.cs src/FengZhi.Foundation/Romance/RomancePersistenceAdapter.cs tests/integration/romance/romance_save_and_contact_continuation_test.cs

## Session Extract — /code-review-fix 2026-06-13
- Story: production/epics/romance-system/stories/rs-006-romance-save-and-contact-continuation.md — rs-006 感情存档与联系计时续算
- Fixes: added NPC State immediate flag writer for persistence; RestoreRomanceFlags now bypasses death guards and dialogue queues; ResetRomanceForNewRun clears current and pending romance flags immediately
- Test evidence: `RomanceSaveAndContactContinuationTest` 12/12 passed; Romance filtered suite 86/86 passed; Foundation full suite 1191/1191 passed
- Blockers: None
- Next: re-run /code-review src/FengZhi.Foundation/NpcState/NpcStateManager.cs src/FengZhi.Foundation/Romance/RomanceState.cs src/FengZhi.Foundation/Romance/RomancePersistenceAdapter.cs tests/integration/romance/romance_save_and_contact_continuation_test.cs

## Session Extract — /story-done 2026-06-14
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/romance-system/stories/rs-006-romance-save-and-contact-continuation.md — rs-006 感情存档与联系计时续算
- Criteria: 4/4 passing; code review APPROVED WITH SUGGESTIONS after fixes
- Test evidence: `RomanceSaveAndContactContinuationTest` plus bond/force_break/NPC immediate event regressions passed; Foundation full suite 1198/1198 passed
- Tech debt logged: None
- Next recommended: rs-007 文学化关系展示契约 — run `/story-readiness production/epics/romance-system/stories/rs-007-literary-relationship-presentation-contract.md` after replacing placeholder TR-ID

## Session Extract — /dev-story 2026-06-14
- Story: production/epics/romance-system/stories/rs-007-literary-relationship-presentation-contract.md — rs-007 文学化关系展示契约
- Files changed: src/FengZhi.Foundation/Romance/RomanceService.cs, src/FengZhi.Foundation/Romance/RelationshipPresentation.cs, tests/unit/romance/relationship_presentation_contract_test.cs, production/qa/evidence/rs-007-literary-relationship-presentation-contract-evidence.md, production/epics/romance-system/stories/rs-007-literary-relationship-presentation-contract.md
- Test written: tests/unit/romance/relationship_presentation_contract_test.cs (16 checks)
- Verification: `dotnet test --filter RelationshipPresentationContractTest` passed 16/16; `dotnet test --filter Romance` passed 107/107; `dotnet test` passed 1214/1214
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Romance/RomanceService.cs src/FengZhi.Foundation/Romance/RelationshipPresentation.cs tests/unit/romance/relationship_presentation_contract_test.cs production/qa/evidence/rs-007-literary-relationship-presentation-contract-evidence.md then /story-done production/epics/romance-system/stories/rs-007-literary-relationship-presentation-contract.md

## Session Extract — /story-done 2026-06-14
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/romance-system/stories/rs-007-literary-relationship-presentation-contract.md — rs-007 文学化关系展示契约
- Criteria: 4/4 passing; code review APPROVED WITH SUGGESTIONS
- Test evidence: `RelationshipPresentationContractTest` passed 16/16; UI evidence doc approved; Foundation full suite 1214/1214 passed
- Tech debt logged: None
- Next recommended: romance-system Epic MVP/P1 stories are complete; run `/smoke-check sprint` or select the next Epic/story via `/sprint-status`

<!-- QA RUN: 2026-06-14 | Sprint: sprint-3 | Verdict: APPROVED WITH CONDITIONS | Report: production/qa/qa-signoff-sprint-3-2026-06-14.md -->

## Session Extract — /dev-story 2026-06-14
- Story: production/epics/combat-ui/stories/cu-001-combat-ui-foundation-and-event-adapter.md — cu-001 战斗 UI 基础层与事件适配
- Files changed: src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs, src/FengZhi.Foundation/CombatUi/CombatUiEventAdapter.cs, tests/integration/combat-ui/combat_ui_foundation_event_adapter_test.cs, production/qa/evidence/cu-001-combat-ui-foundation-and-event-adapter-evidence.md, production/epics/combat-ui/stories/cu-001-combat-ui-foundation-and-event-adapter.md
- Test written: tests/integration/combat-ui/combat_ui_foundation_event_adapter_test.cs (7 checks)
- Verification: `dotnet test --filter CombatUiFoundationEventAdapterTest` passed 7/7; `dotnet test` passed 1221/1221
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs src/FengZhi.Foundation/CombatUi/CombatUiEventAdapter.cs tests/integration/combat-ui/combat_ui_foundation_event_adapter_test.cs production/qa/evidence/cu-001-combat-ui-foundation-and-event-adapter-evidence.md then /story-done production/epics/combat-ui/stories/cu-001-combat-ui-foundation-and-event-adapter.md

## Session Extract — /story-done 2026-06-14
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/combat-ui/stories/cu-001-combat-ui-foundation-and-event-adapter.md — cu-001 战斗 UI 基础层与事件适配
- Criteria: 6/6 passing; code review APPROVED WITH SUGGESTIONS
- Test evidence: `CombatUiFoundationEventAdapterTest` passed 9/9; Foundation full suite 1223/1223 passed; integration evidence documented
- Tech debt logged: None
- Notes: real Godot scene visibility and dual-focus walkthrough evidence deferred to later combat UI stories
- Next recommended: cu-002 意图图标与 HUD 汇总 — production/epics/combat-ui/stories/cu-002-intent-icons-and-hud-summary.md

<!-- QA-PLAN: 2026-06-14 | System: sprint-4 exploration-insight | Plan written: production/qa/qa-plan-sprint-4-2026-06-14.md -->

## Session Extract — /dev-story 2026-06-14
- Story: production/epics/combat-ui/stories/cu-002-intent-icons-and-hud-summary.md — cu-002 意图图标与 HUD 汇总
- Files changed: src/FengZhi.Foundation/Combat/BattleEventBus.cs, src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs, src/FengZhi.Foundation/CombatUi/CombatUiEventAdapter.cs, src/FengZhi.Foundation/CombatUi/CombatUiLayers.cs, tests/integration/combat-ui/combat_ui_intent_icons_hud_summary_test.cs, production/qa/evidence/cu-002-intent-icons-and-hud-summary-evidence.md, production/epics/combat-ui/stories/cu-002-intent-icons-and-hud-summary.md
- Test written: tests/integration/combat-ui/combat_ui_intent_icons_hud_summary_test.cs (7 checks)
- Verification: `dotnet test tests/Foundation/Foundation.Tests.csproj --filter CombatUi` passed 16/16
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Combat/BattleEventBus.cs src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs src/FengZhi.Foundation/CombatUi/CombatUiEventAdapter.cs src/FengZhi.Foundation/CombatUi/CombatUiLayers.cs tests/integration/combat-ui/combat_ui_intent_icons_hud_summary_test.cs production/qa/evidence/cu-002-intent-icons-and-hud-summary-evidence.md then /story-done production/epics/combat-ui/stories/cu-002-intent-icons-and-hud-summary.md

## Session Extract — /dev-story 2026-06-14
- Story: production/epics/exploration-insight/stories/ei-001-insight-node-registry-and-scene-activation.md — ei-001 InsightNode 数据模型、注册表与场景激活
- Files changed: src/FengZhi.Foundation/Exploration/InsightNode.cs, src/FengZhi.Foundation/Exploration/InsightNodeRegistry.cs, tests/unit/exploration/insight_node_registry_test.cs, production/epics/exploration-insight/stories/ei-001-insight-node-registry-and-scene-activation.md, production/sprint-status.yaml
- Test written: tests/unit/exploration/insight_node_registry_test.cs (9 tests)
- Verification: `dotnet test tests/Foundation/Foundation.Tests.csproj --filter InsightNodeRegistryTest` passed 9/9; `dotnet test tests/Foundation/Foundation.Tests.csproj` passed 1239/1239
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Exploration/InsightNode.cs src/FengZhi.Foundation/Exploration/InsightNodeRegistry.cs tests/unit/exploration/insight_node_registry_test.cs then /story-done production/epics/exploration-insight/stories/ei-001-insight-node-registry-and-scene-activation.md

## Session Extract — /story-done 2026-06-14
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/exploration-insight/stories/ei-001-insight-node-registry-and-scene-activation.md — ei-001 InsightNode 数据模型、注册表与场景激活
- Criteria: 4/4 passing; code review APPROVED WITH SUGGESTIONS
- Test evidence: `InsightNodeRegistryTest` passed 9/9; diagnostics clean
- Tech debt logged: None
- Next recommended: ei-002 洞察距离检测、门槛检定与重访发现 — production/epics/exploration-insight/stories/ei-002-insight-detection-threshold-and-revisit.md

## Session Extract — /dev-story 2026-06-14
- Story: production/epics/exploration-insight/stories/ei-002-insight-detection-threshold-and-revisit.md — ei-002 洞察距离检测、门槛检定与重访发现
- Files changed: src/FengZhi.Foundation/Exploration/InsightEvents.cs, src/FengZhi.Foundation/Exploration/ProximityDetector.cs, tests/unit/exploration/insight_detection_threshold_test.cs, production/epics/exploration-insight/stories/ei-002-insight-detection-threshold-and-revisit.md, production/sprint-status.yaml
- Test written: tests/unit/exploration/insight_detection_threshold_test.cs (9 tests)
- Verification: `dotnet test tests/Foundation/Foundation.Tests.csproj --filter InsightDetectionThresholdTest` passed 9/9; `dotnet test tests/Foundation/Foundation.Tests.csproj` passed 1250/1250
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Exploration/InsightEvents.cs src/FengZhi.Foundation/Exploration/ProximityDetector.cs tests/unit/exploration/insight_detection_threshold_test.cs then /story-done production/epics/exploration-insight/stories/ei-002-insight-detection-threshold-and-revisit.md

## Session Extract — /story-done 2026-06-14
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/combat-ui/stories/cu-002-intent-icons-and-hud-summary.md — cu-002 意图图标与 HUD 汇总
- Criteria: 6/6 passing; code review APPROVED WITH SUGGESTIONS
- Test evidence: `CombatUiIntentIconsHudSummaryTest` + `CombatUiFoundationEventAdapterTest` passed 18/18 via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter CombatUi`; Foundation full suite previously passed 1241/1241
- Tech debt logged: None
- Notes: `intent_icon_fadein_duration` tuning knob and real Godot scene animation/focus walkthrough remain advisory follow-ups
- Next recommended: cu-003 资源条与伤害反馈 — production/epics/combat-ui/stories/cu-003-resource-bars-and-damage-feedback.md, or cu-004 招式选择面板与预览卡 — production/epics/combat-ui/stories/cu-004-move-selection-panel-and-preview-card.md

## Session Extract — /story-done 2026-06-14
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/exploration-insight/stories/ei-002-insight-detection-threshold-and-revisit.md — ei-002 洞察距离检测、门槛检定与重访发现
- Acceptance criteria: 5/5 passing; `InsightDetectionThresholdTest` passed 9/9
- Code review: Complete — approved with suggestions
- Tech debt logged: 2 items in docs/tech-debt-register.md
- Next recommended: ei-003 多节点 stagger、忽略与 linger 恢复 — production/epics/exploration-insight/stories/ei-003-multi-node-stagger-and-cue-linger.md, or ei-004 发现奖励分派：Clue 与 CodePhrase — production/epics/exploration-insight/stories/ei-004-discovery-reward-dispatch.md

## Session Extract — /dev-story 2026-06-14
- Story: production/epics/combat-ui/stories/cu-003-resource-bars-and-damage-feedback.md — cu-003 资源条与伤害反馈
- Files changed: src/FengZhi.Foundation/Combat/BattleEventBus.cs, src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs, src/FengZhi.Foundation/CombatUi/CombatUiEventAdapter.cs, src/FengZhi.Foundation/CombatUi/CombatUiLayers.cs, tests/integration/combat-ui/combat_ui_resource_bars_damage_feedback_test.cs, production/qa/evidence/cu-003-resource-bars-and-damage-feedback-evidence.md, production/epics/combat-ui/stories/cu-003-resource-bars-and-damage-feedback.md, production/epics/combat-ui/EPIC.md
- Test written: tests/integration/combat-ui/combat_ui_resource_bars_damage_feedback_test.cs (9 tests)
- Verification: `dotnet test tests/Foundation/Foundation.Tests.csproj --filter CombatUi` passed 31/31; `dotnet test tests/Foundation/Foundation.Tests.csproj` passed 1273/1273
- Blockers: None
- Notes: Visual/Feel 实机走查仍需在 Godot 场景中确认资源条 Tween、伤害浮字可读性、破绽脉冲与池化焦点释放。
- Next: /code-review src/FengZhi.Foundation/Combat/BattleEventBus.cs src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs src/FengZhi.Foundation/CombatUi/CombatUiEventAdapter.cs src/FengZhi.Foundation/CombatUi/CombatUiLayers.cs tests/integration/combat-ui/combat_ui_resource_bars_damage_feedback_test.cs production/qa/evidence/cu-003-resource-bars-and-damage-feedback-evidence.md then /story-done production/epics/combat-ui/stories/cu-003-resource-bars-and-damage-feedback.md

## Session Extract — /dev-story 2026-06-14
- Story: production/epics/exploration-insight/stories/ei-003-multi-node-stagger-and-cue-linger.md — ei-003 多节点 stagger、忽略与 linger 恢复
- Files changed: src/FengZhi.Foundation/Exploration/InsightEvents.cs, src/FengZhi.Foundation/Exploration/ProximityDetector.cs, tests/unit/exploration/insight_cue_timing_test.cs, production/epics/exploration-insight/stories/ei-003-multi-node-stagger-and-cue-linger.md, production/sprint-status.yaml
- Test written: tests/unit/exploration/insight_cue_timing_test.cs (9 tests)
- Verification: `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InsightCueTimingTest|InsightDetectionThresholdTest"` passed 19/19; `dotnet test tests/Foundation/Foundation.Tests.csproj --filter exploration` passed 29/29
- Full suite note: standard full run crashed test host nondeterministically; rerun with `RunConfiguration.DisableParallelization=true` completed with 1 unrelated combat-ui failure in `CombatUiResourceBarsDamageFeedbackTest.HudPanel_AppliesResourcesDamageNumbersAndStaggerCues`
- Blockers: None for ei-003 scope
- Next: /code-review src/FengZhi.Foundation/Exploration/InsightEvents.cs src/FengZhi.Foundation/Exploration/ProximityDetector.cs tests/unit/exploration/insight_cue_timing_test.cs then /story-done production/epics/exploration-insight/stories/ei-003-multi-node-stagger-and-cue-linger.md

## Session Extract — /story-done 2026-06-14
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/exploration-insight/stories/ei-003-multi-node-stagger-and-cue-linger.md — ei-003 多节点 stagger、忽略与 linger 恢复
- Test evidence: `InsightCueTimingTest|InsightDetectionThresholdTest` passed 19/19 via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InsightCueTimingTest|InsightDetectionThresholdTest"`
- Code review: Complete — approved with suggestions
- Tech debt logged: 3 items (`Detect()` linger tracking bypass, idempotent hidden event duplication, pending queue cleanup deferred to `ei-005`)
- Next recommended: ei-004 发现奖励分派：Clue 与 CodePhrase — production/epics/exploration-insight/stories/ei-004-discovery-reward-dispatch.md

## Session Extract — /story-done 2026-06-14
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/combat-ui/stories/cu-003-resource-bars-and-damage-feedback.md — cu-003 资源条与伤害反馈
- Criteria: 6/6 passing
- Test evidence: `CombatUiResourceBarsDamageFeedbackTest` and CombatUi suite passed 31/31 via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter CombatUi`
- Code review: Complete — APPROVED WITH SUGGESTIONS
- Tech debt logged: None
- Notes: Visual/Feel 实机走查仍需在真实 Godot 战斗场景中确认资源条闪白与 Tween 体感、破绽脉冲、同帧 6 个伤害浮字可读性，以及池化 Label 回收后的焦点释放。
- Next recommended: ei-004 发现奖励分派：Clue 与 CodePhrase — production/epics/exploration-insight/stories/ei-004-discovery-reward-dispatch.md；若继续 Presentation/UI 并行线，可做 cu-004 招式选择面板与预览卡。

## Session Extract — /dev-story 2026-06-14
- Story: production/epics/exploration-insight/stories/ei-004-discovery-reward-dispatch.md — ei-004 发现奖励分派：Clue 与 CodePhrase
- Files changed: src/FengZhi.Foundation/Exploration/DiscoveryDispatcher.cs, src/FengZhi.Foundation/Exploration/InsightEvents.cs, tests/integration/exploration/insight_reward_dispatch_test.cs, production/epics/exploration-insight/stories/ei-004-discovery-reward-dispatch.md, production/sprint-status.yaml
- Test written: tests/integration/exploration/insight_reward_dispatch_test.cs (10 tests)
- Verification: `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InsightRewardDispatchTest|InsightCueTimingTest|InsightDetectionThresholdTest"` passed 31/31; `dotnet test tests/Foundation/Foundation.Tests.csproj --filter exploration` passed 41/41
- Code review fix: post-preflight downstream rejection removed from port commit contract; duplicate CodePhrase learn covered as safe no-op
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Exploration/DiscoveryDispatcher.cs src/FengZhi.Foundation/Exploration/InsightEvents.cs tests/integration/exploration/insight_reward_dispatch_test.cs then /story-done production/epics/exploration-insight/stories/ei-004-discovery-reward-dispatch.md

## Session Extract — /story-done 2026-06-14
- Verdict: COMPLETE
- Story: production/epics/exploration-insight/stories/ei-004-discovery-reward-dispatch.md — ei-004 发现奖励分派：Clue 与 CodePhrase
- Criteria: 6/6 passing; post-preflight `Try*` commit contract added for Narrative and CodePhrase ports
- Test evidence: `InsightRewardDispatchTest|InsightCueTimingTest|InsightDetectionThresholdTest` passed 34/34; exploration suite passed 44/44
- Code review: Complete — Lean mode; user confirmed post-fix small contract change reviewed
- Tech debt logged: 1 item (`DiscoveryDispatcher` monologue request remains a presentation-side effect if reward commit rejects)
- Next recommended: ei-005 存档恢复、场景卸载清理与战斗/对话锁恢复 — production/epics/exploration-insight/stories/ei-005-save-scene-lock-recovery.md

## Session Extract — /dev-story 2026-06-14
- Story: production/epics/exploration-insight/stories/ei-005-save-scene-lock-recovery.md — ei-005 存档恢复、场景卸载清理与战斗/对话锁恢复
- Files changed: src/FengZhi.Foundation/Exploration/ExplorationPersistenceAdapter.cs, src/FengZhi.Foundation/Exploration/ExplorationLockGuard.cs, src/FengZhi.Foundation/Exploration/InsightNodeRegistry.cs, src/FengZhi.Foundation/Exploration/ProximityDetector.cs, src/FengZhi.Foundation/StateMachine/GameStateLock.cs, tests/integration/exploration/insight_save_scene_lock_test.cs, production/epics/exploration-insight/stories/ei-005-save-scene-lock-recovery.md, production/sprint-status.yaml
- Test written: tests/integration/exploration/insight_save_scene_lock_test.cs (6 tests)
- Verification: `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "InsightSaveSceneLockTest|InsightCueTimingTest|InsightNodeRegistryTest|InsightRewardDispatchTest"` passed 40/40; `dotnet test tests/Foundation/Foundation.Tests.csproj --filter exploration` passed 50/50; full `dotnet test` passed 1294/1294
- Deviations: Story-readiness documentation gaps remain (Estimate, Out of Scope, Control Manifest Rules, Engine Notes, Performance Notes); implementation followed those constraints but did not add the missing sections.
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/Exploration/ExplorationPersistenceAdapter.cs src/FengZhi.Foundation/Exploration/ExplorationLockGuard.cs src/FengZhi.Foundation/Exploration/InsightNodeRegistry.cs src/FengZhi.Foundation/Exploration/ProximityDetector.cs src/FengZhi.Foundation/StateMachine/GameStateLock.cs tests/integration/exploration/insight_save_scene_lock_test.cs then /story-done production/epics/exploration-insight/stories/ei-005-save-scene-lock-recovery.md

## Session Extract — /story-done 2026-06-14
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/exploration-insight/stories/ei-005-save-scene-lock-recovery.md — ei-005 存档恢复、场景卸载清理与战斗/对话锁恢复
- Criteria: 6/6 passing; integration evidence covers save-only-investigated, restore validation, scene unload cleanup, lock pause/resume, nested locks, and ignored-node regression
- Test evidence: `InsightSaveSceneLockTest|InsightCueTimingTest|InsightNodeRegistryTest|InsightRewardDispatchTest` passed 41/41; exploration suite passed 51/51; full `dotnet test` passed 1295/1295
- Code review: Complete — Lean mode; `/code-review` approved after lock-pause regression fix
- Tech debt logged: 1 item (story-readiness documentation gaps)
- Next recommended: Sprint 4 Must Have complete; run `/smoke-check sprint`, then `/team-qa sprint`

<!-- QA RUN: 2026-06-14 | Sprint: sprint-4 | Verdict: APPROVED WITH CONDITIONS | Report: production/qa/qa-signoff-sprint-4-2026-06-14.md -->

<!-- QA-PLAN: 2026-06-14 | System: sprint-5-combat-ui | Plan written: production/qa/qa-plan-sprint-5-2026-06-14.md -->

## Session Extract — /dev-story 2026-06-15
- Story: production/epics/combat-ui/stories/cu-004-move-selection-panel-and-preview-card.md — cu-004 招式选择面板与预览卡
- Files changed: src/FengZhi.Foundation/CombatUi/CombatUiMoveSelection.cs, src/FengZhi.Foundation/Presentation/Shared/FocusManagement.cs, tests/integration/combat-ui/combat_ui_move_selection_panel_test.cs, production/epics/combat-ui/stories/cu-004-move-selection-panel-and-preview-card.md, production/sprint-status.yaml
- Test written: tests/integration/combat-ui/combat_ui_move_selection_panel_test.cs (17 tests)
- Verification: `DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 dotnet test tests/Foundation/Foundation.Tests.csproj --filter FullyQualifiedName~CombatUiMoveSelectionPanelTest --no-restore` passed 17/17 via SDK 8.0.421; full `dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore` passed 1312/1312 via SDK 8.0.421
- Notes: Default SDK 10.0.300 fails before build with MSBuild task-host error; tests require temporary SDK 8 `global.json` plus `DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0`.
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/CombatUi/CombatUiMoveSelection.cs tests/integration/combat-ui/combat_ui_move_selection_panel_test.cs then /story-done production/epics/combat-ui/stories/cu-004-move-selection-panel-and-preview-card.md

## Session Extract — /story-done 2026-06-15
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/combat-ui/stories/cu-004-move-selection-panel-and-preview-card.md — cu-004 招式选择面板与预览卡
- Acceptance criteria: 8/8 passing; automated traceability covered by `CombatUiMoveSelectionPanelTest`
- Test evidence: `CombatUiMoveSelectionPanelTest` passed 18/18; full `Foundation.Tests` passed 1313/1313 via SDK 8.0.421
- Code review: Complete — APPROVED WITH SUGGESTIONS after FocusManager lifecycle fix
- Tech debt logged: 1 item (manual UI walkthrough evidence pending)
- Next recommended: cu-005 反制与决胜行动提示 — production/epics/combat-ui/stories/cu-005-counter-and-decisive-action-prompts.md

## Session Extract — /dev-story 2026-06-15
- Story: production/epics/combat-ui/stories/cu-005-counter-and-decisive-action-prompts.md — cu-005 反制与决胜行动提示
- Files changed: src/FengZhi.Foundation/CombatUi/CombatUiMoveSelection.cs, tests/integration/combat-ui/combat_ui_counter_decisive_prompt_test.cs, production/epics/combat-ui/stories/cu-005-counter-and-decisive-action-prompts.md, production/sprint-status.yaml
- Test written: tests/integration/combat-ui/combat_ui_counter_decisive_prompt_test.cs (17 tests)
- Verification: `CombatUiCounterDecisivePromptTest` passed 17/17; `CombatUiMoveSelectionPanelTest` passed 18/18; full `Foundation.Tests` passed 1330/1330 via SDK 8.0.421 with `DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0`
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/CombatUi/CombatUiMoveSelection.cs tests/integration/combat-ui/combat_ui_counter_decisive_prompt_test.cs then /story-done production/epics/combat-ui/stories/cu-005-counter-and-decisive-action-prompts.md

## Session Extract — /story-done 2026-06-15
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/combat-ui/stories/cu-005-counter-and-decisive-action-prompts.md — cu-005 反制与决胜行动提示
- Tech debt logged: 1 item (manual UI walkthrough evidence pending)
- Next recommended: production/epics/combat-ui/stories/cu-008-dual-focus-and-gamepad-navigation.md — blocked by S5-Preflight readiness cleanup

## Session Extract — /dev-story 2026-06-15
- Story: production/epics/combat-ui/stories/cu-008-dual-focus-and-gamepad-navigation.md — cu-008 双焦点与手柄导航
- Files changed: src/FengZhi.Foundation/CombatUi/CombatUiMoveSelection.cs, tests/integration/combat-ui/combat_ui_dual_focus_navigation_test.cs, production/epics/combat-ui/stories/cu-008-dual-focus-and-gamepad-navigation.md, production/sprint-status.yaml
- Test written: tests/integration/combat-ui/combat_ui_dual_focus_navigation_test.cs (9 tests)
- Verification: `CombatUiDualFocusNavigationTest` passed 9/9; `CombatUiMoveSelectionPanelTest` passed 18/18; `CombatUiCounterDecisivePromptTest` passed 17/17; full `Foundation.Tests` passed 1339/1339 via SDK 8.0.421 with `DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0`
- Blockers: None
- Next: /code-review src/FengZhi.Foundation/CombatUi/CombatUiMoveSelection.cs tests/integration/combat-ui/combat_ui_dual_focus_navigation_test.cs then /story-done production/epics/combat-ui/stories/cu-008-dual-focus-and-gamepad-navigation.md

## Session Extract — /story-done 2026-06-15
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/combat-ui/stories/cu-008-dual-focus-and-gamepad-navigation.md — cu-008 双焦点与手柄导航
- Criteria: 8/8 covered by automated contract tests; Godot keyboard fallback manually verified after `BUG-0001` fix
- Test evidence: `CombatUiDualFocusNavigationTest|CombatUiMoveSelectionPanelTest|CombatUiCounterDecisivePromptTest` passed 45/45; vertical slice build passed 0 warnings / 0 errors; evidence video at `production/qa/evidence/media/cu-008-keyboard-dual-focus-navigation-rerun.mp4`
- Code review: Pending — user chose to rerun before Sprint 5 close-out after vertical-slice adapter fix
- Tech debt logged: 1 item (real physical controller D-pad / A-key and hover/focus recording pending)
- Next recommended: Sprint 5 Must Have complete; run `/code-review` for final changed files, then `/smoke-check sprint` and `/team-qa sprint`

## Session Extract — /review-all-gdds 2026-06-16
- Verdict: FAIL
- GDDs reviewed: 25 system GDDs + game-concept.md + systems-index.md
- Flagged for revision: combat-system.md, character-attributes.md, combat-ui.md, enemy-ai.md, systems-index.md, game-concept.md, epiphany-breakthrough.md, npc-state.md, misunderstanding-system.md, main-narrative.md, living-jianghu-layer.md, exploration-insight.md, romance-system.md, item-system.md
- Blocking issues: 3 — combat model drift from Burst+Read to 行气战棋; insight incorrectly affects action speed; enemy AI tell events not closed through combat/UI
- Recommended next: rewrite combat-system.md around 行气驱动的轻量武侠战棋, then sync character-attributes.md, combat-ui.md, enemy-ai.md, systems-index.md, and game-concept.md
- Report: design/gdd/gdd-cross-review-2026-06-16.md

## Session Extract — /design-system combat-system 2026-06-16
- Task: Rewriting combat-system.md from Burst+Read simultaneous resolution to 行气驱动的轻量武侠战棋
- Current section: Open Questions written; combat-system.md section rewrite complete
- File: design/gdd/combat-system.md
- Constraints: 保留 HP/内息/破绽、刚柔巧、一击决胜；移除同步结算与基础反制按钮；行气速度只使用敏捷与明确轻功词条，不使用洞察；招式自带刚/柔/巧属性；出手后行气默认归零，明确内功/轻功词条最多可保留 30%；无普通攻击兜底，0 内息且无零消耗招式时强制调息

## Session Extract — /design-sync character-attributes 2026-06-16
- Task: 同步 `character-attributes.md` 到新版行气战棋战斗口径
- File: design/gdd/character-attributes.md
- Completed: 移除洞察对行气增长与行动排序的影响；删除属性系统对当前破绽、破绽阈值和破绽衰减的归属；移除 `AddStagger(amount)` 接口；0 内息且无零消耗招式时强制调息
- Verification: `GetDiagnostics` 无报错；旧 `speed` / `AddStagger` / `base_stagger_threshold` / `stagger_decay` 有效口径已清除

## Session Extract — /review-all-gdds rerun 2026-06-16
- Verdict: PASS after inline fixes
- GDDs reviewed: 25 system GDDs + game-concept.md + systems-index.md
- Flagged for revision: None remaining
- Blocking issues: None
- Resolved since previous review: Burst+Read combat drift, insight affecting action speed, old combat action types, registry cleanup, ending-count drift, stale status metadata, and epiphany timing baseline clarification
- Recommended next: proceed to `/gate-check pre-production` or return to Production Sprint 5 smoke/QA flow
- Report: design/gdd/gdd-cross-review-2026-06-16-rerun.md

## Session Extract — /gate-check Production to Polish 2026-06-16
- Verdict: FAIL
- Target transition: Production -> Polish
- Blocking issues: Latest sprint smoke is FAIL; Sprint 5 QA sign-off missing; playtest evidence missing; Feature/Presentation scope not production-complete; cu-006/cu-007 still backlog; cu-008 manual controller/hover-focus evidence partial
- Positive evidence: `dotnet test FengZhi.slnx` passed 1344/1344; BUG-0001 is closed; GDD cross-review rerun is PASS
- Recommended next: rerun `/smoke-check sprint`; if PASS/PASS WITH WARNINGS, run `/team-qa sprint`, then collect playtest evidence before re-running this gate
- Report: production/gate-checks/gate-production-to-polish-2026-06-16.md

## Session Extract — /smoke-check sprint 2026-06-16
- Verdict: PASS WITH WARNINGS
- Automated tests: `dotnet test FengZhi.slnx` passed 1344/1344; Godot/GdUnit4 runner NOT RUN because `godot` is not on PATH
- Manual smoke: Core stability, Sprint 5 regression, data integrity, and performance batches all confirmed PASS by user
- Warnings: cu-006/cu-007 remain backlog with missing expected tests/evidence; cu-008 physical controller and hover/focus coexistence evidence remains partial
- QA hand-off: Allowed with warnings; next recommended command is `/team-qa sprint`
- Report: production/qa/smoke-2026-06-16.md

## Session Extract — /team-qa sprint 2026-06-16
- Verdict: NOT APPROVED
- Scope: Sprint 5 Must Have close-out (`cu-004`, `cu-005`, `cu-008`); `cu-006` and `cu-007` explicitly deferred
- Manual QA results: `cu-004` FAIL; `cu-005` FAIL; `cu-008` PASS
- Bugs filed: `BUG-0002` for missing cu-004 basic actions / unavailable reasons / preview card; `BUG-0003` for cu-005 using old move-type counter semantics instead of enemy current inner power qi state
- Blocking severity: `BUG-0003` is S1-Critical design-contract drift; Sprint 5 cannot close until cu-005 is rewritten against current Xingqi Tactics GDD
- Reports: production/qa/qa-signoff-sprint-5-2026-06-16.md; production/qa/test-cases-sprint-5-must-closeout-zh-2026-06-16.md

## Session Extract — Sprint 5 Combat UI harness QA 2026-06-17
- Target: `prototypes/sprint5-combat-ui-harness` replaced stale `prototypes/fengzhi-vertical-slice` for Sprint 5 Combat UI targeted QA
- Runtime: Godot 4.7-stable Mono launched cleanly; no script/runtime errors in debug output; project stopped after manual test
- User manual QA: `cu-005` PASS; `cu-008` PASS
- QA document updates: `production/qa/test-cases-sprint-5-must-closeout-zh-2026-06-16.md`, `production/qa/qa-signoff-sprint-5-2026-06-16.md`, `production/qa/evidence/cu-005-counter-and-decisive-action-prompts-evidence.md`, `production/qa/evidence/cu-008-dual-focus-and-gamepad-navigation-evidence.md`
- Bug updates: `BUG-0003` marked superseded by stale target; `BUG-0002` reframed around current `cu-004` registry drift and harness re-test
- Remaining blocker: `cu-004` still needs Foundation contract cleanup because current Combat UI output includes `BasicAttack/basic_attack`, while latest `combat_action_types` excludes ordinary attack
- Next recommended: remove or replace `BasicAttack/basic_attack` in the current Combat UI Foundation contract, then re-run `dotnet test FengZhi.slnx` and targeted `cu-004` harness QA

## Session Extract — cu-004 registry drift fix 2026-06-17
- Fix: removed `BasicAttack/basic_attack` from the current Combat UI Foundation move selection output
- Files updated: `src/FengZhi.Foundation/CombatUi/CombatUiMoveSelection.cs`, `tests/integration/combat-ui/combat_ui_move_selection_panel_test.cs`, `tests/integration/combat-ui/combat_ui_dual_focus_navigation_test.cs`, `prototypes/sprint5-combat-ui-harness/scripts/ui/Sprint5CombatUiHarnessView.cs`, `production/epics/combat-ui/stories/cu-004-move-selection-panel-and-preview-card.md`, QA sign-off / bug docs
- Harness update: `cu-005` expected contract now treats the compatibility field as enemy current qi for this harness, avoiding stale `CONTRACT DRIFT` output after user-confirmed pass
- Verification: harness build passed 0 warnings / 0 errors; targeted Combat UI tests passed 49/49; full `dotnet test FengZhi.slnx` passed 1344/1344
- Remaining blocker: targeted manual `cu-004` QA still needs to be re-run in `prototypes/sprint5-combat-ui-harness` before Sprint 5 Must Have sign-off can be approved

## Session Extract — Sprint 5 Must QA approved 2026-06-17
- User manual QA: `cu-004` PASS in `prototypes/sprint5-combat-ui-harness`
- Final Must scope results: `cu-004` PASS, `cu-005` PASS, `cu-008` PASS
- QA sign-off updated: `production/qa/qa-signoff-sprint-5-2026-06-16.md` now `APPROVED WITH CONDITIONS`
- Evidence added: `production/qa/evidence/cu-004-move-selection-panel-and-preview-card-evidence.md`
- Bugs: `BUG-0002` closed after fix + harness verification; `BUG-0003` remains superseded stale-target evidence
- Remaining conditions: `godot` CLI / GdUnit4 smoke warning remains; `cu-006` and `cu-007` remain explicitly deferred backlog scope
- Next recommended: rerun Production -> Polish gate check with Sprint 5 QA blocker cleared

## Session Extract — /gate-check Production to Polish rerun 2026-06-17
- Verdict: FAIL
- Report: `production/gate-checks/gate-production-to-polish-2026-06-17.md`
- Cleared since previous gate: Sprint 5 Must QA blocker is resolved; `cu-004`, `cu-005`, `cu-008` pass harness QA; `BUG-0002` closed; full `dotnet test FengZhi.slnx` passes 1344/1344
- Remaining blockers: no `production/playtests/` evidence; fun hypothesis not validated; Feature layer remains 6/6 Ready and Presentation layer 4/4 Ready in `production/epics/index.md`; Art Bible and key UX specs remain Draft; accessibility/platform evidence missing
- Stage remains: `Production`
- Minimal path to PASS: document 3 playtests, make a milestone scope decision for Feature/Presentation and `cu-006`/`cu-007`, run Art/UX review, add accessibility/controller/performance evidence

## Session Extract — Polish gate playtest templates 2026-06-17
- Created playtest plan: `production/playtests/playtest-plan-polish-gate-2026-06-17.md`
- Created session templates:
  - `production/playtests/playtest-2026-06-17-new-player-experience.md`
  - `production/playtests/playtest-2026-06-17-mid-game-systems.md`
  - `production/playtests/playtest-2026-06-17-difficulty-curve.md`
- Status: templates only, not executed; these do not yet satisfy the Production -> Polish gate evidence requirement
- Next recommended: run the three playtests, fill the observations/results/action-routing sections, then rerun `/gate-check polish`

## Session Extract — retired old vertical slice 2026-06-17
- Deleted `prototypes/fengzhi-vertical-slice` at user request because it used the obsolete Burst+Read combat model and was misleading current QA/playtest work
- Updated `prototypes/index.md` to mark the slice as retired/deleted historical evidence
- Cancelled `production/playtests/playtest-2026-06-17-new-player-experience.md` because it had started against the deleted stale target
- Current runnable targets left: `prototypes/sprint5-combat-ui-harness` for Sprint 5 Combat UI targeted QA and `prototypes/burst-read-combat-concept/engine` as old concept spike only
- Next recommended: create a new current full-loop playtest target before running New Player Experience playtest evidence for the Polish gate

## Session Extract — Combat Decision Loop harness visual check 2026-06-18
- Target: `prototypes/sprint5-combat-ui-harness`
- Scope: harness visual state check for fixture selection and action submenu selection/focus/hover markers
- Result: PASS by user confirmation after screenshot review
- Evidence: `production/qa/evidence/sprint5-harness-highlight-and-selection-evidence.md`
- Screenshots:
  - `production/qa/evidence/media/sprint5-harness-highlight-check-2026-06-18-godot.png`
  - `production/qa/evidence/media/sprint5-harness-left-menu-highlight-check-2026-06-18-godot.png`
- Confirmed: left fixture menu updates selected row with blue highlight and `▶`; action submenu displays selected/focus/hover states; Godot debug output had no errors
- Limitation: this is harness UI evidence only, not a full Production -> Polish playtest report
- Next recommended: run targeted `DecisionLoopBalanced`, `DecisionLoopResourcePressure`, and `DecisionLoopDecisiveWindow` playtest observations, then record them under `production/playtests/`

## Session Extract — Combat Decision Loop harness targeted playtest 2026-06-18
- Target: `prototypes/sprint5-combat-ui-harness`
- Scope: targeted playtest of three harness fixtures: `DecisionLoopBalanced`, `DecisionLoopResourcePressure`, `DecisionLoopDecisiveWindow`
- Result: PASS by user manual play; harness decision loop is readable and exposes contract drift surface as designed
- Evidence: `production/playtests/playtest-2026-06-18-combat-decision-loop-harness.md`
- Decision: skip Combat-side unit tests for now; lock current Foundation contract via existing Combat UI tests instead of adding new ones
- Polish gate impact: this counts only as harness-scope playtest evidence; Production -> Polish gate still needs a real New Player Experience playtest, a Mid-Game Systems playtest, and a Difficulty Curve playtest plus Art Bible / UX review sign-off and accessibility/controller/performance evidence
- Next recommended: pick one of (a) make a milestone scope decision for Feature/Presentation and `cu-006`/`cu-007`, (b) build a current full-loop playtest target to replace the deleted slice, or (c) advance Art Bible / UX review to clear the documentation half of the Polish gate

## Session Extract — /dev-story cu-007 2026-06-18
- Story: production/epics/combat-ui/stories/cu-007-synergy-and-round-warning-feedback.md — 协同与回合警戒反馈
- Files changed:
  - src/FengZhi.Foundation/Combat/BattleEventBus.cs (added SynergyDeclaredEvent DTO)
  - src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs (added CombatUiTurnWarningKind + CombatUiSynergyCueEntry + CombatUiTurnWarningDisplayEntry; extended CombatUiSnapshot with SynergyCueEntries + TurnWarning; added round threshold + synergy duration tuning)
  - src/FengZhi.Foundation/CombatUi/CombatUiEventAdapter.cs (subscribed to SynergyDeclaredEvent; added HandleSynergyDeclared; added UpdateTurnWarning called from RoundStart/RoundEnd; cleared synergy buffer on RoundStart)
- Tests added: tests/integration/combat-ui/combat_ui_synergy_round_warning_test.cs (7 facts, all green)
- Evidence template: production/qa/evidence/cu-007-synergy-and-round-warning-feedback-evidence.md (awaiting Visual/Feel capture)
- Suite status: 87/87 CombatUi tests pass, full solution build clean
- Out-of-scope deviation: added SynergyDeclaredEvent into Combat/BattleEventBus.cs as the boundary input contract — necessary so UI does not self-judge synergy; flagged here for /story-done review
- Out-of-scope deviation #2 (2026-06-18): extended sprint5-combat-ui-harness for evidence capture — added prototypes/sprint5-combat-ui-harness/scripts/testdata/Sprint5CombatUiAdapterFixtures.cs (4 cu-007 fixtures: synergy_gold / no_synergy_baseline / round_12_caution / round_14_critical) and a "cu-007 Round Warning / Synergy Cue" panel in Sprint5CombatUiHarnessView.cs that owns its own BattleEventBus + CombatUiEventAdapter; harness move-selection pipeline untouched; flagged here for /story-done review
- Code review: APPROVED (verdict 2026-06-18) — 4 changed files reviewed; 3 INFO suggestions (string→enum tokens, synergy dedup, evidence pending); no required changes
- Next: capture 4 evidence screenshots from the new harness panel (synergy_gold / no_synergy_baseline / round_12_caution / round_14_critical) into production/qa/evidence/media/cu-007-*.png, fill in the evidence MD, then /story-done

## Session Extract — /story-done cu-007 2026-06-18
- Verdict: COMPLETE WITH NOTES (lean review mode)
- Story: production/epics/combat-ui/stories/cu-007-synergy-and-round-warning-feedback.md — 协同与回合警戒反馈 (Status → Complete)
- AC: 6/6 通过 — 全部由 tests/integration/combat-ui/combat_ui_synergy_round_warning_test.cs 7 个 fact 覆盖；4 张 harness 截图就位
- Evidence: production/qa/evidence/cu-007-synergy-and-round-warning-feedback-evidence.md + media/cu-007-{synergy-gold-double-fist,no-synergy-baseline,round-12-caution-orange,round-14-critical-red}.png
- Code review: APPROVED (lean mode, 已先跑 /code-review)
- Tech debt logged: 5 items (docs/tech-debt-register.md — 2 项 OUT OF SCOPE deviation + 3 项 INFO suggestion: string→enum tokens、synergy dedup、evidence sign-off 表)
- Sprint status: production/sprint-status.yaml cu-007 → done (2026-06-18)
- Next recommended: cu-006 一击决胜演出编排（should-have，blocker=cu-005 已 done，可解锁）；或 sprint-status 检查 must-have 是否全部完成以触发 sprint close-out
- Suggested commit: git add src/FengZhi.Foundation/Combat/BattleEventBus.cs src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs src/FengZhi.Foundation/CombatUi/CombatUiEventAdapter.cs tests/integration/combat-ui/combat_ui_synergy_round_warning_test.cs prototypes/sprint5-combat-ui-harness/scripts/testdata/Sprint5CombatUiAdapterFixtures.cs prototypes/sprint5-combat-ui-harness/scripts/ui/Sprint5CombatUiHarnessView.cs production/qa/evidence/cu-007-synergy-and-round-warning-feedback-evidence.md production/qa/evidence/media/cu-007-*.png production/epics/combat-ui/stories/cu-007-synergy-and-round-warning-feedback.md docs/tech-debt-register.md production/sprint-status.yaml production/session-state/active.md && git commit -m "feat(combat-ui): cu-007 synergy and round warning feedback"

## Session Extract — /dev-story cu-006 2026-06-18
- Story: production/epics/combat-ui/stories/cu-006-decisive-strike-animation-director.md — 一击决胜演出编排
- Plan: .trae/documents/cu-006-decisive-strike-animation-director-plan.md (approved)
- Files added (Foundation):
  - src/FengZhi.Foundation/CombatUi/DecisiveStrikeDirector/DecisiveTuning.cs (调参常量集中地：TimeScale 0.2/1.0、慢进/慢出/推镜/动画/浮字/恢复时长、3 档优先级、镜头缩放、PauseDetectionThreshold)
  - src/FengZhi.Foundation/CombatUi/DecisiveStrikeDirector/CombatCinematicLock.cs (输入锁数值契约 + 白名单 {ui_pause, ui_system_back} + LIFO IDisposable handle)
  - src/FengZhi.Foundation/CombatUi/DecisiveStrikeDirector/TimeScaleController.cs (优先级栈 + ScaleChanged event + IDisposable handle)
  - src/FengZhi.Foundation/CombatUi/DecisiveStrikeDirector/CameraRequestBus.cs (优先级栈 + ActiveRequestChanged event + IDisposable handle + zoom / disable smoothing)
  - src/FengZhi.Foundation/CombatUi/DecisiveStrikeDirector/AnimationCommand.cs (IAnimationCommand + AnimationCommandContext + DecisiveStrikeRequest)
  - src/FengZhi.Foundation/CombatUi/DecisiveStrikeDirector/DecisiveStrikeSequence.cs (7 阶段状态机 + 反向 LIFO 释放 + 暂停互斥)
  - src/FengZhi.Foundation/CombatUi/DecisiveStrikeDirector/CombatAnimationDirector.cs (串行命令队列 + RequestDecisiveStrike + Tick + Cancel + Dispose)
- Files modified (Foundation):
  - src/FengZhi.Foundation/Combat/BattleEventBus.cs (added DecisiveStrikeStartedEvent / DecisiveStrikePhaseAdvancedEvent / DecisiveStrikeCompletedEvent — 3 个 record struct，作为音效与体系动画层的挂载点)
  - src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs (added DecisiveStrikePhase 枚举 9 值)
- Tests added: tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs (8 facts, all green) — 覆盖 AC-1..AC-7 + 通用回滚
- Files modified (Harness):
  - prototypes/sprint5-combat-ui-harness/scripts/testdata/Sprint5CombatUiDecisiveFixtures.cs (4 fixtures: decisive_gang / decisive_rou / decisive_qiao / decisive_pause_conflict)
  - prototypes/sprint5-combat-ui-harness/scripts/ui/Sprint5CombatUiHarnessView.cs (cu-006 panel: 4 fixture 按钮 + Tick 0.1/0.3/1.0s 步进 + Hold/Release ui_pause + Reset + 数值面板 + 事件日志 + _ExitTree 反向 dispose)
- Evidence template: production/qa/evidence/cu-006-decisive-strike-animation-director-evidence.md (Foundation Captured；实机 TimeScale/Tween/Camera2D 录屏 deferred)
- Suite status: 95/95 CombatUi tests pass (87 + 8); dotnet build FengZhi.slnx 0 错误（4 旧 warning 与 cu-006 无关）；dotnet build prototypes/sprint5-combat-ui-harness/Sprint5CombatUiHarness.csproj 0 错误 0 警告
- Out-of-scope deviation #1 (2026-06-18): added 3 decisive lifecycle events (Started / PhaseAdvanced / Completed) into Combat/BattleEventBus.cs as the audio + style-animation mounting contract — necessary so director does not expose a handle to other systems; 沿用 cu-007 SynergyDeclaredEvent 模式；flagged here for /story-done review
- Out-of-scope deviation #2 (2026-06-18): extended sprint5-combat-ui-harness for evidence capture — added Sprint5CombatUiDecisiveFixtures (4 fixtures) + cu-006 Decisive Strike Director panel that owns its own BattleEventBus + 4 controllers + director (与 cu-007 panel 同模式，move-selection pipeline 不动)；flagged here for /story-done review
- Deferred (next sprint): Godot Node 实机集成（修改 Engine.TimeScale 的 Tween 必须 SetProcessMode(TweenProcessMode.Always) 防自锁；Camera2D 订阅 CameraRequestBus.ActiveRequestChanged 投射 zoom/smoothing；InputMap 拦截层调用 CombatCinematicLock.IsAllowedDuringLock）；ICombatService 决胜入口 Facade；体系专属动画美术资源
- Next: /code-review src/FengZhi.Foundation/CombatUi/DecisiveStrikeDirector/*.cs src/FengZhi.Foundation/Combat/BattleEventBus.cs src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs；then 按 cu-006 fixture 截图 4 张 harness 证据并补到 production/qa/evidence/media/cu-006-*.png；then /story-done production/epics/combat-ui/stories/cu-006-decisive-strike-animation-director.md

## Session Extract — /story-done cu-006 2026-06-18
- Verdict: COMPLETE WITH NOTES (lean review mode)
- Story: production/epics/combat-ui/stories/cu-006-decisive-strike-animation-director.md — 一击决胜演出编排 (Status → Complete)
- AC: 7/7 通过 — 全部由 tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs 8 个 fact 覆盖（Director_RequestDecisiveStrike_QueuesAllSevenPhasesInOrder / TimeScaleController_DecisiveRequest_UsesPriority50AndReachesPoint2 / TimeScaleController_PauseStackPreemptsDecisiveAndRestoresOnRelease / DecisiveSequence_PauseDuringSlowMotion_ContinuesFromInterruptedPhase / CameraRequestBus_DecisiveRequest_LocksTargetAndDisablesSmoothing / CinematicLock_AcquireDuringDecisive_BlocksCombatActionWhitelistsPause / DecisiveDamageNumber_AtPhase5_PublishesPhaseAdvancedSoStyleDecisiveCanRender / Director_DisposeReleasesAllHandlesEvenIfSequenceUncompleted）
- Evidence: production/qa/evidence/cu-006-decisive-strike-animation-director-evidence.md (Foundation Captured 2026-06-18 — 自动化 8/8 + harness panel 就位；Godot 4.7-stable Engine.TimeScale + Tween + Camera2D 实机录屏 deferred 到下一 sprint，与 ICombatService Facade 一同落地)
- Code review: APPROVED with NOTES (lean mode, 已先跑 /code-review 2026-06-18) — 3 NOTE：N1 防御性死代码、N2 OnExternalScaleChanged Idle 短路冗余、N3 RequestDecisiveStrike 并发 throw + CancelCurrent 路径未独立断言；不阻塞 done
- Tech debt logged: 6 items (docs/tech-debt-register.md — 2 项 OUT OF SCOPE deviation + 1 项 Deferred 实机集成 + 3 项 code review NOTE)
- Sprint status: production/sprint-status.yaml cu-006 → done (2026-06-18)；Sprint 5 should-have 全部完成（cu-006/cu-007 done），must-have 早已 done
- Next recommended: 跑 /sprint-status 看 sprint close-out 是否就绪；或 /gate-check 看是否可以推进到下一阶段；体系专属动画美术 + Godot 实机集成留到下一 sprint
- Suggested commit: git add src/FengZhi.Foundation/CombatUi/DecisiveStrikeDirector src/FengZhi.Foundation/Combat/BattleEventBus.cs src/FengZhi.Foundation/CombatUi/CombatUiDefinitions.cs tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs prototypes/sprint5-combat-ui-harness/scripts/testdata/Sprint5CombatUiDecisiveFixtures.cs prototypes/sprint5-combat-ui-harness/scripts/ui/Sprint5CombatUiHarnessView.cs production/qa/evidence/cu-006-decisive-strike-animation-director-evidence.md production/epics/combat-ui/stories/cu-006-decisive-strike-animation-director.md docs/tech-debt-register.md production/sprint-status.yaml production/session-state/active.md && git commit -m "feat(combat-ui): cu-006 decisive strike animation director (Foundation)"

<!-- QA-PLAN: 2026-06-18 | System: sprint-6 | Plan written: production/qa/qa-plan-sprint-6-2026-06-18.md -->

## Session Extract — /architecture-review 2026-06-22
- Verdict: PASS WITH MINOR CONCERNS
- Mode: full (delta review w.r.t. 2026-06-08 baseline)
- Requirements: 25 systems total — 15 covered, 10 partial, 0 gaps (unchanged from baseline)
- ADR changes: ADR-0019 → Superseded by ADR-0020 (Pure 2D Wuxia Rendering Direction, 《大侠立志传》方向)
- Cross-ADR conflicts: 1 found and fixed during review (ADR-0020 vs ADR-0010 TileMapLayer 层级 schema — additive Background 节点 + 沿用 ADR-0010 五层)
- New TR-IDs registered: None (ADR-0020 不引入新 GDD 需求)
- GDD revision flags: map-scene-management.md (Flag 1 fixed in-place: WorldEnvironment → CanvasModulate; Flag 2 pre-existing low-priority 216 估算偏差仍待 design-review)
- Files updated: docs/architecture/adr-0019-2d-wuxia-tactics-rendering-direction.md, docs/architecture/adr-0020-pure-2d-wuxia-rendering-direction.md, docs/architecture/architecture-review-2026-06-22.md (新), docs/architecture/traceability-index.md, docs/consistency-failures.md, design/art/art-bible.md, design/gdd/game-concept.md, design/gdd/item-system.md, design/gdd/map-scene-management.md, design/gdd/systems-index.md (#12 → Needs Revision), production/epics/combat-system/EPIC.md
- Top ADR gaps: None
- Report: docs/architecture/architecture-review-2026-06-22.md

<!-- SMOKE RUN: 2026-06-22 | Sprint: Sprint 5 全作用域 close-out | Verdict: PASS | Report: production/qa/smoke-2026-06-22-sprint-5.md -->

<!-- SMOKE RUN: 2026-06-22 | Sprint: Sprint 6 close-out | Verdict: PASS WITH WARNINGS | Report: production/qa/smoke-2026-06-22-sprint-6.md -->

<!-- QA RUN: 2026-06-22 | Sprint: Sprint 6 close-out | Verdict: APPROVED WITH CONDITIONS | Report: production/qa/qa-signoff-sprint-6-2026-06-22.md -->

<!-- QA RUN: 2026-06-22 | Sprint: Sprint 5 全作用域 close-out | Verdict: APPROVED WITH CONDITIONS | Report: production/qa/qa-signoff-sprint-5-2026-06-22.md -->

## Session Extract — S6-Effort-Tracking 2026-06-22
Sprint 6 retroactive effort recap（接 Sprint 5 retro Action #5；后续每个 /story-done 必须落 Effort 段）：

| Task ID | Estimate (h) | Actual (h) | Variance | Notes |
|---------|-------------|------------|----------|-------|
| S6-Commit-Workspace      | 2.0 | 1.5 | -25% | — |
| S6-Sprint5-DoD           | 4.0 | 3.0 | -25% | — |
| cu-006-godot-integration | 6.0 | 7.0 | +17% | Godot 实机 Tween/InputMap 调试超出 buffer |
| S6-Commit-Lint           | 2.0 | 2.5 | +25% | — |
| S6-Combat-UI-Epic-Close  | 4.0 | 0.5 | -88% | 纯文档 close-out；预估明显高估 |
| EI-Debt-Triage           | 4.0 | 0.6 | -85% | 纯文档分级；预估按 spike 估，实际只做 desk review |
| S6-Effort-Tracking       | 2.0 | 0.5 | -75% | 模板补丁 + 回填，无代码改动 |

观察：
- 文档类 task 估算长期高估 60%+，下个 sprint 起 doc-only task 默认 ≤0.25d / 2h。
- 唯一正方差是 Godot 实机集成 (cu-006-godot-integration)，与 Sprint 5 retro Action #5 中"engine integration 仍是估算最大不确定来源"一致。
- 待回填：cu-visual-evidence / S6-Next-Presentation-Cut / cu-008-Gamepad-HW-Verify（done 时再补 actual_hours）。

## Session Extract — cu-visual-evidence Carryover Decision 2026-06-22

- **决策**：cu-visual-evidence (must-have, ready-for-dev) **carryover 到 Sprint 7**，不在 Sprint 6 完成。
- **原因**：
  1. `prototypes/sprint5-combat-ui-harness/` 已在 commit `c8d8c14` 主动删除（与 burst-read-combat-concept spike / 134 个旧 protagonist 资产同清理）；原计划的 4 个 fixture (`decisive_gang` / `decisive_rou` / `decisive_qiao` / `decisive_pause_conflict`) 录屏路径已断。
  2. Sprint 7 第一优先级 = ADR-0020 全循环 Vertical Slice 重建（见 `production/gate-checks/gate-tech-setup-to-pre-production-2026-06-22.md` §C-VS / §C-PLAYTEST）。新 VS 会同步产出代表当前架构（纯 2D 武侠 + 行气战棋）的 visual baseline。
  3. 在已删除的 spike 上录屏 = 在过时架构上拍 evidence；与 Sprint 7 VS 合并出新 evidence = 一次到位、不重复劳动。
- **影响文件**：
  - `production/sprint-status.yaml` — cu-visual-evidence status: ready-for-dev → backlog；加 `carryover_to_sprint: 7`；blocker 字段填决策理由
  - `production/sprints/sprint-6.md` — Must Have 表 cu-visual-evidence 行加 `[Carryover → Sprint 7]` 标记；新增 §Carryover to Next Sprint；DoD `combat-ui EPIC 8/8` checkbox 标 done with note
  - `production/qa/evidence/cu-{004,005,006,008}-*.md` — 4 份 Visual Captured 段标题 `(Sprint 6 — Pending)` → `(Sprint 7 carryover — Pending)`，并加改派说明
- **Sprint 6 关账影响**：Must Have 4/5 完成（其余 4 项 done + cu-visual-evidence carryover），Should Have 3/3 done。可继续走 `/smoke-check sprint 6` → `/team-qa sprint 6` → `/retrospective sprint 6` 闭环。
- **Sprint 7 启动锚点**：retrospective 输出 + 本决策 → Sprint 7 plan 必须包含「ADR-0020 全循环 VS 重建 + cu-visual-evidence 4 份 evidence 在新 VS 上录制」。

## Session Extract — S7 Day 1 双 story 闭环 2026-06-22

- **完成 story**：
  - `S7-VS-Foundation-Scene` (must-have, 2.0d est → 2.5h actual, **-84% variance**)
  - `S7-Day1-Smoke-Startup` (must-have, 0.25d est → 0.5h actual, **-75% variance**)
- **commits**：
  - `5c07433` feat(vs): 江南骨架 (explore/battle/outcome) + autoload
  - `0c2d2f5` chore(qa): S7-Day1-Smoke evidence CLI Pre-flight baseline
- **交付**：
  - `feng-zhi/scenes/vs/{jiangnan_riverside,battle_jiangnan_bandit,battle_outcome_jiangnan}.tscn` (placeholder, ColorRect + Button)
  - `feng-zhi/scripts/vs/Jiangnan{FlowController,RiversideGame,BattleGame,OutcomeGame}.cs`
  - `feng-zhi/project.godot` 注册 `JiangnanFlow` autoload (`*res://scripts/vs/JiangnanFlowController.cs`)
  - `production/qa/evidence/s7-day1-smoke-2026-06-23.md` (Pre-flight + Sign-off PASS)
- **验证链**：
  1. `dotnet build feng-zhi`：0 warn / 0 err
  2. `godot --headless --import`：唯一 ERROR 来自 `StartCave.tscn` `AnimatedSprite2D` 空 default animation (cosmetic, pre-existing)
  3. `godot --headless --quit-after 60 <each VS scene>`：autoload Boot + `_Ready()` 全部触发
  4. **Owner 实机**：StartCave Play + VS loop (explore→battle→outcome→explore) 全 PASS；心境分支文案切换正常
- **关键决策**：
  - 跳过 Area2D + CavePlayer 移动链路（CavePlayer 依赖 8dir AnimatedSprite2D + SpriteFrames，骨架阶段不值得复刻）
  - 战斗 scene 仅暴露胜负 hook；BattleFacade 集成留给 S7-VS-Combat-Loop（1.5d）
  - blurred-ui 用半透明 Panel 代替（与 spike `partial: blurred-ui 1 panel` 一致）
  - Pre-flight CLI 校验 + owner sign-off 替代 media 录屏（gate W2 接受 owner sign-off）
- **gate / retro 影响**：
  - gate W2 (Sprint 7 Day 1 manual startup baseline) **闭环**
  - Sprint 6 retro Action #2 (Day 1 manual smoke baseline) **闭环**
  - Sprint 6 retro Action #3 (estimate calibration, doc-only ≤2h) 反向暴露：**placeholder scene story 也严重高估 (16h est → 2.5h actual, -84%)**，下次 placeholder/skeleton story 估时应 ≤0.5d
- **下一步**：
  - 推进 `S7-VS-Combat-Loop` (1.5d, must-have)：在 `battle_jiangnan_bandit.tscn` 里接 `BattleFacade` + `CombatUiHud` + `MoveSelectionPanel` + `DecisiveStrikeDirector`，包装为"观气→出招→破绽→决胜"语言层
  - 完成后串联 `S7-VS-Outcome-Feedback` (1.25d) 与 `cu-visual-evidence` (0.75d carryover)
- **Sprint 7 progress**：3/8 stories done (Spike + Foundation + Smoke)；剩余 must-have 3 项 (Combat-Loop / Outcome-Feedback / cu-visual-evidence)；should-have 1 项 (Playtest)；nice-to-have 1 项 (Gamepad-HW)。可用 capacity 余 ≈ 7.5d / 总 10d。

## Session Extract — S7-VS-Combat-Loop MVP-A PASS 2026-06-23

- **完成 story**：`S7-VS-Combat-Loop` (must-have, 1.5d est → 4.0h actual, **-67% variance**, MVP-A 紧缩范围)
- **Owner 实机 sign-off**：2026-06-23 09:06 「PASS」
- **commits**：
  - `7aa8786` plan(s7): MVP-A dev-story spec
  - `6f768a8` feat(vs-combat): Subtask 0b+1 fixture + smoke test
  - `204ec03` feat(vs-combat): Subtask 2 VsBattleLoopController
  - `bc84362` feat(vs-combat): Subtask 3+4+5 Godot scene 接入
- **交付**：
  - `docs/superpowers/specs/2026-06-23-s7-vs-combat-loop-mvp-a.md` — dev-story spec
  - `src/FengZhi.Foundation/Combat/Fixtures/JiangnanBandit1v1Fixture.cs` — 1v1 fixture (luo_han_quan / tie_bi_heng_lan，HP/Gang 调校至 2-5 回合分胜负)
  - `src/FengZhi.Foundation/Combat/Runtime/VsBattleLoopController.cs` — 事件驱动战斗循环编排器
  - `tests/integration/vs/{jiangnan_bandit_fixture_smoke_test.cs,vs_battle_loop_controller_test.cs}` — 双层 8 个测试 case
  - `feng-zhi/scripts/vs/Jiangnan{BattleGame,FlowController,OutcomeGame}.cs` — Godot 集成 + 转场携带 BattleResult
  - `feng-zhi/scenes/vs/battle_jiangnan_bandit.tscn` — 占位 UI (HP/Neixi label + Light/Heavy button)
- **验证链**：
  1. Foundation `dotnet test`：1386/1386 PASS (新增 8 个 case)
  2. `dotnet build feng-zhi`：0 warn / 0 err
  3. `godot --headless --quit-after 60 battle_jiangnan_bandit.tscn`：scene 加载、`_Ready` 触发、autoload Boot 全 clean
  4. **Owner 实机 Subtask 8**：完整跑通 explore → 触发战斗 → 轻击/重击点击 → 2-3 回合分胜负 → BattleEndEvent → GoToOutcome(result) → 胜/负文案切换 → 返回 explore (动态状态更新)
- **关键决策**：
  - Subtask 0a 暴露 spec 用了不存在的 move ID (`quick_strike` / `heavy_strike`)，改用 registry 中实际存在的 `luo_han_quan` / `tie_bi_heng_lan`（落汉拳 / 铁臂横拦）
  - Subtask 1 fixture 测试暴露 `ResolutionService.ExecuteMove` 硬编码 `BaseMultiplier = 1.0f`（移动数据被忽略，tech-debt 已记入 Sprint 8）；通过调校 fixture HP/Gang (player 25/80, bandit 20/60) 让战斗在 2-5 回合分胜负，标注 VS-only，不污染主线平衡
  - Subtask 6 PhaseBanner 砍除（StatusLabel 已覆盖 UX 需求）
  - CombatMoveSelectionPanel + CombatHudPanel 集成推到 cu-visual-evidence 阶段（cu-004 录制时一并做），MVP-A 用 ColorRect+Button 占位
  - VsBattleLoopController 单方法 publish 复合事件 (RoundStart + IntentReveal)，避免 UI 双 tick 闪烁
- **估时偏差归因**：
  - **-67% (12h → 4h)** 主因：dependency-survey 暴露的 R2「编排层 0%」实际只花 1h 化解 (`VsBattleLoopController` ~120 lines)，远低于 spike 估计的 4-6h
  - 复用率 ~50%（vs spike §2.2 假设 75%）—— BattleFacade / BattleEventBus 可直用，但 ResolutionService 的 BaseMultiplier 黑盒 + CombatUiEventAdapter 在 MVP-A 未启用降低实际复用率
- **Iso follow-up（与 ADR-0022 pivot 协同）**：
  - 当前 `battle_jiangnan_bandit.tscn` = ColorRect 背景 + 占位 UI，**iso pivot 决策 (commit 40d81c4) 不阻塞 MVP-A 关账**（docs-only stage 1）
  - S7-Iso-Pivot-Foundation (ready-for-dev, 1.0d) 落地后，在 cu-visual-evidence 阶段顺手按 iso 视觉重新蒙皮（替换背景为 iso TileMap + sprite，估时 < 1h，逻辑层不动）
- **gate / retro 影响**：
  - VS prove-or-pivot 决策点向 **PROVE** 迈进一步（核心循环可玩 + 0 S1/S2 bug，剩 playtest 评估）
  - 估时偏差列表加一条：**S7-VS-Combat-Loop -67%**（与 Foundation-Scene -84% 同源 —— spike survey 的悲观假设过度计入估时）
  - Sprint 6 retro Action #3 (estimate calibration) 持续暴露：integration spike 估时需引入「survey-risk 降级因子」(若 survey 已枚举具体 gap 模式，actual 通常是 estimate 的 0.3-0.5x)
- **下一步**：
  - 推进 `S7-VS-Outcome-Feedback` (1.25d, must-have)：mindset 双轴位移 + blurred 战后面板 partial
  - 或并行启动 `S7-Iso-Pivot-Foundation` (1.0d, ready-for-dev)：等 iso pivot 落地后再做 cu-visual-evidence iso 蒙皮
- **Sprint 7 progress**：4/8 stories done (Spike + Foundation + Smoke + Combat-Loop) + Animator-Port (并行 done) + ADR-0022 pivot docs；剩余 must-have 3 项 (Iso-Pivot-Foundation / Outcome-Feedback / cu-visual-evidence)；should-have 1 项 (Playtest)；nice-to-have 1 项 (Gamepad-HW)。可用 capacity 余 ≈ 3.5d / 总 10d（含 buffer）。

## Session Extract — S7-Iso-Pivot-Foundation PASS (代码层) 2026-06-23

- **完成 story**：`S7-Iso-Pivot-Foundation` (must-have, 1.0d est → 2.5h actual, **variance -69%**, 代码层 done；DoD owner 实机录屏 pending)
- **commits**：
  - `7da0ba1` feat(geometry): AC2 IsoProjection 纯静态投影工具 + 10 互逆单测
  - `0836963` feat(animation): AC4 Iso4 4 斜方向兄弟接口 + Adapter + Fake + ADR §2 erratum
  - `5a5ec05` test(animation): AC5 Iso4 14 单测覆盖选区/迟滞/WASD/Fake + 修订 sector 几何
  - `3ae8116` refactor(animation): AC3+AC7 删 8dir 代码 + CavePlayer 迁移到 IIso4
  - `5e0bcdf` feat(asset): AC6 main_character.tres 重建为 iso4 + 8dir 资产归档
- **AC 进度**：
  - AC1 (ADR + traceability): 已由 commit 40d81c4 完成
  - AC2 (IsoProjection + 10 tests): 7da0ba1 ✅
  - AC3 (删 8dir + 11 tests): 3ae8116 ✅
  - AC4 (Iso4 interface/adapter/fake): 0836963 ✅
  - AC5 (14 Iso4 tests): 5a5ec05 ✅
  - AC6 (main_character.tres + assets archive): 5e0bcdf ✅
  - AC7 (CavePlayer 迁移): 3ae8116 ✅ (与 AC3 原子提交)
  - AC8 (StartCave TileMapLayer iso): **N/A** — feng-zhi/ 全项目无 TileMapLayer
  - AC9 (combat-system/adr-0010/art-bible 同步): 已由 40d81c4 完成
  - AC10 (sprint-status/traceability/story doc): 待 closeout commit
- **关键技术决策**：
  - ADR-0022 §2 sector 区间表 erratum 落盘：原稿左闭右开 `[a, b)` 让 W key (atan2=-3π/4) 落入 NE 区间冲突 §3 (W → NW)。修订为右闭左开 `(a, b]`，sector 中心从 WASD 对角移到 cart 卡式轴 (NE=-π/2, SE=0, SW=π/2, NW=π)。
  - 测试驱动发现并修复 sector 几何 bug：AC4 初版用错误的 boundary (cart 卡式半轴 0/±π/2)，cart (-1, 0) atan2=π 错误归到 SW 而非屏幕 NW；AC5 测试 fail 后回炉，正确实现是 boundary 在 cart 对角 (±π/4, ±3π/4)，中心在 cart 卡式轴。
  - CavePlayer 输入映射：WASD 直接按 ADR §3 解释成 cart 对角向量（不用 Input.GetVector 因为它返回 screen-space）；屏幕速度通过 IsoProjection 后归一化 × Speed 保持恒定（避免 N/S 比 E/W 慢一倍的视觉异感）。
  - AC8 N/A 决策：feng-zhi/ 全项目 grep 无 TileMapLayer，StartCave 用 Sprite2D PNG 背景，VS 场景全是 ColorRect。AC8 是 story 写作时的乐观假设，真正 iso TileMap 实现导入点是未来 VS 重建 story。
  - 资产 rename + split 策略：folder rename `main_character_16bit_8dir/` → `main_character_iso4/` + 4 卡式 (E/N/S/W) 32 文件 git mv 到 `_archive_8dir_2026-06-22/`；perl -i -pe 批量更新 .import source_file 路径；archive 加 `.gdignore` 让 Godot 忽略。godot --headless --import 16 张 PNG reimport 成功 0 err。
- **估时偏差归因（-69%, 8h → 2.5h）**：
  - AC8 N/A 节省 1.0h
  - AC4 + AC5 测试驱动发现的 geometry bug 多花 ~30min 回炉
  - 净节省主因：survey 阶段对 .import / .tres / .tscn 文本编辑的悲观估计 — 实际 Godot 4 资源都是文本格式 + 路径稳定可靠，perl -i 批量改路径 + headless reimport 自愈，比预想的「手工 Godot 编辑器操作」快 10x
- **Foundation 测试基线**：1386 → 1399 (净 +13: AC2 +10 / AC4-5 +14 / AC3 -11)。spec 原目标 1378-11+8=1375，实际超出 ~24 (因 baseline 已含 Combat-Loop +8 tests，且我做了 10+14 而非 spec 期望的 8+4=12 tests)。
- **暴露/解锁/影响**：
  - **暴露 ADR 写作问题**：ADR-0022 §2/§3 之间初版有自相矛盾 — 类似 sprint 内 ADR-0021 自我演化为 0022 的快迭代下，新 ADR 内部一致性需 reviewer 验证（建议加入 architecture-review 工作流的 chain-of-verification 步骤）
  - **解锁 downstream**：S7-VS-Combat-Loop iso 蒙皮 / cu-visual-evidence / S7-VS-Outcome-Feedback 现在可推进
  - **rollback 残留**：`_archive_8dir_2026-06-22/` + `.gdignore` 形成 1-sprint window 可一次性回滚（Sprint 8 retro 决策）
- **下一步**：
  - **DoD pending**：owner 在 Godot 4.7 编辑器里打开 StartCave Play 验证 WASD 走 4 斜向、sprite 切换正确；录制 ≥1 段 30s 录屏 + ≥3 张 NE/SE/NW 截图落 `production/qa/evidence/s7-iso-pivot-foundation/`
  - Sprint 7 must-have 剩 2 项：S7-VS-Outcome-Feedback (1.25d) + cu-visual-evidence (0.75d carryover)
- **Sprint 7 progress**：5/8 stories done (Spike + Foundation + Smoke + Combat-Loop + **Iso-Pivot**) + Animator-Port + ADR-0022 docs；剩余 must-have 2 项 (Outcome-Feedback / cu-visual-evidence)；should-have 1 项 (Playtest)；nice-to-have 1 项 (Gamepad-HW)。可用 capacity 余 ≈ 3d / 总 10d（含 buffer）。VS prove-or-pivot 向 **PROVE** 大步前进。

## Session Extract — S7-VS-Outcome-Feedback PASS (代码层) 2026-06-23

- **完成 story**：`S7-VS-Outcome-Feedback` (must-have, 1.25d est → 4.5h actual, **variance -55%**, 代码层 done；DoD owner 实机录屏 pending)
- **commits**：
  - `79b9b0d` docs(sprint-7): AC1 dev-story spec 草稿 (8 AC + 8 subtask + DoD + 风险)
  - `4464998` feat(mindset): AC5 MindsetZoneLiteraryNames 9 zone + 5 tier 文学名（落 Foundation, deviation from spec §2.1）
  - `7e0200c` feat(vs-outcome): AC2 JiangnanFlowController 集成 MindsetService + EventBus + Snapshot
  - `9be8a8b` feat(vs-outcome): AC3 OutcomeGame 接入 + 文学化前后对比渲染
  - `5002aa3` feat(vs-outcome): AC4 BlurredPanel StyleBoxFlat + warmth → SelfModulate 染色
  - `86bb24a` test(vs-outcome): AC6 Foundation 化 MindsetOutcomeShifts + 4 integration tests (4/4 PASS)
  - `f527e29` docs(vs-outcome): AC7 代码层 smoke evidence + owner pending checklist
- **AC 进度**：
  - AC1 spec ✅ (79b9b0d)
  - AC2 FlowController Mindset 集成 ✅ (7e0200c)
  - AC3 OutcomeGame 接入 + 文学化渲染 ✅ (9be8a8b)
  - AC4 BlurredPanel 视觉升级 ✅ (5002aa3)
  - AC5 MindsetZoneLiteraryNames ✅ (4464998)
  - AC6 4 integration tests ✅ (86bb24a)
  - AC7 build/test/headless smoke ✅ (f527e29)
  - AC8 closeout (active.md / sprint-7 / sprint-status / spec Final Status) ⏳ 本 commit
- **关键技术决策**：
  - **Foundation 化 spec §3 mapping**：Subtask 6 写 tests 时发现 `JiangnanFlowController.ResolveShifts` 在 feng-zhi/，tests 项目只链 Foundation 拿不到。决策：把 mapping 抽到 `MindsetOutcomeShifts.cs` 落 Foundation（含 `MindsetOutcomeChoice` enum）；JiangnanFlowController.MindsetChoice 保留 Godot 友好枚举但内部委托给 Foundation。**单一权威 + tests 可直接验证 spec §3 数值表**。
  - **SelfModulate vs Modulate**：BlurredPanel 染色用 `SelfModulate` 而非 `Modulate` — Modulate 会乘子级联到所有子 Label 让文字偏色；SelfModulate 只影响 Panel 自己的 StyleBoxFlat 背景绘制，文字保持白色高对比。Godot 推荐模式。
  - **Snapshot 不依赖事件序**：`MindsetShiftSnapshot` record 一次性冻结 old/new state；UI 不订阅事件做累加，避免 single-frame 多事件触发顺序问题（spec §9 风险表已预警）
  - **Autoload 单例 vs DI 容器**：feng-zhi 还没 ServiceLocator/IoC；MindsetService + EventBus 都挂 `JiangnanFlowController` Autoload，跨 scene 存活，Sprint 8 可考虑统一 DI
  - **fallback path**：OutcomeGame 在 /root/JiangnanFlow autoload 缺失（独立 scene 调试）时降级到老 placeholder 文本 + PushWarning，保留 standalone 可调试
  - **modulate-only 朦胧化**：不上 ShaderMaterial 高斯模糊（spec §2.2 Out of Scope）。StyleBoxFlat 半透明深底 + warmth modulate 给"朦胧"基线视觉门槛 — blurred-ui partial 已满足
- **估时偏差归因（-55%, 10h → 4.5h）**：
  - Mindset epic 7/7 stories done 但**从未在真实 scene 中接入过** — 本 story 等于"首次集成 + 暴露集成成本"。结果：暴露成本几乎为 0（Foundation 完备 + Godot Autoload 友好）
  - 主要节省 (Subtask 3/4/5 联合 -3.7h)：StyleBoxFlat + SelfModulate 是 Godot 内置能力，0 实验时间
  - 单一权威重构（Subtask 6）反而**节省**：用 Foundation 化 ResolveShifts 一次性消除"feng-zhi vs tests 跨项目不对称"问题
- **Foundation 测试基线**：1399 → 1403 (+4)。spec 原目标 1399 + 4 = 1403，实际命中。
- **暴露 / 解锁 / 影响**：
  - **暴露 epic-done ≠ integration-done**：Mindset epic 7/7 done 实际是 Foundation 层闭环；本 story 是首次"在游戏场景里能看到位移"。建议 traceability-index 加 "integration evidence" 栏区分
  - **解锁 cu-visual-evidence**：现在 outcome 朦胧化 + zone 切换可拍进 evidence 录屏（cu-006 决胜动画 + outcome 心境位移连拍 = 完整 narrative beat）
  - **解锁 Sprint 8 blurred-ui epic**：留下 prior art — MindsetZoneLiteraryNames 静态表 + WarmthToModulate + StyleBoxFlat 模板
  - **解锁 Sprint 8 mindset-dual-axis 真集成**：Autoload 单例 + EventBus 接线模式可复用到存档 / 跨章节累积 / NPC 反应
- **暴露的技术债（5 项）**：
  1. BattleEnd 自动位移路径未启用（GDD §来源 B 战斗结果位移）
  2. modulate-only 朦胧化 — 最终方案是 ShaderMaterial（高斯模糊 + LUT 色调）
  3. mindset_zone 场景级染色未联动 explore scene（blurred-ui Rule 4 pending_reveals 队列）
  4. MindsetService.UpdateReputation 未在 outcome 流调用
  5. tests 内 MindsetShiftSnapshotData 与 feng-zhi 的 MindsetShiftSnapshot 不共享类型（无 feng-zhi.Domain 公共项目）
- **下一步**：
  - **DoD pending**：owner 在 Godot 4.7-mono 编辑器实机走查 (Spare / Defeat / Zone-or-Tier 切换) + ≥30s 录屏 + ≥3 张截图 存 `production/qa/evidence/s7-vs-outcome-feedback/`
  - Sprint 7 must-have 剩 1 项：`cu-visual-evidence` (0.75d carryover, 可与 outcome DoD 一并录制)
  - Sprint 7 should-have 剩 1 项：`S7-VS-Playtest-Session` (0.5d, 依赖 cu-visual-evidence)
- **Sprint 7 progress**：6/8 stories done (Spike + Foundation + Smoke + Combat-Loop + Iso-Pivot + **Outcome-Feedback**) + Animator-Port + ADR-0022 docs；剩余 must-have 1 项 (cu-visual-evidence)；should-have 1 项 (Playtest)；nice-to-have 1 项 (Gamepad-HW)。本 sprint 实际 actual hours 已花 ~14.5h / 总 10d capacity (含 buffer)，估算偏差累计 -55~-69% 让 sprint 跑赢 calendar；剩余 7+ 天给 cu-visual-evidence + Playtest + 总结 buffer 充裕。VS prove-or-pivot **PROVE 信号已强**——VS 全循环（explore → combat → mindset outcome → return）已 owner 实机 PASS（Combat-Loop 09:06 sign-off），剩余只是录制 + 玩家试玩 + 视觉打磨。

## Session Extract — /story-done story-002-bgm-crossfade 2026-06-26
- Verdict: COMPLETE WITH NOTES (lean review mode)
- Story: production/epics/audio-system/story-002-bgm-crossfade.md — BGM 管理 + 等功率 Crossfade (Status → Complete)
- AC: 6/6 通过 — 全部由 tests/unit/audio/bgm_crossfade_test.cs 29 个 fact 覆盖
- Test evidence: `BgmCrossfadeTest` 29/29 passed
- Code review: APPROVED after 2 issues fixed (IsSameTrack helper 统一同曲检测; interrupt 时 StopAndSwap 避免 outPlayer 音量跳变)
- Files changed:
  - src/FengZhi.Foundation/Audio/BgmCrossfadeEngine.cs (IsSameTrack helper)
  - feng-zhi/scripts/audio/BgmManager.cs (StopAndSwap + interrupt 保护)
  - feng-zhi/scripts/audio/AudioDirector.cs (using alias 解决命名空间冲突)
- Effort: Estimate 3-4h / Actual ~3h / Variance -14%
- Tech debt logged: None
- Next recommended: story-003-adaptive-combat-music.md — 自适应战斗音乐（6 段水平分层），Status: Ready，依赖 Story 002 已解锁

## Session Extract — /dev-story story-003-adaptive-combat-music 2026-06-26
- Story: production/epics/audio-system/story-003-adaptive-combat-music.md — 自适应战斗音乐（6 段水平分层）
- Status: In Progress → 实现完成待 review
- Files created:
  - src/FengZhi.Foundation/Audio/CombatMusicEngine.cs (Foundation 纯逻辑: CombatSegment enum + EvaluateCombatState F3 + bar boundary 调度)
  - feng-zhi/scripts/audio/CombatMusicController.cs (Presentation 层: Godot Node 逐帧驱动)
  - tests/unit/audio/combat_music_test.cs (29 个 Fact 全绿)
- Files modified:
  - docs/architecture/tr-registry.yaml (注册 TR-audio-003)
  - production/epics/audio-system/story-003-adaptive-combat-music.md (Status → In Progress)
- Test evidence: CombatMusicTest 29/29 passed
- Blockers: None
- Next: /code-review then /story-done

## Session Extract — /story-done story-003-adaptive-combat-music 2026-06-26
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/audio-system/story-003-adaptive-combat-music.md — 自适应战斗音乐（6 段水平分层）(Status → Complete)
- AC: 6/6 通过 — tests/unit/audio/combat_music_test.cs 31 facts 全绿
- Code review: APPROVED after 3 issues fixed (GetActivePlayer→ActivePlayer, PlayBgm→CrossfadeBgm, F3+counterStreak)
- Files created: CombatMusicEngine.cs, CombatMusicController.cs, combat_music_test.cs
- Files modified: BgmManager.cs (ActivePlayer+CrossfadeBgm), BgmCrossfadeEngine.cs (StartCrossfadeOnly)
- Effort: Estimate 4-5h / Actual ~3.5h / Variance -22%
- Tech debt logged: None
- Next recommended: 查看 audio-system epic 下一个 story 或 Sprint 7 剩余 must-have

## Session Extract — /dev-story story-004-ambient-layers 2026-06-26
- Story: production/epics/audio-system/story-004-ambient-layers.md — 环境音三层系统
- Status: In Progress → 实现完成待 review
- Files created:
  - src/FengZhi.Foundation/Audio/AmbientLayerEngine.cs (Foundation 纯逻辑: AmbientLayer enum + AmbientLayerState + SceneAudioConfig + 三层 fade 引擎)
  - feng-zhi/scripts/audio/AmbientManager.cs (Presentation 层: 3x AudioStreamPlayer 逐帧 fade)
  - tests/unit/audio/ambient_layers_test.cs (18 个 Fact 全绿)
- Files modified:
  - docs/architecture/tr-registry.yaml (注册 TR-audio-004)
  - production/epics/audio-system/story-004-ambient-layers.md (Status → In Progress)
- Test evidence: AmbientLayersTest 18/18 passed
- Blockers: None
- Next: /code-review then /story-done

## Session Extract — /story-done 2026-06-27
- Verdict: COMPLETE WITH NOTES
- Story: production/epics/audio-system/story-004-ambient-layers.md — 环境音三层系统
- Tech debt logged: None
- Effort: estimate 2.50 h / actual 2.75 h (variance +10%)
- Next recommended: Story 005 (SFX Priority Pool) or sprint close-out

## Session Extract — /dev-story 2026-06-27
- Story: production/epics/audio-system/story-005-sfx-priority-pool.md — SFX 优先级仲裁 + 并发池
- Status: In Progress → 实现完成待 review
- Files created:
  - src/FengZhi.Foundation/Audio/SfxPoolEngine.cs (Foundation 纯逻辑: SfxPriority enum + SfxSlot + SfxPoolEngine 仲裁引擎)
  - feng-zhi/scripts/audio/SfxManager.cs (Presentation 层: 12x AudioStreamPlayer 池 + 逐帧回收)
  - tests/unit/audio/sfx_pool_test.cs (14 个 Fact 全绿)
- Files modified:
  - docs/architecture/tr-registry.yaml (注册 TR-audio-005)
  - production/epics/audio-system/story-005-sfx-priority-pool.md (Status → In Progress)
- Test evidence: SfxPoolTest 14/14 passed
- Blockers: None
- Next: /code-review then /story-done

## Session Extract — /story-done 2026-06-27
- Verdict: COMPLETE
- Story: production/epics/audio-system/story-005-sfx-priority-pool.md — SFX 优先级仲裁 + 并发池
- Tech debt logged: None
- Effort: estimate 3.00h / actual 2.50h (variance -17%)
- Next recommended: story-006-cutscene-audio.md (演出音频接管与跳过恢复)

## Session Extract — /dev-story 2026-06-27
- Story: production/epics/audio-system/story-006-cutscene-audio.md — 演出音频接管与跳过恢复
- Status: In Progress → 实现完成待 review
- Files created:
  - src/FengZhi.Foundation/Audio/CutsceneAudioEngine.cs (Foundation 纯逻辑: 演出状态跟踪 + 战斗位置保存)
  - feng-zhi/scripts/audio/CutsceneAudioController.cs (Presentation 层: 协调 BgmManager/SfxManager 完成演出接管)
  - tests/integration/audio/cutscene_audio_test.cs (12 个 Fact 全绿)
- Files modified:
  - src/FengZhi.Foundation/Audio/SfxPoolEngine.cs (增加 bypassCooldown 参数 + P0/P1 豁免同源限制 + GetActiveSlotsBySource)
  - feng-zhi/scripts/audio/SfxManager.cs (bypassCooldown 透传 + FadeOutBySource 方法)
  - docs/architecture/tr-registry.yaml (注册 TR-audio-006)
  - production/epics/audio-system/story-006-cutscene-audio.md (Status → In Progress)
- Test evidence: CutsceneAudioTest 12/12 + SfxPoolTest 14/14 passed (136 total audio tests green)
- Blockers: None
- Next: /code-review then /story-done
