# cu-003 资源条与伤害反馈 — Test Evidence

> 日期：2026-06-14  
> Story：`production/epics/combat-ui/stories/cu-003-resource-bars-and-damage-feedback.md`  
> 类型：Visual/Feel  
> 证据状态：自动契约已覆盖；实机视觉走查待后续 Godot 场景绑定确认

## 自动测试覆盖

- `CombatUiResourceBarsDamageFeedbackTest`
  - 覆盖所有参战角色的气血、内息、破绽资源展示条目。
  - 覆盖气血受击闪白与平滑扣减入口。
  - 覆盖内息变化即时可见。
  - 覆盖破绽达到 5 时深红脉冲与“破绽！”浮字提示。
  - 覆盖克制、同系、被克、暴击、一击决胜五类伤害数字样式。
  - 覆盖同帧 6 个伤害浮字稳定分 lane，不丢失。
  - 覆盖 `DamageNumberPool` 预分配 12 个非交互 Label，符合 ADR-0011。

## 实机走查项

- 在 Godot 场景中确认气血条先闪白再平滑扣减，`resource_bar_update_speed = 0.25s` 体感清楚。
- 在 Godot 场景中确认内息消耗与调息恢复的变化能被玩家立即看见。
- 在 Godot 场景中确认破绽达到 5 时，资源条深红脉冲与目标处“破绽！”不会遮挡关键单位。
- 在 Godot 场景中确认 6 个同帧伤害数字在目标附近可读，不重叠到不可辨认。
- 在 Godot 场景中确认池化 Label 回收后不保留键盘/手柄焦点。

## 结论

cu-003 的展示契约和自动验证已就绪。视觉手感相关项目属于 Visual/Feel 验证，需要在 `/story-done` 或后续 UI 场景接入时做实机确认。
