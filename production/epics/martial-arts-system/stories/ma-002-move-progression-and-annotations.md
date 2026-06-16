# Story: ma-002 — 招式成长路径、残卷合成与批注版替换

> **Epic**: martial-arts-system
> **类型**: Logic
> **优先级**: P0 — 武学成长状态核心
> **Estimate**: M（约 4-6h）
> **依赖**: ma-001
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/martial-arts-system.md §Detailed Design, §States and Transitions, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-martial-arts-system-002
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

实现每个角色或存档中的招式成长状态：普通武学习得即满，高级/绝学通过残卷、拓本、完本、真传逐层成长，批注版作为奇遇终态直接替换当前进度。

## 范围

### 包含
- 普通武学：习得后 `completion=1.00`
- 高级/绝学：残卷 `0.55`、拓本 `0.72`、完本 `0.88`、真传 `1.00`
- 3 张同招式残卷合成拓本
- 完本/真传状态的特殊效果解锁标记
- 批注版：任意阶段触发后 `completion=1.00`，覆盖当前进度
- 多余残卷记录为可移交给物品系统处理的状态

### 不包含
- 伤害公式 → ma-003
- 批注条件和效果运行时判定 → ma-004
- 物品系统实际背包扣除/交易/分解 → item-system
- 修炼消耗体力和天数的日历集成 → 后续自然日/物品集成 story

## 技术说明

- 成长状态是运行时/存档状态，不应写回静态 YAML 定义
- 静态配置只定义招式类别、是否允许批注、批注组件和层级规则
- 状态变更接口应返回明确结果：成功、缺少残卷、已是终态、招式不存在、不可批注
- 批注版是终态：发现后不保留原完本/真传进度作为并行状态

## 验收标准

- [x] 普通武学习得后直接以 `completion=1.00` 可出战，无进度门槛
- [x] 获得第 1 张高级/绝学残卷后，该招式以残卷状态 `completion=0.55` 可出战
- [x] 3 张同一招式残卷可合成拓本，`completion=0.72`
- [x] 拓本可进入完本状态，`completion=0.88`，高级特效 / 绝学特效 v1 解锁
- [x] 叙事事件可将招式提升为真传，`completion=1.00`，绝学特效 v2 解锁
- [x] 触发批注版奇遇后，批注版直接替换当前状态，当前进度不保留
- [x] 不存在批注版的招式不显示批注入口，并以真传为最高形态

## QA 测试用例

- **AC-1**：普通武学习得即满
  - Given：玩家未习得普通武学“罗汉拳”
  - When：调用习得接口
  - Then：状态为已习得，`completion=1.00`，可装备出战
  - 边界：重复习得同一普通武学不产生重复记录

- **AC-2**：残卷到拓本
  - Given：玩家持有同一高级招式 2 张残卷
  - When：获得第 3 张并选择合成
  - Then：生成拓本状态，`completion=0.72`
  - 边界：2 张时不能合成；第 4 张记录为多余残卷

- **AC-3**：批注版覆盖
  - Given：玩家持有某招式完本状态
  - When：触发该招式批注版奇遇
  - Then：状态变为批注版，`completion=1.00`，原完本状态不作为并行状态保留
  - 边界：未习得招式直接获得批注版；不可批注招式触发失败

## 测试证据路径

`tests/unit/martial-arts/move_progression_and_annotations_test.cs`

## 依赖关系

- Depends on: ma-001
- Unlocks: ma-003, ma-004, ma-005

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

