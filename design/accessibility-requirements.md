---
title: 无障碍需求规范 (Accessibility Requirements)
project: 风止 (Wind Stops)
status: Draft
tier: Standard (WCAG 2.1 AA)
owner: UX Lead
created: 2026-06-09
last_updated: 2026-06-09
related_docs:
  - design/gdd/game-concept.md
  - design/ux/interaction-patterns.md (TBD)
  - design/ux/hud.md (TBD)
target_platforms:
  - PC (Windows / Linux)
  - Steam Deck
input_devices:
  - 键盘 + 鼠标
  - 手柄 (Xbox / PlayStation / Steam Controller)
---

# 无障碍需求规范

> 本文档定义《风止》在 Pre-Production 与 Production 阶段必须满足的无障碍基线，
> 是所有 UX spec、HUD design 与 Feature 实现的契约约束。

---

## 1. Tier & Rationale

**选定 Tier**: **Standard (WCAG 2.1 AA)**

### 1.1 选择理由

1. **平台特性**: 项目主战场为 PC + Steam Deck，玩家群体涵盖键鼠玩家与手柄玩家，Steam 评测对无障碍敏感度高（特别是字号/手柄重映射），低于 Standard 会被持续负评。
2. **玩法特性**: Burst+Read 回合制天然友好——无实时操作压力、可暂停、回合内可反悔（取决于 ADR-0011 设计），Motor 维度成本低；但 **朦胧化 UI**（战斗外文学化）会显著抬高 Cognitive 维度门槛，需在 Tier 内重点投入。
3. **内容特性**: 武侠 RPG 文字密集（对话/书信/招式描述），对低视力玩家与阅读障碍玩家不友好，必须保证字号可调 + 对比度 AA + 字幕完备。
4. **资源特性**: 独立团队，Comprehensive (AAA) 的屏幕朗读 + 完整手语成本不可承受；Basic 又不足以撑起 Steam Deck Verified 与主流口碑。

### 1.2 范围边界

- ✅ 覆盖：视觉 / 运动 / 认知 / 听觉 四大维度的 WCAG 2.1 AA 基线
- ✅ 覆盖：键鼠 + 手柄 + Steam Deck 三类输入
- ✅ 覆盖：25 个 GDD 系统的逐项检查矩阵
- ❌ 不覆盖（v1.0 范围外）：完整屏幕朗读 (TTS)、手语翻译、本地化无障碍（v2.0 考虑）、专用色盲调色板模式（v1.5 升级，v1.0 用双轨编码替代）、UI 整体缩放（Post-Launch 评估）

### 1.3 升级路径

- **v1.0 (本文档)** → Standard，与 Pre-Production 阶段对齐
- **v1.5 (Production 中期)** → Standard + Comprehensive 部分功能（高对比度模式 / 多色盲模式）
- **v2.0 (Post-Launch)** → 评估社区反馈后决定是否冲刺 Comprehensive

### 1.4 Compliance Targets

| 标准 | 等级 | 验证时机 |
|------|------|---------|
| WCAG 2.1 | AA | Beta 阶段第三方审计 |
| Game Accessibility Guidelines | Basic + Intermediate 全覆盖，Advanced 50%+ | Production 中期自审 |
| Xbox Accessibility Guidelines | 适用项 80%+ | Beta 阶段自审（即使不上 Xbox 也遵循） |
| Steam Deck Verified | 全部 5 项 | Beta 阶段 Valve 提交 |


## 2. Visual Accessibility

### 2.1 Text & Typography

**Required (必须)**:
- **基础字号**: UI 主体字号 ≥ 16pt @ 1080p（Steam Deck 720p 等比缩放后 ≥ 14pt 实际像素）
- **字号缩放档位**: 100% / 125% / 150% / 200%，至少 4 档
  - 200% 档位 UI 不得发生溢出/截断/遮挡（需 Reflow 而非 Overflow）
- **字体选择**: 中文使用思源宋体（衬线，正文）+ 思源黑体（无衬线，UI 控件），禁止使用纯艺术字作为可读文本
- **行高**: ≥ 1.5 倍字号（WCAG 2.1 AA 1.4.12）
- **段落间距**: ≥ 字号的 2 倍

**Forbidden (禁止)**:
- 禁止使用 < 14pt @ 1080p 的可读文本（图标 tooltip 除外，且必须可放大）
- 禁止使用纯图片承载关键信息（如未提供 alt 文本/说明）
- 禁止字号缩放后导致信息丢失（必须 reflow）

### 2.2 Color & Contrast

**Required**:
- **正文对比度**: 文本/背景 对比度 ≥ **4.5:1** (WCAG 2.1 AA 1.4.3)
- **大字号文本** (≥ 18pt 或 14pt 加粗) 对比度 ≥ **3:1**
- **UI 控件边界**: 可交互元素与背景对比度 ≥ **3:1** (WCAG 2.1 AA 1.4.11)
- **状态指示**: 不得 **仅** 依赖颜色传达信息（如"红=敌/绿=友"必须叠加图标或形状）(WCAG 2.1 AA 1.4.1)

**Forbidden**:
- 禁止水墨晕染背景上直接放低对比度文本（必须叠加纸张底/磨砂底）
- 禁止仅用颜色区分状态/属性

### 2.3 Color-Blind Strategy

> **v1.0 设计决策**: 不实现专用色盲模式（Deuteranopia/Protanopia/Tritanopia 等调色板替换），
> 改为通过 **强制双轨编码** 满足 WCAG 2.1 AA 1.4.1 (Use of Color) 要求。
> 色盲模式列入 v1.5 升级路径。

**Required（强制双轨编码规则）**:
- **颜色 + 图标/形状/文字 三选一**: 任何用颜色表达的状态/类别，必须叠加至少一种非颜色编码
  - 红色（敌/危险/低值）→ 叠加 ⚠️ 警告图标 / 实心方块 / "敌"字标
  - 绿色（友/安全/高值）→ 叠加 ✓ 标记 / 圆形 / "友"字标
  - 黄色（中立/警示）→ 叠加 ◇ 菱形 / "中"字标
  - 心境状态色 → 必须叠加文字描述（如"心如止水"）
  - 误会状态色 → 必须叠加 6 状态图标（无论开启朦胧化与否）
  - 顿悟进度色 → 必须叠加进度数值或刻度标记
- **武功/招式色彩区分** → 必须叠加图标或文字标签
- **小地图/标记** → 必须用形状（圆/方/三角）+ 颜色双重区分
- **审计要求**: Production 阶段 UX-Lead 必须用 **灰度截图** 审计所有 HUD/UI 关键状态，确保去色后仍可读

**Forbidden**:
- 禁止任何"仅靠颜色"传达的信息（含战斗、对话、地图、菜单全场景）
- 禁止仅用红/绿表达成功/失败（必须叠加 ✓/✗ 或文字）

**v1.5 升级目标（文档化记录，不在 v1.0 实现）**:
- 氘色盲模式 (Deuteranopia)
- 原色盲模式 (Protanopia)
- 蓝黄色盲模式 (Tritanopia)
- 全色盲模式 (Achromatopsia)

### 2.4 UI Scaling

> **v1.0 设计决策**: UI 整体不提供缩放，所有 UI 元素采用固定布局。
> 字号缩放（Section 2.1）仍提供 4 档以满足 WCAG 2.1 AA 1.4.4 (Resize Text)。
> UI 缩放列入 Post-Launch 评估范围。

**Required**:
- UI 布局必须在 **1080p 与 720p (Steam Deck)** 两个分辨率下均不发生截断/遮挡
- Steam Deck 720p 下采用专用布局参数（设计稿同步出 Deck 版本），而非运行时缩放
- 字号缩放（见 2.1）独立工作，不影响 UI 控件几何尺寸
  - 字号 200% 时若文本溢出 UI 容器，必须 **Reflow**（容器自动撑高/换行），而非整体缩放

**Forbidden**:
- 禁止使用运行时整体缩放（Control.scale）作为适配手段
- 禁止 720p 下出现 UI 截断


### 2.5 Motion & Flashing

**Required**:
- **运动减弱开关 (Reduce Motion)**: 关闭/减弱以下效果
  - 镜头摇晃 (CameraRequestBus 强度 ≤ 30%)
  - 屏幕震动
  - 大幅 UI 过渡动画（淡入淡出 ≤ 100ms，无平移/缩放）
  - 战斗演出的运动模糊
- **闪烁限制**: 任何 1 秒内闪烁次数 ≤ **3 次**（WCAG 2.1 AA 2.3.1，光敏性癫痫预防）
- **闪光强度**: 全屏白闪 / 红闪强度限制（峰值亮度差 ≤ 50%）

**Forbidden**:
- 禁止任何 > 3Hz 的高频闪烁（即使在动画/演出中）
- 禁止强制性运动效果（必须可在 Reduce Motion 下被替代）


## 3. Motor Accessibility

### 3.1 Input Remapping

**Required**:
- **全键位重映射**: 100% 操作（含战斗、菜单、对话推进、跳过）必须可重映射
  - 不得有"硬编码"按键（即不可重绑定的按键）
- **重映射粒度**: 单键、组合键（Shift/Ctrl/Alt + 字母）、Modifier
- **手柄重映射**: 通过 **Steam Input API** 实现（社区可分享配置）+ 游戏内自带 UI
- **预设方案**: 至少提供 3 套预设
  - 默认（标准武侠 RPG）
  - 单手键盘（仅左手区域：QWER/ASDF/ZXCV）
  - 左右镜像（默认手柄左右键反转）
- **冲突检测**: 重映射时实时显示冲突，禁止保存冲突配置
- **重置选项**: 一键恢复默认

**Forbidden**:
- 禁止硬编码按键（含 Esc / F-key 系统级按键的可选重映射例外）
- 禁止重映射界面只能用鼠标操作（必须支持键盘和手柄完成全流程）

### 3.2 Controller Full Navigation

**Required**:
- **100% 手柄可达**: 所有 UI 屏幕（菜单/HUD/对话/存档/设置/暂停/Map/Inventory）必须 100% 可用手柄完成
- **焦点可见性**: 当前焦点元素必须有清晰高亮（≥ 3:1 对比度边框 + 可选放大效果）
- **焦点循环**: Tab/方向键/摇杆移动焦点不得"卡死"（焦点循环或显式边界提示）
- **快捷键映射**: PC 键鼠的快捷键（如 Tab 切换页签）必须有手柄等价映射（如 LB/RB）
- **触控板支持**: Steam Deck 触控板默认作为 **鼠标光标** 替代（可选关闭）

**Forbidden**:
- 禁止"鼠标必需"的交互（如必须拖拽到精确像素位置）
- 禁止焦点丢失（任何 UI 状态下必须有可见焦点）

### 3.3 Steam Deck Specific

**Required（Steam Deck Verified 5 项）**:
1. **Input**: 默认手柄配置（无需用户配置即可玩）
2. **Display**: 720p 默认配置不发生 UI 截断
3. **Seamlessness**: 无键盘 prompt（除非有手柄等价提示）
4. **System Support**: 退出/休眠/恢复正常工作
5. **Default Configuration**: 默认图形配置稳定 ≥ 30 FPS

**Recommended**:
- 触控屏点击作为可选输入（覆盖战斗/对话/菜单）
- Trackpad 做精细瞄准/光标控制
- Steam Deck 720p 使用专用布局参数（设计阶段产出，非运行时缩放，详见 Section 2.4）

### 3.4 Hold-to-Toggle Conversion

**Required**:
- **任何长按操作必须提供"切换模式"备选**:
  - 长按瞄准 → 单击切换瞄准模式
  - 长按跑步 → 单击切换跑/走
  - 长按集气 → 自动集气至上限或单击触发
- **切换状态视觉提示**: 切换模式时必须有持续可见的状态指示（HUD 图标）
- **设置项位置**: 设置 → 控制 → 切换/长按 二级开关，每个动作独立

**Forbidden**:
- 禁止"必须连续按住 X 秒"的强制时序（必须提供 Toggle 替代）

### 3.5 Combat Timing Tolerance

**Required**:
- **回合制无强制时序**: 玩家可以暂停思考任意时长（无 Timer 倒计时）
  - 例外：可选挑战模式（成就解锁用）可启用 Timer，但默认关闭
- **Burst+Read 阶段切换**: Burst 阶段（连击决策）允许"慢速模式"开关
  - 慢速模式下 TimeScale 0.5x（影响 Tween，需走 TimeScaleController 优先级栈）
- **演出可跳过**: 所有非首次播放的过场（CutsceneService）必须可一键跳过
  - 首次播放也必须可跳过（按住 X 1 秒）
- **QTE 替代**: 若存在 QTE（待 GDD 确认），必须提供"自动通过"或"延长窗口至 3x"选项

**Forbidden**:
- 禁止任何无法被暂停/跳过的强制时序内容
- 禁止依赖 reflex 反应的关卡/Boss 设计（武侠 RPG 不应有此类）


## 4. Cognitive Accessibility

### 4.1 Tutorial Pacing

**Required**:
- **教程可重读**: 所有教程提示必须存档于"江湖见闻录"或类似 codex，玩家可随时回看
- **教程不强制**: 进阶教程（连击、心境、误会等）必须可跳过；新手必要教程（基础移动、菜单）可跳过但默认开启
- **教程速度**: 教程文本不得自动消失（必须玩家点击/按键继续）
- **教程节奏**: 单次教程 ≤ 3 个新概念，避免认知过载
- **复合系统拆解**: 误会系统、心境双轴等复杂系统必须分阶段引入（first-encounter vs full-mechanics）

**Forbidden**:
- 禁止"一次性介绍 5+ 概念"的信息倾泻
- 禁止教程文本自动消失（除环境装饰性文本外）

### 4.2 Pause & Save Anywhere

**Required**:
- **任意时刻暂停**: 战斗内、对话中、过场中均可暂停（按 Esc / Start）
  - 例外：CutsceneService 锁定期可选不可暂停，但必须有"暂停场景"占位（黑屏 + 提示）
- **多存档槽**: ≥ 5 个手动存档槽 + 3 个自动存档槽
- **存档点密度**: 每个剧情段落（5-15 分钟）至少 1 个 autosave
- **快速保存/读取**: F5 / F9 + 可自定义按键

**Forbidden**:
- 禁止单存档槽
- 禁止 > 20 分钟无存档点的关卡段落

### 4.3 Literary UI Readability （朦胧化 UI 重点）

> **核心矛盾**: 项目设计上战斗外刻意使用文学化描述（"心如止水" vs "心境值 75"），
> 这对 Cognitive 障碍玩家是显著门槛，需通过"数值化模式"开关化解。

**Required**:
- **数值化模式 (Numerical Mode)**: 设置 → 显示 → 朦胧化 UI 开关
  - **关闭**时（默认）：保持文学化描述（设计意图）
  - **开启**时：所有文学化描述并列显示数值（如"心如止水 (75/100)"）
- **数值化模式覆盖范围**:
  - 心境双轴（attitude / mood）
  - 误会系统状态（六态机的当前状态名 + 数值化进度）
  - 角色好感度（如有）
  - 顿悟突破进度
- **Tooltip 必须**: 所有文学化术语 hover/focus 显示标准定义（如"心如止水 = 心境值 ≥ 75"）
- **战斗内**: 已是数值化 UI（HP/SP/连击数），无需切换

**Forbidden**:
- 禁止仅用文学化描述传达**关键玩法信息**（如剩余生命、危险警告）
- 禁止数值化模式下数值与文学描述不一致

### 4.4 Tooltip & Glossary

**Required**:
- **江湖见闻录 (Glossary)**: 内置词条库，覆盖所有专有术语（武功名、招式、心境、势力、人物）
- **首次出现高亮**: 文学化术语首次出现时，文字带可点击下划线，链接到见闻录
- **自动收录**: 玩家遇到的所有 NPC、地点、武功首次出现自动收录见闻录
- **Tooltip 延迟**: 鼠标 hover 200ms 触发；手柄按 Y/Triangle 触发当前焦点词条
- **见闻录搜索**: 至少支持"按名称搜索"

### 4.5 Difficulty / Story Mode

> **v1.0 设计决策**: 难度锁定到存档，新建时选定后存档生命周期内不可更改。
> 辅助开关（自动战斗、决策回退、战斗重试）独立于难度，可随时切换。

**Required**:
- **难度档位**: 至少 3 档
  - **故事模式 (Story)**: 战斗伤害减半，敌方生命减 30%，专注剧情
  - **武侠模式 (Standard)**: 默认平衡
  - **江湖模式 (Hard)**: 战斗参数标准化，挑战为主
- **难度锁定到存档**: 新建存档时必须明确选择难度，**存档生命周期内不可更改**
  - 新建难度选择 UI 必须显式提示"难度选定后无法修改"
  - 提示采用警告色 + 图标 + 二次确认（防止误选）
  - 存档列表必须可见每个存档的难度标签（不仅依赖颜色，遵循 Section 2.3 双轨编码）
- **难度独立辅助开关**: 以下辅助选项独立于难度，可随时切换（不锁档）
  - 自动战斗（AI 接管 Burst 决策）
  - 关键决策可重选（误会系统对话 1 次内可回退）
  - 战斗失败重试（无惩罚 vs 损失声望）

**Forbidden**:
- 禁止运行时切换难度
- 禁止"故事模式"剥夺剧情成就（仅可保留挑战向成就为高难度专属）
- 禁止难度选择 UI 缺少不可逆警告


## 5. Auditory Accessibility

### 5.1 Subtitles

**Required**:
- **字幕全覆盖**: 所有语音（NPC 对话、过场配音、Boss 战独白）必须有字幕
  - 包括环境音/旁白（"风声"、"远处传来打斗声"等氛围音也需文字提示）
- **字幕默认开启**: 首次启动游戏字幕默认 ON
- **字幕字号**: 独立于 UI 字号设置，4 档（小/中/大/特大），与 Section 2.1 一致
- **字幕背景**: 必须提供 **磨砂底/纯黑底/无背景** 三选项（默认磨砂底）
  - 透明字幕必须保证文字与背景对比度 ≥ 4.5:1（可加描边或阴影）
- **字幕停留时间**: 与朗读时长成比例，至少 ≥ 字数 × 100ms（中文按字符算）
- **字幕区域**: 默认底部 20% 安全区，不得遮挡角色面部或关键 UI
- **字幕回放**: 对话历史回看（按 H 键 / 手柄等价）保留最近 50 条

**Forbidden**:
- 禁止纯白字幕无描边/无背景叠加在浅色场景
- 禁止无法关闭的强制大字幕
- 禁止字幕速度无法调节

### 5.2 Speaker Identification

**Required**:
- **说话人前缀**: 字幕必须明确标注说话角色名（如"林渊：今夜月色甚好"）
  - 旁白 → "（旁白）" 前缀
  - 内心独白 → "（心声）" 前缀 或 斜体
  - 未知角色 → "（神秘人）" 等占位（保持悬念时也需有标识）
- **多人同时说话**: 必须分行展示，每行带前缀，不混在一起
- **角色色彩可选**: 字幕可选择"为每个角色分配专属色彩"（与 Section 2.3 双轨编码兼容，必须叠加角色名前缀）

### 5.3 Sound Effect Visualization

**Required（关键音效视觉化）**:
- **战斗关键音效**: 必须有视觉对应
  - 招式触发 → 屏幕边缘高亮闪烁 + 招式名称浮现
  - 受击/格挡 → 屏幕震动（受 Reduce Motion 影响）+ 数值飘字
  - 暴击/破防 → 醒目特效 + 文字提示"暴击！"/"破防！"
- **环境提示音**: 战斗外重要音效（脚步声、远处警示）需可选"音效字幕"
  - 设置 → 音频 → 音效字幕开关
  - 开启时屏幕边缘出现"（脚步声 - 后方）""（剑鸣 - 远处）"等文字提示
- **方位提示**: 涉及方向的音效（左右、前后）必须叠加方向图标或方位文字

**Forbidden**:
- 禁止仅靠音效传达战斗关键信息（如 BOSS 大招前摇）

### 5.4 Audio Channel Mixing

**Required**:
- **独立音量滑块**: 至少 5 路
  - Master（总音量）
  - Music（背景音乐）
  - SFX（音效）
  - Voice（人声/配音）
  - Ambient（环境音）
- **滑块粒度**: 0-100，至少 1 单位调节
- **静音选项**: 每路可单独静音
- **预设方案**: 至少 3 套
  - 默认（均衡）
  - 强人声（Voice 100%, Music 30%）—— 听障玩家友好
  - 沉浸（Ambient 100%, Voice 80%, Music 60%）
- **音量记忆**: 设置后跨存档持久化
- **单声道输出**: 提供"立体声 / 单声道（左右合并）"切换，单耳听障玩家友好

**Forbidden**:
- 禁止仅提供"总音量"单一控制
- 禁止任何路声音不可静音


## 6. Platform API Integration

### 6.1 Steam Input API

**Required**:
- **集成 Steam Input SDK**: 通过 GodotSteam 或自研 GDExtension 接入
- **手柄重映射委托**: Section 3.1 的手柄重映射 100% 通过 Steam Input 完成（不自实现）
- **Action Set 定义**: 必须在 Steam Partner 后台定义至少以下 Action Set
  - `Combat`（战斗内）
  - `Exploration`（江湖层探索）
  - `Menu`（UI 导航）
  - `Cutscene`（演出，仅跳过/确认）
- **Action Glyphs**: 游戏内 UI 按键提示必须从 Steam Input 动态读取（玩家用 PS/Xbox/Switch 控制器时显示对应符号）
- **配置共享**: 玩家可上传/下载社区配置

**Forbidden**:
- 禁止硬编码 XInput 按键贴图（必须走 Steam Input Glyph API）
- 禁止绕过 Steam Input 直接读取 SDL 手柄（PC 平台 Steam 启动场景下）

### 6.2 Godot Accessibility Hooks

**Required**:
- **Godot 4.7-stable Native Accessibility**: 启用 `display/window/accessibility/enabled` (project settings)
- **Control 节点 a11y 元数据**: 所有可交互 Control 节点必须设置:
  - `accessibility_name`（可读名称）
  - `accessibility_description`（操作说明）
  - `accessibility_role`（按钮/列表/标签等语义角色）
- **焦点可见性 API**: 使用 `Control.grab_focus()` + 自定义 focus theme override，避免依赖默认焦点框（默认焦点框对比度未必达标）
- **TimeScale 兼容**: TimeScaleController 必须暴露"无障碍优先级槽位"（accessibility 优先级 ≥ 演出优先级），保证慢速模式不被演出抢占

**Forbidden**:
- 禁止使用 Godot Sprite/绘图 API 绘制可交互按钮（必须使用 Control 子类，否则无法被未来屏幕朗读器识别）

### 6.3 Steam Deck Verified 检查项映射

| Verified 项 | 本文档对应章节 | 验证方式 |
|------------|--------------|---------|
| Input | Section 3.1, 3.2, 6.1 | 手动测试：纯手柄完成新建存档→战斗→存档退出 |
| Display | Section 2.1, 2.4, 5.1 | 截图测试：720p 下所有 UI/字幕不截断 |
| Seamlessness | Section 3.1, 6.1 | 检查：所有按键提示用 Steam Input Glyph，无键盘键名残留 |
| System Support | Section 4.2 | 测试：Deck 休眠 5min 恢复后游戏正常继续 |
| Default Configuration | Section 6.4 | 性能测试：Deck 默认图形配置 ≥ 30 FPS（连续 30 分钟） |

### 6.4 Performance Floor for Accessibility

> 性能不仅是体验问题，也是无障碍问题：低帧率会放大 Reduce Motion 用户的不适。

**Required**:
- **最低帧率保证**: Steam Deck 默认配置下 ≥ **30 FPS**（连续 30 分钟稳定）
- **PC 推荐配置 (1080p)**: ≥ **60 FPS**
- **PC 高端配置 (1440p+)**: ≥ **60 FPS**
- **帧率掉落处理**: 帧率 < 30 FPS 时自动降低粒子/特效等级（独立于 Reduce Motion 设置）
- **加载时间**: 单次场景加载 ≤ **10 秒**（Steam Deck），≤ **5 秒**（PC SSD），过长加载必须有进度条 + 旋转图标

**Forbidden**:
- 禁止在加载页缺少进度反馈（黑屏 > 3 秒视为不可接受）


## 7. Per-Feature Accessibility Matrix

> **标记说明**：
> - **V** = Visual（指向 Section 2.x）
> - **M** = Motor（指向 Section 3.x）
> - **C** = Cognitive（指向 Section 4.x）
> - **A** = Auditory（指向 Section 5.x）
> - 单元格内填写关键子节号，"—" 表示不适用，"⚠" 表示高风险关注点

| # | System | V | M | C | A | 关键风险点 |
|---|--------|---|---|---|---|----------|
| 1 | 角色属性 / 功力 | 2.1, 2.2 | — | 4.3 ⚠（数值化模式必备） | — | 心境/功力数值文学化 |
| 2 | 回合制战斗 (Burst+Read) | 2.1, 2.5 | 3.5 | 4.2, 4.5 | 5.3 | TimeScale 慢速模式; 暂停 |
| 3 | 武学组合 | 2.2, 2.3 | 3.1 | 4.4 | — | 武功色彩区分必须双轨 |
| 4 | 敌方 AI | 2.5 | — | — | 5.3 | Intent tell 必须视觉化 |
| 5 | 对话系统 | 2.1, 2.2 | 3.2 | 4.4 | 5.1, 5.2 ⚠ | 字幕全覆盖 + 说话人标识 |
| 6 | 心境双轴 | 2.3 ⚠ | — | 4.3 ⚠ | — | 数值化模式核心场景 |
| 7 | 战斗 UI | 2.1, 2.2, 2.3 | 3.2 | — | — | 焦点可见性; 对比度 4.5:1 |
| 8 | 存档系统 | 2.1 | 3.2 | 4.2 ⚠, 4.5 | — | 多存档槽; 难度标签 |
| 9 | 主线叙事 / 章节推进 | 2.1 | — | 4.4 | 5.1 | 关键剧情字幕完备 |
| 10 | NPC 状态管理 | 2.3 | — | 4.3 ⚠ | — | 态度文学化必须可数值化 |
| 11 | 自然日 + 体力 | 2.2 | — | 4.3 | — | "倦怠/精力充沛"需数值化 |
| 12 | 地图 / 场景管理 | 2.1, 2.3 ⚠ | 3.2 | 4.4 | 5.3 | 小地图标记双轨编码 |
| 13 | 感情系统（彗星模型）| — | — | 4.3 ⚠, 4.4 | — | "渐远/渐近"需数值化 |
| 14 | 朦胧化 UI | — | — | 4.3 ⚠⚠ | — | **本系统 = 数值化模式开关核心** |
| 15 | 物品 / 道具 | 2.2, 2.3 | 3.2 | 4.4 | — | 装备稀有度色彩双轨 |
| 16 | 活江湖层 | — | — | 4.4 ⚠ | — | 传闻/暗号需见闻录索引 |
| 17 | 顿悟突破 | 2.5 | — | 4.3, 4.4 | 5.3 | 突破演出可跳过; 进度数值化 |
| 18 | 误会系统 | 2.3 ⚠ | — | 4.3 ⚠⚠, 4.5 | — | 6 状态机必须双轨; 决策回退 |
| 19 | 探索 / 洞察 | 2.2 | 3.2 | 4.4 | 5.3 | 提示音必须视觉对应 |
| 20 | CG / 演出 | 2.5 ⚠ | 3.5 ⚠ | — | 5.1 ⚠ | 闪烁限制; 可跳过; 字幕 |
| 21 | 音乐 / 音效 | — | — | — | 5.3, 5.4 ⚠ | 5 路独立音量 + 音效字幕 |
| 22 | 教学 / 引导 | 2.1 | 3.1 ⚠ | 4.1 ⚠⚠ | 5.1 | 节奏 ≤3 概念; 可重读 |
| 23 | 设置 / 选项 | 2.1, 2.2 | 3.1 ⚠⚠ | — | 5.4 | **所有无障碍开关入口** |
| 24 | 成就 / Steam 集成 | 2.3 | — | 4.5 | — | 难度限定成就需明示 |
| 25 | 队伍管理 / 同伴成长 | 2.1, 2.3 | 3.2 | 4.3, 4.4 | — | 5 人队列焦点导航; 同伴心境数值化 |

### 7.1 高风险系统专项（Top 5 关注度）

按 ⚠⚠ 标记排序，列出最需要 UX-Lead 跟进的 5 个系统：

1. **#14 朦胧化 UI**: 数值化模式开关的实现核心，决定其余朦胧化系统是否合规（Section 4.3）
2. **#18 误会系统**: 6 状态机色彩 + 文学化进度，必须双轨编码 + 数值化模式 + 决策回退
3. **#22 教学 / 引导**: 教程节奏控制 + 输入提示一致性，影响所有新手玩家可达性
4. **#23 设置 / 选项**: 所有无障碍开关汇总入口，UI 复杂度高 + 重映射必须自洽
5. **#13 感情系统**: 彗星模型"渐远/渐近"高度文学化，且贯穿剧情，必须数值化 + 见闻录索引


## 8. Test Plan

### 8.1 Manual Test Cases

**Required（每个测试包必须覆盖）**:

#### TC-A: 视觉无障碍包（Section 2）
- TC-A1: 切换字号 4 档，所有 HUD/菜单/对话不溢出（截图对比）
- TC-A2: 灰度截图测试 —— 关键 HUD 状态（战斗/误会/心境/小地图）去色后仍可读
- TC-A3: 对比度仪测量 —— 至少 30 个 UI 状态对比度 ≥ 4.5:1
- TC-A4: Reduce Motion ON/OFF 测试 —— 镜头摇晃、屏幕震动符合规约
- TC-A5: 闪烁测试 —— 录屏后用 PEAT (Photosensitive Epilepsy Analysis Tool) 扫描

#### TC-B: 运动无障碍包（Section 3）
- TC-B1: 纯键盘完成新建存档→序章→第一次战斗→存档退出
- TC-B2: 纯手柄（Xbox）完成同上路径
- TC-B3: Steam Deck 上完成同上路径（720p 默认配置）
- TC-B4: 重映射所有按键到非默认位置，所有交互仍可达
- TC-B5: Hold-to-Toggle 切换：长按→切换模式各 3 个动作覆盖测试

#### TC-C: 认知无障碍包（Section 4）
- TC-C1: 数值化模式 ON/OFF 切换 —— 心境/误会/感情/顿悟 4 个 UI 表现一致
- TC-C2: 江湖见闻录覆盖率检查 —— 序章 + 第一章所有专有术语 100% 收录
- TC-C3: 教程跳过/重读流程 —— 跳过教程后能从见闻录回看
- TC-C4: 难度锁定测试 —— 新建后无法在设置中改难度，UI 提示明示
- TC-C5: 暂停穿透测试 —— 战斗、对话、过场均可暂停

#### TC-D: 听觉无障碍包（Section 5）
- TC-D1: 静音运行游戏完成序章 —— 所有关键信息可通过字幕/视觉获取
- TC-D2: 字幕所有变体（小/中/大/特大 × 磨砂/纯黑/无背景）截图测试
- TC-D3: 单声道输出测试（仅左/仅右耳机）—— 所有方位音效仍可识别
- TC-D4: 音量预设切换 —— 默认/强人声/沉浸 三套均可正常工作

### 8.2 Automated Checks

**Required（CI 自动化检查清单，可在 GdUnit4 内实现）**:

| 检查项 | 实现方式 | 频率 |
|-------|---------|------|
| 对比度检查 | 静态分析 theme.tres，扫描所有 Color 对比度 | 每次 PR |
| 字号下限检查 | 扫描所有 Label/RichTextLabel 默认字号 ≥ 16 | 每次 PR |
| 焦点可达性 | 自动 walk 所有 UI 场景，模拟 Tab/方向键，检测焦点循环 | 每日 |
| 字幕覆盖率 | 扫描所有语音 asset，检查是否有对应字幕条目 | 每次 PR |
| 硬编码按键检查 | grep 检查源码中是否有 KEY_* 常量直接使用（应走 InputMap） | 每次 PR |
| 性能 baseline | 运行 30 分钟 soak 测试，记录帧率分布 | 每周 |

### 8.3 External Audit

**Required（Beta 阶段必须完成）**:
- 委托第三方无障碍审计机构进行 WCAG 2.1 AA 全面审计（推荐 Game Accessibility Conference 推荐机构）
- 至少 5 名残障玩家试玩反馈（视障 1、运动 1、认知 1、听障 1、综合 1）
- Steam Deck Verified 自助提交 + Valve 反馈循环

**Recommended**:
- Production 中期一次中期审计（不发证书，仅诊断）
- Post-Launch 30 天内一次复审（基于玩家反馈）

### 8.4 Player Feedback Channels

**Required**:
- 游戏内反馈入口（设置 → 反馈 → 无障碍专题）
- Steam 社区 Hub 设置"无障碍"标签话题
- 邮箱白名单收件（accessibility@项目域名）
- Beta 阶段建立残障玩家测试者社群（10-20 人）

**Forbidden**:
- 禁止将无障碍反馈与一般 bug 反馈混合（必须独立分类）


## 9. Known Limitations

### 9.1 v1.0 明确不覆盖的范围

| 项 | 状态 | 计划升级 | 替代方案 |
|----|------|---------|---------|
| **专用色盲调色板模式** (Deuteranopia/Protanopia/Tritanopia/Achromatopsia) | 不实现 | v1.5 (Production 中期) | Section 2.3 强制双轨编码 |
| **UI 整体缩放** | 不实现 | Post-Launch 评估 | Section 2.1 字号 4 档 + Section 2.4 720p/1080p 双布局 |
| **屏幕朗读 (TTS)** | 不实现 | v2.0 评估 | Section 6.2 仅打底 a11y 元数据 |
| **手语翻译/视频解说** | 不实现 | 不计划 | 字幕全覆盖代偿 |
| **本地化无障碍** | 不实现（仅中英） | v2.0 | 中英双语字幕完备 |
| **完整运动症缓解** (FOV slider / 第一人称晕动) | N/A | — | 项目为 2D 像素风，不涉及第一人称 |

### 9.2 设计权衡说明

#### 9.2.1 朦胧化 UI 与 Cognitive 冲突
- **冲突**: 文学化描述（"心如止水"）是设计核心，但对认知障碍玩家是门槛
- **权衡**: 通过"数值化模式"开关化解，**不会**为了无障碍弱化文学化（默认仍文学化）
- **风险**: 玩家可能不知道开关存在 → 必须在新建存档/教程阶段引导

#### 9.2.2 难度锁定到档 与 Cognitive 冲突
- **冲突**: WCAG 通常推荐"任意时刻调整难度"
- **权衡**: 项目设计意图（"一读定生死"）要求难度承诺
- **缓解**: 提供"故事模式"作为最低门槛 + 辅助开关随时可切（自动战斗/决策回退/重试）
- **风险**: 误选难度后无法补救 → 必须二次确认 UI

#### 9.2.3 UI 不缩放 与 Steam Deck Verified 冲突
- **冲突**: Steam Deck Display 项要求 720p 不截断
- **权衡**: 用专用 720p 布局而非运行时缩放
- **风险**: 设计阶段需双路布局产出 → UX 工作量增加 ~20%

### 9.3 已知技术债（实施时关注）

- **TimeScale 与 Tween 兼容性**: 慢速模式下 Tween 行为需走 TimeScaleController 优先级栈（详见 ADR-0011/0013）
- **Steam Input Glyph 离线**: Steam 离线启动时 Glyph API 可能不返回，需缓存默认贴图
- **江湖见闻录的内容更新**: 词条收录 = 内容运营成本，需建立维护流程

### 9.4 不在本文档范围

以下话题不在本文档约束范围（属于其他文档/团队）：

- 内容警告 (Content Warnings) → 由 narrative-director 负责，记于剧本元数据
- 性别 / 文化敏感性 → 由 narrative-director + localization 负责
- 反作弊 / 防沉迷 → 由发行方/法务负责
- GDPR / 数据隐私 → 由 backend / legal 负责


## 10. Audit History

| 日期 | 审计人 | 范围 | 结论 |
|------|--------|------|------|
| 2026-06-09 | UX Lead (skill: ux-design) | 文档 v1.0 初稿全章节 | Draft - 待 stakeholder review |
| TBD (Production 中期) | UX Lead | TC-A/B/C/D 全套手动测试 | TBD |
| TBD (Beta) | 第三方审计机构 | WCAG 2.1 AA 全面审计 | TBD |
| TBD (Beta) | 5 名残障玩家 | 试玩反馈 | TBD |
| TBD (Post-Launch +30d) | UX Lead | 玩家反馈复审 | TBD |


## 11. External Resources

### 11.1 标准与指南
- [WCAG 2.1 (W3C)](https://www.w3.org/TR/WCAG21/) — 本文档主要合规基准
- [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/) — 游戏行业最常用的检查清单（Basic/Intermediate/Advanced 三级）
- [Xbox Accessibility Guidelines (XAG)](https://learn.microsoft.com/en-us/gaming/accessibility/) — 即使不上 Xbox 也建议参考
- [Steam Deck Verified 要求](https://partner.steamgames.com/doc/steamdeck/compatibility) — Section 6.3 映射依据

### 11.2 工具
- [PEAT (Photosensitive Epilepsy Analysis Tool)](https://trace.umd.edu/peat) — 闪烁测试工具（Section 8.1 TC-A5）
- [Color Oracle](https://colororacle.org/) — 色盲模拟器（双轨编码审计辅助）
- [Stark](https://www.getstark.co/) — 对比度仪（Section 8.1 TC-A3）

### 11.3 项目内部参考
- [game-concept.md](file:///Users/bytedance/my-game/design/gdd/game-concept.md) — 项目核心概念
- [systems-index.md](file:///Users/bytedance/my-game/design/gdd/systems-index.md) — 25 系统索引（Section 7 矩阵依据）
- [ADR-0011 战斗 UI 与动画](file:///Users/bytedance/my-game/docs/architecture/adr-0011-combat-ui-animation.md) — TimeScaleController / 慢速模式接入点
- [ADR-0013 演出系统](file:///Users/bytedance/my-game/docs/architecture/adr-0013-cutscene-system.md) — CutsceneService / 跳过协议
- [ADR-0014 活江湖层](file:///Users/bytedance/my-game/docs/architecture/adr-0014-living-jianghu-layer.md) — Flag Namespace Registry


## 12. Open Questions

> 已在 Section Authoring 过程中收敛的问题（v1.0 已决）：
> - ✅ Q1: 朦胧化 UI 是否提供数值化模式 → **是**（默认关闭，可开启，Section 4.3）
> - ✅ Q2: Burst+Read 是否需要 QTE 替代 → **可能存在 QTE 时必须提供"自动通过"或"窗口 ×3"**（Section 3.5）
> - ✅ Q3: Steam Deck 触控板支持 → **默认作为鼠标光标替代，可关闭**（Section 3.2）
> - ✅ Q4: UI 整体缩放 → **不实现，用 720p/1080p 双布局**（Section 2.4）
> - ✅ Q5: 色盲模式 → **v1.0 不实现，用强制双轨编码**（Section 2.3）
> - ✅ Q6: 难度调整 → **锁定到存档**（Section 4.5）
> - ✅ Q7: Tier 选择 → **Standard (WCAG 2.1 AA)**（Section 1）

### 待 Pre-Production 阶段决议

- [ ] **Q8**: 字幕字体是否单独区分（思源宋体 vs 思源黑体）？还是与 UI 字体保持一致？
- [ ] **Q9**: 江湖见闻录的 UI 入口位置 —— 主菜单、暂停菜单、还是右下浮动按钮？
- [ ] **Q10**: 数值化模式开关是否在新建存档时引导玩家（"是否需要数值显示？"），还是仅在设置中提供？
- [ ] **Q11**: Action Glyphs 离线缓存策略 —— 内置默认贴图覆盖 PS/Xbox/Switch/Generic 4 套？还是仅默认 Xbox 一套？
- [ ] **Q12**: 第三方审计预算 —— 1-3w 还是 3-5w 区间？影响审计深度
- [ ] **Q13**: 玩家反馈邮箱白名单是 accessibility@项目域名 还是统一 support@ 加 tag？

### 待 Production 阶段决议

- [ ] **Q14**: v1.5 色盲模式是否值得投入（基于 v1.0 玩家反馈数据决定）
- [ ] **Q15**: 中期审计 vs Beta 审计是否合并（取决于预算与时间窗）

---

**End of Document v1.0**

> 本文档完成 v1.0 全章节填充，进入 stakeholder review 阶段。
> 通过 review 后将作为所有 UX spec、HUD design、Feature 实现的契约约束。

