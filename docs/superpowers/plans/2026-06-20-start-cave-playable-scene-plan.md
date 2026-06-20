# Start Cave Playable Scene Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the approved 3-5 minute playable start-cave prototype in `feng-zhi`, covering movement, investigation prompts, inner monologue, insight follow-up, and one-time item pickups for “寿酒” and `止血散 x1`.

**Architecture:** Keep this as a lightweight Godot C# prototype. `StartCaveGame` owns scene-local interaction state and UI, while `CavePlayer` owns player movement only. The prototype does not connect to formal Foundation, inventory, save, quest, or dialogue systems.

**Tech Stack:** Godot 4.6.3 Mono, C# / .NET 8, `Godot.NET.Sdk/4.6.3`, existing `feng-zhi/StartCave.tscn` background art.

## Global Constraints

- Communicate with the user in Chinese.
- Do not commit unless the user explicitly asks for a commit.
- Before implementation edits, ask: “May I write this changeset to `feng-zhi/FengZhi.csproj`, `feng-zhi/project.godot`, `feng-zhi/StartCave.tscn`, `feng-zhi/scripts/CavePlayer.cs`, and `feng-zhi/scripts/StartCaveGame.cs`?”
- Scope is the `feng-zhi` Godot prototype only.
- No battle, formal save, scene transition, complex UI art, quest log, massacre reveal, or formal inventory integration.
- Target playtime is 3-5 minutes.
- Tone is warm, familiar, and lightly playful.

---

## File Structure

- Create `feng-zhi/FengZhi.csproj`: Godot C# project file so the prototype can compile C# scripts.
- Create `feng-zhi/scripts/CavePlayer.cs`: player movement component only.
- Create `feng-zhi/scripts/StartCaveGame.cs`: scene-local state, interaction detection, prompt UI, monologue UI, insight text, and pickup feedback.
- Modify `feng-zhi/project.godot`: add C# feature flag, viewport size, stretch mode, and input actions.
- Modify `feng-zhi/StartCave.tscn`: attach `StartCaveGame`, add player node, UI labels, and five `Area2D` interaction points.

---

### Task 1: Godot C# Project and Input Setup

**Files:**
- Create: `feng-zhi/FengZhi.csproj`
- Modify: `feng-zhi/project.godot`

**Interfaces:**
- Produces: a buildable Godot C# project named `FengZhi`.
- Produces input actions consumed by later tasks: `move_left`, `move_right`, `move_up`, `move_down`, `interact`, `insight`.

- [ ] **Step 1: Ask permission for the implementation changeset**

Ask the user:

```text
我准备开始实现。可以写入这组文件吗？

- feng-zhi/FengZhi.csproj
- feng-zhi/project.godot
- feng-zhi/StartCave.tscn
- feng-zhi/scripts/CavePlayer.cs
- feng-zhi/scripts/StartCaveGame.cs
```

Expected: user explicitly approves before any implementation edit.

- [ ] **Step 2: Create the Godot C# project file**

Create `feng-zhi/FengZhi.csproj`:

```xml
<Project Sdk="Godot.NET.Sdk/4.6.3">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <RootNamespace>FengZhi</RootNamespace>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Update `project.godot` for C#, viewport, and input**

Replace `feng-zhi/project.godot` with:

```ini
; Engine configuration file.
; It's best edited using the editor UI and not directly,
; since the parameters that go here are not all obvious.
;
; Format:
;   [section] ; section goes between []
;   param=value ; assign values to parameters

config_version=5

[application]

config/name="FengZhi"
run/main_scene="uid://dq57hnpshsgyy"
config/features=PackedStringArray("4.6", "C#", "Forward Plus")
config/icon="res://icon.svg"

[display]

window/size/viewport_width=1152
window/size/viewport_height=648
window/stretch/mode="canvas_items"
window/canvas_textures/default_texture_filter=1

[dotnet]

project/assembly_name="FengZhi"

[input]

move_left={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":65,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null), Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":4194319,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null)]
}
move_right={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":68,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null), Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":4194321,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null)]
}
move_up={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":87,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null), Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":4194320,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null)]
}
move_down={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":83,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null), Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":4194322,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null)]
}
interact={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":69,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null), Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":32,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null)]
}
insight={
"deadzone": 0.5,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":70,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null)]
}

[physics]

3d/physics_engine="Jolt Physics"

[rendering]

textures/canvas_textures/default_texture_filter=0
rendering_device/driver.windows="d3d12"
```

- [ ] **Step 4: Run build to verify project file discovery**

Run:

```bash
dotnet build feng-zhi/FengZhi.csproj
```

Expected: build succeeds, or fails only because scripts do not exist yet if Task 2 has not been implemented. If the local default SDK causes an MSBuild task-host issue, rerun with the repo's known SDK 8 environment:

```bash
DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 dotnet build feng-zhi/FengZhi.csproj
```

Expected after Task 1 alone: `Build succeeded`.

---

### Task 2: Player Movement and Scene Skeleton

**Files:**
- Create: `feng-zhi/scripts/CavePlayer.cs`
- Create: `feng-zhi/scripts/StartCaveGame.cs`
- Modify: `feng-zhi/StartCave.tscn`

**Interfaces:**
- Consumes input actions from Task 1.
- Produces node names consumed by Task 3:
  - `Player`
  - `UiLayer/PromptLabel`
  - `UiLayer/MessagePanel`
  - `UiLayer/MessagePanel/MessageLabel`
  - `UiLayer/InventoryLabel`
  - `Interactions/*`

- [ ] **Step 1: Create `CavePlayer.cs`**

Create `feng-zhi/scripts/CavePlayer.cs`:

```csharp
using Godot;

namespace FengZhi;

public partial class CavePlayer : CharacterBody2D
{
    private const float Speed = 220.0f;

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = direction * Speed;
        MoveAndSlide();
    }
}
```

- [ ] **Step 2: Create a minimal `StartCaveGame.cs` skeleton**

Create `feng-zhi/scripts/StartCaveGame.cs`:

```csharp
using Godot;

namespace FengZhi;

public partial class StartCaveGame : Node2D
{
    public override void _Ready()
    {
        GD.Print("Start cave playable scene ready.");
    }
}
```

- [ ] **Step 3: Replace `StartCave.tscn` with a scripted scene skeleton**

Replace `feng-zhi/StartCave.tscn` with:

```ini
[gd_scene load_steps=7 format=3 uid="uid://dq57hnpshsgyy"]

[ext_resource type="Texture2D" uid="uid://c3tk627h43mvr" path="res://起始山洞.png" id="1_cv6gc"]
[ext_resource type="Script" path="res://scripts/StartCaveGame.cs" id="2_game"]
[ext_resource type="Script" path="res://scripts/CavePlayer.cs" id="3_player"]

[sub_resource type="CircleShape2D" id="CircleShape2D_player"]
radius = 13.0

[sub_resource type="RectangleShape2D" id="RectangleShape2D_table"]
size = Vector2(88, 60)

[sub_resource type="RectangleShape2D" id="RectangleShape2D_default"]
size = Vector2(96, 72)

[node name="StartCave" type="Node2D" unique_id=2112748860]
script = ExtResource("2_game")

[node name="Background" type="Sprite2D" parent="."]
texture = ExtResource("1_cv6gc")
centered = false
scale = Vector2(0.59412, 0.798)

[node name="Player" type="CharacterBody2D" parent="."]
position = Vector2(155, 474)
script = ExtResource("3_player")

[node name="Body" type="ColorRect" parent="Player"]
offset_left = -9.0
offset_top = -24.0
offset_right = 9.0
offset_bottom = 6.0
color = Color(0.88, 0.86, 0.78, 1)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Player"]
shape = SubResource("CircleShape2D_player")

[node name="Interactions" type="Node2D" parent="."]

[node name="DeskNote" type="Area2D" parent="Interactions"]
position = Vector2(556, 315)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Interactions/DeskNote"]
shape = SubResource("RectangleShape2D_table")

[node name="TrainingMarks" type="Area2D" parent="Interactions"]
position = Vector2(632, 220)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Interactions/TrainingMarks"]
shape = SubResource("RectangleShape2D_default")

[node name="BirthdayWine" type="Area2D" parent="Interactions"]
position = Vector2(984, 260)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Interactions/BirthdayWine"]
shape = SubResource("RectangleShape2D_default")

[node name="SupplyChest" type="Area2D" parent="Interactions"]
position = Vector2(350, 518)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Interactions/SupplyChest"]
shape = SubResource("RectangleShape2D_default")

[node name="HerbBasket" type="Area2D" parent="Interactions"]
position = Vector2(270, 342)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Interactions/HerbBasket"]
shape = SubResource("RectangleShape2D_default")

[node name="UiLayer" type="CanvasLayer" parent="."]

[node name="PromptLabel" type="Label" parent="UiLayer"]
visible = false
offset_left = 432.0
offset_top = 560.0
offset_right = 720.0
offset_bottom = 600.0
horizontal_alignment = 1
text = "E 调查"

[node name="InventoryLabel" type="Label" parent="UiLayer"]
offset_left = 24.0
offset_top = 18.0
offset_right = 420.0
offset_bottom = 54.0
text = "任务物品：未取得寿酒"

[node name="MessagePanel" type="Panel" parent="UiLayer"]
visible = false
offset_left = 176.0
offset_top = 470.0
offset_right = 976.0
offset_bottom = 625.0

[node name="MessageLabel" type="Label" parent="UiLayer/MessagePanel"]
offset_left = 24.0
offset_top = 18.0
offset_right = 776.0
offset_bottom = 132.0
autowrap_mode = 3
text = ""
```

- [ ] **Step 4: Build after scene skeleton**

Run:

```bash
dotnet build feng-zhi/FengZhi.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 5: Manual movement smoke**

Open `feng-zhi/project.godot` in Godot 4.6.3 and run the project.

Expected:

- Scene starts with the cave background.
- A small pale player marker appears near the left entrance.
- WASD and arrow keys move the marker.
- No runtime errors appear in the Godot output panel.

---

### Task 3: Interaction Content, Insight, Pickups, and Verification

**Files:**
- Modify: `feng-zhi/scripts/StartCaveGame.cs`

**Interfaces:**
- Consumes scene nodes from Task 2 by exact `NodePath`.
- Consumes input actions: `interact` for normal investigation, `insight` for follow-up.
- Produces local state:
  - `hasBirthdayWine`
  - `chestLooted`
  - `trainingInsightSeen`

- [ ] **Step 1: Replace `StartCaveGame.cs` with complete interaction logic**

Replace `feng-zhi/scripts/StartCaveGame.cs` with:

```csharp
using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class StartCaveGame : Node2D
{
    private const int PlayerInsight = 10;
    private const int TrainingInsightThreshold = 8;

    private readonly Dictionary<string, InteractionDefinition> _interactions = new()
    {
        ["DeskNote"] = new(
            "中央桌",
            "小师弟，若你又把酒坛认成药坛，回来罚你抄剑谱。\n\n她明知道我早分得清了，还是每回都要这样写。"),
        ["TrainingMarks"] = new(
            "练功痕迹",
            "墙上还留着从前练剑时画下的起手式。线条有些歪，却看得出当时改了很多遍。",
            "那一笔是她补的。她总说我腕太硬，剑未出，意先绷住了。"),
        ["BirthdayWine"] = new(
            "寿酒",
            "取得：寿酒"),
        ["SupplyChest"] = new(
            "木箱",
            "取得：止血散 x1"),
        ["HerbBasket"] = new(
            "药篓",
            "这些药材多半是师姐晒的。她总说我分不清辛温寒凉。")
    };

    private Label _promptLabel = null!;
    private Label _inventoryLabel = null!;
    private Panel _messagePanel = null!;
    private Label _messageLabel = null!;
    private Area2D? _focusedArea;
    private bool _hasBirthdayWine;
    private bool _chestLooted;
    private bool _trainingInsightSeen;

    public override void _Ready()
    {
        _promptLabel = GetNode<Label>("UiLayer/PromptLabel");
        _inventoryLabel = GetNode<Label>("UiLayer/InventoryLabel");
        _messagePanel = GetNode<Panel>("UiLayer/MessagePanel");
        _messageLabel = GetNode<Label>("UiLayer/MessagePanel/MessageLabel");

        foreach (var child in GetNode<Node2D>("Interactions").GetChildren())
        {
            if (child is Area2D area)
            {
                area.BodyEntered += body => OnInteractionEntered(area, body);
                area.BodyExited += body => OnInteractionExited(area, body);
            }
        }

        UpdateInventoryLabel();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact"))
        {
            if (_messagePanel.Visible)
            {
                HideMessage();
                return;
            }

            InteractWithFocusedArea();
        }

        if (@event.IsActionPressed("insight") && !_messagePanel.Visible)
        {
            TryInspectFocusedArea();
        }
    }

    private void OnInteractionEntered(Area2D area, Node2D body)
    {
        if (body.Name != "Player")
        {
            return;
        }

        _focusedArea = area;
        UpdatePrompt();
    }

    private void OnInteractionExited(Area2D area, Node2D body)
    {
        if (body.Name != "Player" || _focusedArea != area)
        {
            return;
        }

        _focusedArea = null;
        UpdatePrompt();
    }

    private void InteractWithFocusedArea()
    {
        if (_focusedArea is null)
        {
            return;
        }

        switch (_focusedArea.Name)
        {
            case "BirthdayWine":
                if (_hasBirthdayWine)
                {
                    ShowMessage("寿酒已经取好了，回去莫要耽搁。");
                    return;
                }

                _hasBirthdayWine = true;
                UpdateInventoryLabel();
                ShowMessage(_interactions["BirthdayWine"].Text);
                return;
            case "SupplyChest":
                if (_chestLooted)
                {
                    ShowMessage("已经翻过了，里头只剩些干草和旧绳。");
                    return;
                }

                _chestLooted = true;
                ShowMessage(_interactions["SupplyChest"].Text);
                return;
            default:
                if (_interactions.TryGetValue(_focusedArea.Name, out var interaction))
                {
                    ShowMessage(interaction.Text);
                }

                return;
        }
    }

    private void TryInspectFocusedArea()
    {
        if (_focusedArea?.Name != "TrainingMarks")
        {
            return;
        }

        if (PlayerInsight < TrainingInsightThreshold)
        {
            return;
        }

        if (_trainingInsightSeen)
        {
            ShowMessage("师姐补过的那几笔，我已经记下了。");
            return;
        }

        _trainingInsightSeen = true;
        ShowMessage(_interactions["TrainingMarks"].InsightText);
        UpdatePrompt();
    }

    private void ShowMessage(string text)
    {
        _messageLabel.Text = text;
        _messagePanel.Visible = true;
        _promptLabel.Visible = false;
    }

    private void HideMessage()
    {
        _messagePanel.Visible = false;
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (_focusedArea is null || _messagePanel.Visible)
        {
            _promptLabel.Visible = false;
            return;
        }

        var prompt = "E / 空格 调查";
        if (_focusedArea.Name == "TrainingMarks" &&
            PlayerInsight >= TrainingInsightThreshold &&
            !_trainingInsightSeen)
        {
            prompt = "E / 空格 调查    F 洞察";
        }

        _promptLabel.Text = prompt;
        _promptLabel.Visible = true;
    }

    private void UpdateInventoryLabel()
    {
        _inventoryLabel.Text = _hasBirthdayWine
            ? "任务物品：寿酒"
            : "任务物品：未取得寿酒";
    }

    private readonly record struct InteractionDefinition(
        string Title,
        string Text,
        string InsightText = "");
}
```

- [ ] **Step 2: Build after interaction logic**

Run:

```bash
dotnet build feng-zhi/FengZhi.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 3: Manual verification in Godot**

Run `feng-zhi/project.godot` in Godot 4.6.3.

Verify:

- Player can move from the left entrance to central, right, and foreground areas.
- Standing near central desk shows `E / 空格 调查`.
- Pressing `E` near central desk shows the师姐 paper note and inner monologue.
- Pressing `E` again closes the message panel.
- Standing near wall marks shows `E / 空格 调查    F 洞察`.
- Pressing `E` near wall marks shows the normal training-mark text.
- Pressing `F` near wall marks shows the insight follow-up text.
- Pressing `F` there again shows the already-noted text.
- Pressing `E` near right wine jars shows `取得：寿酒`.
- The inventory label changes from `任务物品：未取得寿酒` to `任务物品：寿酒`.
- Pressing `E` near the wine jars again shows `寿酒已经取好了，回去莫要耽搁。`
- Pressing `E` near the foreground chest shows `取得：止血散 x1`.
- Pressing `E` near the chest again shows `已经翻过了，里头只剩些干草和旧绳。`
- Pressing `E` near the herb basket shows the herb-basket inner monologue.
- Godot output panel has no script/runtime errors.

- [ ] **Step 4: Record evidence**

Create a short manual note in the final response, not a new file unless the user requests QA evidence:

```text
手动验证：Godot 4.6.3 启动成功；移动、5 个交互点、寿酒一次性拾取、宝箱一次性拾取、洞察追查均通过。
```

If Godot is unavailable in PATH or cannot be launched from the terminal, state that clearly and provide the completed build result plus the exact manual steps above for user-side validation.

---

## Self-Review

- Spec coverage: movement is covered by Task 2; five interaction points, monologue, insight, birthday wine, and chest pickup are covered by Task 3; out-of-scope exclusions are preserved in Global Constraints.
- Placeholder scan: no unresolved placeholder markers are allowed in this plan.
- Type consistency: `CavePlayer`, `StartCaveGame`, `InteractionDefinition`, and all node names match the scene skeleton and later code.
