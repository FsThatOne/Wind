# 教学/引导 (Tutorial & Onboarding)

> **Status**: In Design
> **Author**: user + agents
> **Last Updated**: 2026-06-08
> **Implements Pillar**: Pillar 3 (武侠味先于游戏味), Pillar 4 (情感深度优先于内容广度)

## Overview

教学/引导系统是《风止》的渐进式新手引导框架。**基础设施层面**，它通过一个状态跟踪器（Tutorial State Tracker）记录玩家已完成的教学步骤，控制教学提示的触发、抑制和重放；**玩家体验层面**，它将每个系统的首次引入包装为叙事锚点——玩家感受到的不是"教学弹窗"，而是来自身边人的自然一课：

- **序章·师门日常**：师姐教采药和采矿（游戏前期仅开放这两种采集）；与师弟的日常互动教基础操作
- **序章·师兄回山**：回山的师兄教授战斗基础（Burst+Read 核心循环），但**不教顿悟突破**——留给第二章生死危局中自然触发时的未知惊喜
- **章外章·镖局岁月**：老镖师讲江湖规矩，引入心境双轴、自然日/体力、物品/装备、活江湖层
- **第二章·独行江湖**：生死危局触发顿悟突破并顺势教学；探索/洞察机制自然解锁

系统遵循两条核心原则：
1. **叙事驱动，零 HUD 弹窗**（Pillar 3）：所有教学通过对话、演出和受控场景传递，不使用游戏化提示框、高亮箭头或强制暂停遮罩。
2. **渐进解锁，不超过第二章**（来自 systems-index 系统解锁时间线）：系统按序章→章外章→第一章→第二章分批引入，每阶段最多新增 5 个系统，避免认知过载。

如果没有这个系统，玩家将面对朦胧化 UI、隐式心境、彗星模型感情等多个"反直觉"设计而不知所措——游戏独特的"去数据化"哲学恰恰需要一个精心编排的引导层来让玩家理解"这个游戏该怎么读"。

## Player Fantasy

**"我不是在学操作，我是在跟师姐上山采药、跟师兄练剑、被老镖师讲规矩——学艺本身就是生活。"**

教学系统的情感目标是**学艺感**——武侠小说里，主角的能力不是从天上掉下来的，是师长传授、同门切磋、江湖历练中一点一点习得的。每一个"教学时刻"都应该让玩家感受到两层东西：

1. **我学会了新本领**（功能层）——现在我能采药了、能出招了、能读懂心境了
2. **传授我的那个人很重要**（情感层）——师姐教我采药时的耐心、师兄教我出剑时的严厉、老镖师讲规矩时的沧桑，这些记忆让后续的离别与重逢更有重量

**锚定时刻**：当玩家在第二章生死危局中突然触发顿悟突破——一个从未被教过的机制凭空出现——那一刻的"原来还可以这样！"既是对角色的成长惊喜，也是对教学系统克制（故意不提前透露）的回报。

**Pillar 对齐**：
- Pillar 3（武侠味先于游戏味）：武侠小说从来不说"按 X 攻击"，而是"师兄递过一把木剑"
- Pillar 4（情感深度优先于内容广度）：少数几个教学者，但每一位都有人格、有弧线——教学本身加深角色关系

## Detailed Design

### Core Rules

[To be designed]

### States and Transitions

[To be designed]

### Interactions with Other Systems

[To be designed]

## Formulas

[To be designed]

## Edge Cases

[To be designed]

## Dependencies

[To be designed]

## Tuning Knobs

[To be designed]

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Acceptance Criteria

[To be designed]

## Open Questions

[To be designed]
