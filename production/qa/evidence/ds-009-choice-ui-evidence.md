# ds-009 选择 UI、心境暗示与特殊选项呈现证据

> **日期**: 2026-06-11
> **Story**: production/epics/dialogue-system/stories/ds-009-choice-ui-mindset.md
> **状态**: 逻辑/输入契约已验证；Godot Control 视觉与 dual-focus 实机验证待正式 UI 场景接入

## 自动化验证

执行命令：

```bash
/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj
```

结果：

- 510/510 tests passed
- 覆盖文件：`tests/Core/Dialogue/DialogueChoiceUiTests.cs`

## 覆盖的验收标准

- Choice 可见选项：`DialogueChoiceUiTests.ChoiceNode_ShowsVisibleOptionsInEditOrderWithoutHiddenPlaceholders`
  - 验证选项面板显示运行时可见选项、保持编辑顺序、隐藏不可用项且不留灰化占位。
- 心境暗示：`DialogueChoiceUiTests.MindsetOption_ShowsLiteraryHintWithoutAxisDeltaOrFormula`
  - 验证心境选项使用文学暗示，不显示 axis、delta、公式或具体数值。
- 洞察提示：`DialogueChoiceUiTests.InsightCue_PersistsUntilPlayerInvestigatesOrAdvances`
  - 验证洞察提示无限期保持、可追查、追查后进入隐藏分支。
- 暗号样式：`DialogueChoiceUiTests.CodePhraseOption_UsesDistinctStyleWithoutRevealingMatchRule`
  - 验证暗号选项使用 CodePhrase 样式标记，并不暴露匹配上下文。
- 选择确认：`DialogueChoiceUiTests.ConfirmSelection_ReturnsHighlightSnapshotAndImmediatelyAdvancesBranch`
  - 验证确认时返回短暂高亮快照，同时运行时立即沿分支推进。
- 键鼠/手柄输入：`DialogueChoiceUiTests.KeyboardMouseAndGamepad_CanNavigateAndConfirmWithoutChangingTarget`
  - 验证两种输入来源的导航与确认目标一致。

## 手动验证遗留

当前主线尚未建立正式 Presentation/UI 场景结构；本 story 完成的是 Presenter/DTO 层契约。以下检查需在后续正式 Godot Control 场景接入后补做：

- 标准、心境、洞察、暗号、兜底选项的最终视觉样式区分。
- 鼠标 hover 与手柄焦点在 Godot 4.6 dual-focus 下的并存行为。
- 选中项短暂高亮的真实动画时长、音效反馈和无重复确认。
- Steam Deck 或手柄实机导航、确认、输入模式切换后的焦点恢复。
