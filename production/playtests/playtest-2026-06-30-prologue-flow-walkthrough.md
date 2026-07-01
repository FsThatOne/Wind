# Playtest Protocol — Prologue Flow Walkthrough

> **Date**: 2026-06-30  
> **Build**: local `develop` workspace, post prologue scene hook + quest flag gate pass  
> **Tester**: owner / developer walkthrough first, external player later  
> **Platform**: PC, Godot 4.7-stable Mono  
> **Input Method**: KB+M first; gamepad follow-up  
> **Session Type**: Targeted flow walkthrough  
> **Review Mode**: lean — CD-PLAYTEST skipped per `production/review-mode.txt`

## Test Focus

验证序章从 P00-01 到 P00-10 的实机场景流程是否能自然承接：

- 白天山庄日常保持温暖、无明显阴谋感。
- 师姐采药/采矿教学先于跑腿和取酒。
- 主角拖延取酒成立，且不是玩家失败。
- 崖洞互动能建立师姐回忆和夜宿理由。
- 次日返山搜证不触发战斗。
- 师兄归山后才触发误会、合葬、战斗教学占位和书信约定。
- 晚段内容不会在前置 flag 缺失时被误触。

## Build Evidence Before Walkthrough

| Check | Expected |
|---|---|
| `dotnet build feng-zhi/FengZhi.csproj` | PASS, 0 warnings |
| `dotnet test FengZhi.slnx` | PASS |
| `PrologueSceneDialogueHooksTest` | PASS |

## Walkthrough Route

### P00-01 醒在风止山庄

| Item | Record |
|---|---|
| Scene / entry | 风止山院或当前可用序章入口 |
| Expected feel | 寿宴前日常，安全、熟悉、有人照看 |
| Check | HUD 当前目标是否明确，不出现危机提示 |
| Result |  |
| Notes |  |

### P00-02 师姐采药

| Item | Record |
|---|---|
| Scene / marker | 厨房仓房小潭 / `herb_rack_inspect` |
| Dialogue | `sister_gather_herb_01.yaml` |
| Expected flag | `prologue_herb_tutorial_seen=true` |
| Gate check | 未采药前触发采矿应被温柔挡回 |
| Result |  |
| Notes |  |

### P00-03 第一次采矿与动物足迹

| Item | Record |
|---|---|
| Scene / marker | 厨房仓房小潭 / `medicine_pot_inspect`; 雾林小径 / `old_tree_inspect` |
| Dialogue | `sister_gather_ore_01.yaml`, `animal_tracks_mount_foreshadow_01.yaml` |
| Expected flags | `prologue_ore_tutorial_seen=true`, `prologue_mount_foreshadowed=true` |
| Design check | 脚印必须是动物足迹，只埋坐骑伏笔，不透露山庄外危机 |
| Result |  |
| Notes |  |

### P00-04 山庄跑腿

| Item | Record |
|---|---|
| Scene / marker | 风止山院 / `water_pond_inspect` |
| Dialogue | `manor_errands_01.yaml` |
| Expected flag | `prologue_manor_errands_started=true` |
| Gate check | 未完成动物足迹前，跑腿入口应被挡回 |
| Result |  |
| Notes |  |

### P00-05 书房小事

| Item | Record |
|---|---|
| Scene / marker | 正堂 / `master_talk`; 书房 / `scroll_pile_inspect`, `secret_compartment_inspect` |
| Dialogue | `master_study_01.yaml` |
| Expected flags | `books_organized=true`, `prologue_study_hidden_compartment_seen=true` |
| Design check | 灭门前只轻触暗格，不解释令牌、旧信或凶手 |
| Result |  |
| Notes |  |

### P00-06 拖到天黑

| Item | Record |
|---|---|
| Scene / marker | 风止山院 / `mist_gate_inspect` during day |
| Dialogue | `sister_wine_reminder_01.yaml` |
| Expected flag | `prologue_wine_delayed=true` |
| Gate check | 未开始跑腿前，不应直接催玩家取酒 |
| Emotional check | 是否像角色贪玩拖延，而不是玩家被系统惩罚 |
| Result |  |
| Notes |  |

### P00-07 夜入崖洞取酒

| Item | Record |
|---|---|
| Scene / marker | 后山崖洞 / `wine_pickup`, `storage_shelf`, `memory_marker`, `rest_spot` |
| Dialogue | `wine_pickup_01.yaml`, `storage_shelf_01.yaml`, `memory_marker_01.yaml`, `rest_spot_01.yaml` |
| Expected flags | `prologue_wine_obtained=true`, `prologue_cave_overnight=true` |
| Gate check | 未被师姐催取酒前，酒坛入口应被挡回；未取酒前，夜宿入口应被挡回 |
| Design check | 每个洞内互动都勾起师姐/山庄回忆 |
| Result |  |
| Notes |  |

### P00-08 异常安静，回庄

| Item | Record |
|---|---|
| Scene / marker | 雾林小径夜版 / `old_tree_inspect` |
| Dialogue | `massacre_return_01.yaml` |
| Expected flag | `prologue_silent_return_seen=true` |
| Gate check | 未夜宿崖洞前，不应触发返山异常 |
| Emotional check | 空、静、不理解是否成立 |
| Result |  |
| Notes |  |

### P00-09 惨案搜证

| Item | Record |
|---|---|
| Scene / marker | 正堂夜版 / `stone_wall_inspect`, `blood_letter_inspect`; 书房夜版 / `secret_compartment_inspect` |
| Dialogue | `massacre_evidence_01.yaml` |
| Expected flags | `prologue_massacre_discovered=true`, `prologue_blood_letter_obtained=true`, `prologue_sister_missing_known=true`, `prologue_study_compartment_empty_seen=true` |
| Design check | 不出现追杀战；不出现令牌、旧信、直接凶手解释 |
| Result |  |
| Notes |  |

### P00-10 师兄归山，误会与分别

| Item | Record |
|---|---|
| Scene / marker | 风止山院夜版 / `mist_gate_inspect`, `water_pond_inspect`, `motto_axis_inspect`; 正堂夜版 / `weapon_rack_inspect`, `tea_table_interact` |
| Dialogue | `senior_brother_return_01.yaml`, `senior_brother_misunderstanding_01.yaml`, `joint_burial_01.yaml`, `farewell_inheritance_01.yaml`, `letter_promise_01.yaml` |
| Expected flags | `prologue_senior_brother_returned=true`, `senior_brother_mis_resolved=true`, `prologue_joint_burial_completed=true` |
| Gate check | 未发现灭门前，不应触发师兄归山；未解除误会前，不应合葬；未合葬前，不应传承和约信 |
| Design check | 战斗教学只在此后作为临别赠礼出现 |
| Result |  |
| Notes |  |

## Observation Log

| Time | Segment | Observation | Severity | Follow-up |
|---|---|---|---|---|
|  |  |  |  |  |

## Confusion Points

| Segment | What was confusing? | Likely cause | Fix idea |
|---|---|---|---|
|  |  |  |  |

## Bugs Encountered

| # | Description | Severity | Reproducible | File / Scene |
|---|---|---|---|---|
|  |  |  |  |  |

## Design Checks

| Requirement | Status | Notes |
|---|---|---|
| 灭门前不教学战斗 |  |  |
| 采集段不透露山庄外危机 |  |  |
| 动物足迹只作为坐骑伏笔 |  |  |
| 主角拖延取酒成立 |  |  |
| 崖洞互动全部服务回忆 |  |  |
| 灭门搜证不指认凶手 |  |  |
| 书房暗格为空，不出现旧信/令牌 |  |  |
| 师兄误会在次日返山后触发 |  |  |
| 战斗教学在误会/合葬后出现 |  |  |

## Action Routing

### Design Changes Needed

- [ ] 待走查后填写。

### Bug Reports

- [ ] 待走查后填写。若可复现，使用 `/bug-report` 正式登记。

### Polish Items

- [ ] 待走查后填写。优先记录路线提示、文本节奏、目标 HUD 和交互点命名问题。

### Balance Adjustments

- 当前走查不覆盖数值平衡。

## Verdict

**PENDING WALKTHROUGH**

当前自动化与 Godot C# build 已通过，但本报告是走查协议，不代表实机流程已经完成验证。完成 Godot 运行后，将本文件补齐为正式 playtest report。
