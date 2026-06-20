<!--
PROTOTYPE - NOT FOR PRODUCTION
Burst+Read Engine spike · Godot 4.7-stable + C# (.NET 8)
Date: 2026-06-02
-->

# Burst+Read Engine Spike

Throwaway Godot project for validating **4 feel-class questions** from the paper prototype:

1. "一招分胜负" 视觉爆发感是否成立？
2. intent tell 的 UI 呈现是否清晰？
3. 回合切换 pacing 是否符合 "短而重"？
4. simultaneous resolution 的视觉清晰度？

**Not for production.** Code is hardcoded, no architecture, single .cs file with nested types.

---

## 运行步骤

### 1. 第一次打开（导入 + .NET restore）

```bash
cd /Users/bytedance/my-game/prototypes/burst-read-combat-concept/engine
godot --path . --editor
```

Godot 会：
1. 检测到 .csproj → 提示生成 `.sln` （点 yes）
2. 自动 import 所有资源（约 5-10 秒）
3. 自动 build C# 项目（首次会下载 NuGet 包，30 秒到 2 分钟）
4. 打开编辑器

如果 build 失败：
- 检查 `dotnet --version`：必须 ≥ 8.0
- 检查 Godot 是否是 **.NET 版**（标准版没 C# 支持）
- 项目根可能产生 `.godot/` `obj/` `bin/` —— 正常

### 2. 运行 spike

在 Godot 编辑器中：
- 按 **F5** 运行主场景，或菜单 **Run → Play**
- 第一次会弹"未选主场景"对话框 → 选 **Combat.tscn**

---

## 你应该看到什么

### 场景布局

```
┌─────────────────────────────────────────────────────────────┐
│  ┌───────────┐   ┌─────────────┐   ┌───────────┐            │
│  │ 主角       │   │ 回合 N      │   │ 李无双     │           │
│  │ HP /NeiXi  │   │ ─────       │   │ HP /NeiXi  │           │
│  │ /Stagger   │   │ 刚→主角     │   │ /Stagger   │           │
│  └───────────┘   └─────────────┘   └───────────┘            │
│                                                              │
│  ┌──────────────── 战斗 log（带颜色） ──────────────┐         │
│  │  回合 1                                          │         │
│  │  李无双 意图: 刚系 → 主角                         │         │
│  │  主角 → 巧系·回风落雁                             │         │
│  │  反制成功！→ 李无双 受 6 伤害 [克制 1.5×]        │         │
│  └──────────────────────────────────────────────────┘         │
│                                                              │
│  ★ 一击决胜窗口开启 ★                                         │
│  [刚·白虹贯日] [柔·守拙式] [巧·回风落雁] [★ 回风落雁·决胜]    │
└─────────────────────────────────────────────────────────────┘
```

### 关键视觉验证点

| 验证项 | 你应该看到 |
|---|---|
| **Intent tell（Q2）** | 李无双 intent label 的 **系颜色编码**（刚=红 / 柔=蓝 / 巧=绿）+ scale pulse 动画。一眼就能看出"对方要打刚系" |
| **决策按钮** | 招式按钮带系颜色 + 内息成本；内息不足按钮 disabled（变灰）|
| **Simultaneous resolution（Q4）** | 双方招式同回合飘字 + 牌面横向 shake，能同时看到双方受击 |
| **Pacing（Q3）** | intent reveal → 等待选择 → 结算 → 700ms 缓冲 → 下回合。每回合约 5-10s 节奏 |
| **一击决胜视觉（Q1）** | 当对方破绽 3 / HP < 30% / 心法满血+反制成功时，按钮区域出现 **★ 一击决胜窗口开启 ★** banner。点决胜版按钮会触发全屏白光闪 + "一招分胜负！" 大字（96pt 金色）渐入渐出 + 黄色超大伤害飘字 |

### 关键交互流

1. 看 intent → 决定反制 / 卸力 / 硬攻
2. 内息够时选反制（巧·回风落雁，3 内息）→ 看反制成功 log + 高伤害飘字
3. 把李无双 HP 砸到 < 30% → 看到决胜窗口 banner 出现
4. 点决胜版按钮 → 看视觉爆发演出

---

## 报告给我

把以下信息粘回来：

### A. 第一次跑出错？

把 Godot console（**编辑器底部 Output 面板**）的报错粘出来：
- 编译错误：CSC: ...
- 运行时错误：ERROR: ...
- Scene 加载错误：[GdScript] 或 [Scene]

### B. 跑起来了？

回答 4 个 feel 问题，**每个 1-2 句**：

| Q | 你的观察 |
|---|---|
| Q1 一招分胜负的视觉爆发感？ | __________ |
| Q2 intent tell 清晰度？ | __________ |
| Q3 回合 pacing 是否"短而重"？ | __________ |
| Q4 simultaneous resolution 视觉清晰度？ | __________ |

加一句**总体感受** + **PROCEED / TWEAK / REDESIGN** 投票。

---

## 已知简化（不要试图修）

- 只有 1v1（主角 vs 李无双）
- 没有美术资产 / 音效
- 心境系统 / 顿悟 / 一击决胜窗口缩减规则等都没实现
- 招式只 3 招（主角）/ 3 招（李无双）
- AI 是简单 if-else
- 没有 main menu / settings / 存档
- 数值全部硬编码

这些都是**有意**的简化。如果不影响 4 个 feel 问题判断，**不需要修**。

---

## 如果想改改看

- `scripts/CombatScene.cs` 末尾 `MakePlayer()` / `MakeEnemy()` 改招式
- `scripts/CombatScene.cs` 中 `ShowDecisiveBurst()` 改决胜演出
- `scripts/CombatScene.cs` 中 `Sleep(N)` 调用调 pacing
- 修改后保存 → Godot 自动 hot reload C# script

---

**完成 spike 后：** prototype 报告会更新一段 "Engine Spike Findings"，然后 Burst+Read 战斗 prototype 整段完成，转入 `/design-system 战斗` 或 `/art-bible`。
