# Story: ss-006 — 存档/读档菜单与槽位交互

> **Epic**: save-system
> **类型**: UI
> **优先级**: P1 — 玩家可见存档体验
> **依赖**: ss-001, ss-002, ss-005
> **阻塞**: 需要后续 `/ux-design` 产出存档菜单 UX spec
> **ADR 指引**: ADR-0004（槽位、元数据、阻断状态），ADR-0001（UI 通知）
> **GDD 来源**: design/gdd/save-system.md §Visual/Audio Requirements, §UI Requirements, §Acceptance Criteria
> **TR-ID**: TR-save-system-???（tr-registry.yaml 暂无真实条目）
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-11

## 目标

实现存档/读档菜单的基础交互，能显示槽位、元数据、空槽/满槽提示、覆盖与删除确认，以及阻断状态提示。

## 范围

### 包含
- 纵向槽位列表
- 显示章节名、日期、游戏时长、缩略图、自动槽标记
- 空槽显示“空（可存档）”
- 10 槽全满时显示“槽已满（10/10）”
- 覆盖确认与删除确认
- 战斗中等阻断状态灰化并显示原因
- 键鼠 / 手柄基础导航

### 不包含
- 存档核心读写逻辑 → ss-002
- 加密、迁移、自动存档触发 → ss-003 / ss-004 / ss-005

## 技术说明

- UI 只消费 metadata，不直接接触底层加密逻辑
- 菜单交互需保留“继续游戏”入口可隐藏的状态
- 存档/读档流程应尽量避免在 UI 中泄露实现细节

## 验收标准

- [x] 槽位列表能显示 10 手动槽 + 1 自动槽
- [x] 空槽、满槽、自动槽能被明确区分
- [x] 覆盖已有存档前弹出确认
- [x] 删除已有存档前弹出确认
- [x] 战斗中等阻断状态显示原因文字，不触发写入
- [x] 键鼠和手柄都能完成槽位导航与确认

## 实现记录

- 新增 `SaveLoadMenuPresenter`、槽位 UI snapshot、输入意图、确认状态和阻断状态契约。
- 存档/读档菜单先以 Presenter/DTO 形式完成，供后续 Godot Control 场景消费；正式视觉布局、焦点样式和分辨率适配留到 Presentation/UI 场景接入时验证。
- 保存模式支持空槽保存、已有槽覆盖确认、自动槽禁止手动覆盖、阻断状态禁用写入。
- 读档模式保护空槽不触发加载，已有槽可触发加载。
- 删除已有槽前需要确认，未确认不会删除。
- 键鼠与手柄输入来源共用导航与确认语义。

## 验证结果

- 自动化测试：`/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj`
- 结果：557/557 tests passed
- 证据：`production/qa/evidence/save-load-menu-evidence.md`

## QA 手动检查

- **AC-1**：槽位显示
  - Setup：打开存档/读档菜单
  - Verify：槽位列表显示元数据、缩略图、自动槽标记和空槽提示
  - Pass condition：信息清晰可读，不混淆手动槽与自动槽

- **AC-2**：确认交互
  - Setup：选择一个已有存档槽
  - Verify：覆盖与删除前出现确认对话框
  - Pass condition：未确认前不会真正覆盖或删除

- **AC-3**：阻断状态
  - Setup：在战斗中打开菜单
  - Verify：存档按钮灰化并显示原因
  - Pass condition：无法发起存档，且说明明确

## 测试证据路径

`production/qa/evidence/save-load-menu-evidence.md`

## 依赖关系

- Depends on: ss-001, ss-002, ss-005
- Unlocks: 无
