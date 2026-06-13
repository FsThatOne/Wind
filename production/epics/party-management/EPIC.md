# Epic: 队伍管理 / 同伴成长

> **Layer**: Feature
> **GDD**: design/gdd/party-management.md
> **Architecture Module**: `Feature/Party/`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories party-management`

## Overview

队伍管理 / 同伴成长系统实现统一可玩角色结构、五人上阵、主角锁定、同伴状态可用性、关键战斗心得、观战成长、末尾追赶、代办成长结算和装备实例唯一绑定。系统强调成长不靠刷怪，而由叙事、关键战斗、顿悟与个人旅程驱动。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0016: Party Management | 采用 PlayableCharacter Resource + DeploymentManager + GrowthSettlementEngine + CatchupCalculator | LOW |
| ADR-0014: Living Jianghu Layer | 离队历练与代办结果由活江湖层触发，Party 只负责成长结算 | LOW |
| ADR-0004: Save Encryption | 队伍名单、上阵、成长节点、追赶 cap 需要持久化 | LOW |
| ADR-0003: Data Configuration Format | 可玩角色、成长节点和部署锁定由数据配置 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 最多 5 人上阵且主角不可移除 | ADR-0016 ✅ |
| 可同行人数不足 5 人时允许少人上阵 | ADR-0016 ✅ |
| 离队/锁定/重伤同伴不可上阵并显示叙事原因 | ADR-0016 ✅ |
| 同一装备实例不可同时装备给两人 | ADR-0016 ✅ |
| 关键战斗结算上阵心得与观战心得 | ADR-0016 ✅ |
| 低于章节基线时按 catchup_target 补足但受章节上限限制 | ADR-0016 ✅ |
| 代办结果发放个人旅程成长或后续剧情标记 | ADR-0016 + ADR-0014 ✅ |
| 同伴临阵顿悟完成后获得个人奖励并刷新其冷却 | ADR-0016 + ADR-0017 ⚠️ 需集成 story 验证 |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前没有 `TR-party-*` 条目。创建 stories 时应从 `party-management.md` 8 条 AC 建立 trace，并把 Deployment、Growth、Equipment Ownership、Epiphany Integration 拆成独立 stories。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/party-management.md` are verified
- Deployment validation, protagonist lock, state availability, catch-up, observation growth, delegate growth and equipment uniqueness have automated tests
- Growth sources cannot stack beyond chapter caps
- UI-facing status text remains literary and does not expose bench XP percentages or delegate success probability

## Next Step

Run `/create-stories party-management` to break this epic into implementable stories.
