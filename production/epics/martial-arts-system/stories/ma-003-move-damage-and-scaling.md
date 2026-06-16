# Story: ma-003 — 招式伤害、完整度、境界缩放与批注倍率公式

> **Epic**: martial-arts-system
> **类型**: Logic
> **优先级**: P0 — 战斗结算依赖公式
> **Estimate**: S（约 3-4h）
> **依赖**: ma-001, ma-002
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/martial-arts-system.md §Formulas, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-martial-arts-system-003
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

实现武学系统提供给战斗系统的招式输出计算：根据基础倍率、体系攻击力、完整度、境界缩放和批注倍率，计算招式最终伤害或效果强度。

## 范围

### 包含
- `move_damage = effective_base_multiplier × attack_for_type × completion × realm_scaling`
- 境界缩放表：0.7 到 2.0
- 普通、高级、绝学、批注版的 `effective_base_multiplier`
- 批注版 `effective_base_multiplier = base_multiplier × annotation_mult`
- 返璞归真境界与批注倍率同时生效

### 不包含
- 战斗系统最终伤害减防、克制倍率、破绽叠加 → combat-system
- 特殊效果触发判定 → ma-004
- 心法被动加成公式 → ma-006

## 技术说明

- 本 story 只计算武学侧有效输出，不处理目标防御、克制结果或战斗回合状态
- 输入的 `attack_for_type` 来自角色属性系统；这里可用接口或 DTO 注入
- 所有境界缩放值应集中定义，避免散落魔法数字
- 浮点结果应按当前项目数值规范返回原始 double/float，由战斗系统决定取整时机

## 验收标准

- [x] 真传招式在“炉火纯青”境界下，伤害 = `base_multiplier × attack × 1.0 × 1.3`
- [x] 批注版普通招式原 `base_multiplier=0.7`、`annotation_mult=1.8` 时，`effective_base=1.26`
- [x] 批注版普通招式可高于同境界下未批注的高级招式
- [x] 返璞归真境界使用批注版普通招式时，`realm_scaling=2.0` 与 `annotation_mult` 同时生效
- [x] 未知境界、非法 completion、非法倍率会返回明确错误或校验失败

## QA 测试用例

- **AC-1**：真传招式炉火纯青
  - Given：base_multiplier=1.0，attack=35，completion=1.0，realm=炉火纯青
  - When：计算招式伤害
  - Then：结果为 `45.5`
  - 边界：attack=0、base_multiplier=0.6、realm=初学乍练

- **AC-2**：批注倍率
  - Given：普通招式 base_multiplier=0.7，annotation_mult=1.8
  - When：计算 effective_base_multiplier
  - Then：结果为 `1.26`
  - 边界：annotation_mult=1.0、annotation_mult=2.0

- **AC-3**：返璞归真批注最高值
  - Given：批注版普通招式、completion=1.0、realm=返璞归真
  - When：计算招式伤害
  - Then：使用 `realm_scaling=2.0` 且批注倍率同时生效
  - 边界：非批注真传同境界对照、非法境界 key

## 测试证据路径

`tests/unit/martial-arts/move_damage_and_scaling_test.cs`

## 依赖关系

- Depends on: ma-001, ma-002
- Unlocks: combat-system damage stories, ma-006

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

