# ss-006 存档/读档菜单与槽位交互证据

> **日期**: 2026-06-11
> **Story**: production/epics/save-system/stories/ss-006-save-load-menu-ui.md
> **状态**: UI Presenter / 输入契约已验证；Godot Control 视觉实机验证待正式 Presentation 场景接入

## 自动化验证

执行命令：

```bash
/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj
```

结果：

- 557/557 tests passed
- 覆盖文件：`tests/unit/save-system/save-load-menu-presenter_test.cs`

## 覆盖的验收标准

- 槽位列表：`SaveLoadMenuPresenterTests.GetSnapshot_ShowsTenManualSlotsAndOneAutosaveWithMetadata`
  - 验证 10 个手动槽 + 1 个自动槽、章节、场景、缩略图、空槽提示和自动槽标记。
- 满槽提示：`SaveLoadMenuPresenterTests.GetSnapshot_AllManualSlotsOccupied_ShowsFullSlotText`
  - 验证 10 个手动槽全满时显示 `槽已满（10/10）`。
- 覆盖确认：`SaveLoadMenuPresenterTests.ConfirmSaveOnOccupiedSlot_RequiresOverwriteConfirmationBeforeWriting`
  - 验证覆盖已有存档前先进入确认状态，未二次确认前不写入。
- 删除确认：`SaveLoadMenuPresenterTests.DeleteOnOccupiedSlot_RequiresDeleteConfirmationBeforeDeleting`
  - 验证删除已有存档前先进入确认状态，未二次确认前不删除。
- 阻断状态：`SaveLoadMenuPresenterTests.SaveBlocked_DisablesSaveAndReturnsReasonWithoutWriting`
  - 验证战斗等阻断原因会显示，手动槽保存不可用，确认不会触发写入。
- 键鼠/手柄导航：`SaveLoadMenuPresenterTests.KeyboardMouseAndGamepadInputs_NavigateAndConfirmEquivalently`
  - 验证键鼠与手柄输入来源共享移动焦点与确认语义。
- 读档空槽保护：`SaveLoadMenuPresenterTests.LoadMode_EmptySlotDoesNotLoadOccupiedSlotLoads`
  - 验证空槽不能读档，已有存档槽可以读档。

## 手动验证遗留

当前正式 Presentation/UI 场景尚未建立，因此本 story 先完成可被 Godot Control 层消费的 Presenter/DTO 契约。以下检查需在后续正式 UI 场景接入后补做：

- 纵向槽位列表在 PC 与 Steam Deck 分辨率下的可读性。
- 缩略图、章节名、日期、游戏时长、自动槽标记的真实视觉层级。
- 覆盖/删除确认弹窗的焦点默认项、取消项和手柄返回键行为。
- 战斗中灰化按钮、阻断原因文案与输入焦点是否清晰。
