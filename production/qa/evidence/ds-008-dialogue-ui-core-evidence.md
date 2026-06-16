# ds-008 对话 UI 核心呈现与输入证据

> **日期**: 2026-06-11
> **Story**: production/epics/dialogue-system/stories/ds-008-dialogue-ui-core.md
> **状态**: 逻辑/输入契约已验证；Godot Control 视觉与 dual-focus 实机验证待正式 UI 场景接入

## 自动化验证

执行命令：

```bash
/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj
```

结果：

- 504/504 tests passed
- 覆盖文件：`tests/Core/Dialogue/DialogueUiPresenterTests.cs`

## 覆盖的验收标准

- Speech 节点：`DialogueUiPresenterTests.SpeechNode_ShowsSpeakerPortraitNameplateAndText`
  - 验证名牌、说话者、立绘显示标记、当前说话者高亮和正文。
- 打字机与选项延迟：`DialogueUiPresenterTests.Typewriter_HidesChoicePanelUntilTextIsComplete`
  - 验证逐字显示期间不显示选项面板；确认补全文字后仍不误选；第二次确认才进入 Choice。
- 确认键规则：`DialogueUiPresenterTests.Confirm_FirstCompletesText_SecondAdvancesNode`
  - 验证第一次确认补全当前节点，第二次确认推进下一节点。
- InnerMonologue：`DialogueUiPresenterTests.InnerMonologue_UsesIndependentStyleWithoutNpcPortrait`
  - 验证使用独立 `InnerMonologue` 模式、显示内心标签、不显示 NPC 立绘。
- Narration：`DialogueUiPresenterTests.Narration_HasNoNameplateOrPortrait`
  - 验证无名牌、无说话者、无立绘。
- Letter：`DialogueUiPresenterTests.LetterNode_OpensOverlayAndCloseReturnsToDialogueFlow`
  - 验证打开信笺覆盖层、焦点目标为信笺面板、关闭后返回对话流程。
- 键鼠/手柄基础输入：`DialogueUiPresenterTests.KeyboardMouseAndGamepadInputs_ShareConfirmAndLetterCloseBehavior`
  - 验证两种输入来源共享确认/加速语义和焦点目标。

## 手动验证遗留

当前主线尚未建立正式 Presentation/UI 场景结构；Godot 项目与 `Main.tscn` 仍位于 prototype 目录。因此本 story 先完成可被 Control 层消费的 Presenter/DTO 契约，以下检查需在后续正式 UI 场景接入后补做：

- 对话框半透明暗色底板、白字可读性、立绘高亮的真实视觉效果。
- Letter 全屏信笺覆盖层与标准对话框的视觉区分。
- Godot 4.6 dual-focus 下键鼠/手柄切换后的焦点边框、高亮和 `PushFocus` / `PopFocus` 行为。
- Steam Deck 或手柄实机确认、加速、关闭信笺无重复触发。
