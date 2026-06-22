---
title: Art Bible
project: 风止 (Wind Stops)
status: Draft
version: 1.0
author: Art Director
created: 2026-06-09
last_updated: 2026-06-11
engine: Godot 4.7-stable
render_pipeline: 2D Canvas + TileMapLayer + CanvasModulate
target_platforms: PC (Steam), Steam Deck
---

# Art Bible: 风止 (Wind Stops)

## Document Status
- **Version**: 1.0
- **Last Updated**: 2026-06-11
- **Owned By**: Art Director
- **Status**: Draft

---

## Visual Identity Summary

风止的视觉语言是「动态水墨」——取传统东亚水墨画的留白、渲染与意境，注入游戏化的节奏感和可读性。画面在静时如卷轴展阅，动时如墨点溅落。世界不追求照片写实，而追求「笔意」——每一帧都像一幅有意为之的构图。

核心美学三词：**留白 · 气韵 · 对比**

---

## Reference Board

| Reference | Medium | What We're Taking |
|-----------|--------|-------------------|
| 《大神 (Ōkami)》 | Game | 水墨渲染 Shader 风格、笔触式粒子特效、环境与角色的色彩分层 |
| 《大侠立志传》 | Game | 纯 2D 武侠表现、Q 版 sprite 探索 + 立绘对话双轨、2D 战棋格的清晰可读性、自由江湖氛围 |
| 《十三机兵防卫圈》 | Game | 叙事驱动 UI、极简 HUD 在情绪场景中的消隐方式 |
| 《只狼》 | Game | 武侠动作的动势捕捉、环境氛围光影、危险感传达 |
| 《Hades》 | Game | Contextual HUD、浓烈色彩与暗背景的对比、角色肖像画风格 |
| 张大千 泼墨山水 | Art | 大面积色块渲染、留白构图、翠蓝/赭石配色 |
| 王希孟《千里江山图》 | Art | 青绿山水色调、远近层次感、云雾意境 |
| 《英雄 (Hero, 2002)》 | Film | 色彩叙事（每段故事一个主色调）、服装与环境呼应 |
| 《刺客聂隐娘》 | Film | 缓慢镜头中的自然光、极简构图、衣料材质感 |

---

## Color Palette

### Primary Palette

| Name | Hex | Usage |
|------|-----|-------|
| 墨黑 (Ink Black) | #1A1A2E | 主背景、文字、UI 面板底色 |
| 宣纸白 (Rice Paper) | #F5F0E8 | 正文文字、高亮区域、留白 |
| 翠生 (Living Jade) | #4A9E7A | HP 满、安全状态、自然环境 |
| 枯赭 (Withered Ochre) | #8B4513 | HP 低危、损伤状态、衰败环境 |
| 青山蓝 (Mountain Blue) | #3A5B8C | 内力资源、宁静场景、水域 |
| 朱砂红 (Cinnabar Red) | #C73E3E | 危险警示、暴击、Destructive 操作 |
| 金箔 (Gold Leaf) | #D4A843 | Buff 效果、奖励、高品质物品 |
| 紫云 (Purple Cloud) | #6B4E8B | Debuff 效果、神秘元素、内力伤害 |
| 烟灰 (Smoke Grey) | #6B7B8D | 不可用态、辅助文字、次要信息 |
| 玉白 (Jade White) | #E8E4DC | UI 高亮边框、焦点框、分隔线 |

### Emotional Color Mapping

| Game State | Dominant Colors | Mood |
|-----------|----------------|------|
| 探索 (自然) | 翠生 + 青山蓝 + 宣纸白大面积留白 | 宁静、自由、呼吸感 |
| 探索 (城镇) | 暖赭 + 金箔点缀 + 烟灰 | 烟火气、安全、人情味 |
| 战斗 (观 / Stance) | 墨黑加深 + 青山蓝冷调 | 紧张、观察、蓄势 |
| 战斗 (动 / Strike) | 朱砂红 + 金箔 + 高对比度 | 爆发、果断、激烈 |
| 演出/叙事 | 单一主色调 + 大面积黑/白 | 情绪聚焦、戏剧张力 |
| 危险/低 HP | 枯赭渐染 + 画面边缘暗角 | 压迫、紧迫、求生 |
| 心境 — 正面 | 翠生偏暖 + 柔光 | 温暖、亲密 |
| 心境 — 负面 | 青山蓝偏冷 + 紫云暗涌 | 疏离、不安 |

### Color Usage Rules

1. **从不单独用颜色传达信息** — 所有颜色含义必须有图标/形状/文字辅助（WCAG 双轨编码）
2. **暖色=行动方，冷色=观察方** — 贯穿战斗/UI/叙事
3. **饱和度随重要性递增** — 背景低饱和，交互元素中饱和，焦点/警示高饱和
4. **一屏内主色不超过 3 种** — 保持水墨画的克制感

---

## Art Style

### Rendering Style

**纯 2D 武侠战棋 + 立绘 / Sprite 双轨制 (Pure 2D Wuxia, Daxia-Lizhi-Zhuan Style)**

- 视觉目标参考《大侠立志传》式的纯 2D 武侠呈现：场景由手绘 tileset + 分层 2D 背景构成，角色为 Q 版 sprite + 高头身立绘双轨；不追求伪 2.5D / HD-2D / 任何 3D 化构图（详见 [ADR-0020](../../docs/architecture/adr-0020-pure-2d-wuxia-rendering-direction.md)）。
- 场景使用 Godot 2D Canvas、TileMapLayer 多层结构、CanvasModulate 整体色调；**不**使用动态 2D 光照（PointLight2D / DirectionalLight2D）作为常规氛围手段；**不**使用 Forward+ 3D / PBR 管线。
- 角色双轨：探索 / 战斗使用 Q 版 sprite（3–4 头身），对话 / 关键演出使用高头身立绘（6.5–7.5 头身）+ 多表情切换；两者不混用。
- 空间深度通过**美术构图**表现（透视消失点 + 大小关系 + 色调递减 + 远近 tileset 分层），**不**通过多层视差或动态光照表现；阴影由 tileset 直接画进贴图。
- 远景雾、云、雪、落叶使用循环滚动的 2D sprite 或 GPUParticles2D，避免任何"伪 3D 体积感"。
- 所有战斗场景必须先满足格子、移动范围、攻击范围、角色朝向、遮挡关系的可读性，再叠加美术氛围。

### Proportions

| Category | Proportion | Notes |
|----------|-----------|-------|
| 角色战斗小人 | 3-4 头身 | 保证战棋视角下武器、朝向、身份可读 |
| 角色立绘 | 6.5-7.5 头身 | 用于对话/角色面板，可更写实修长 |
| 环境尺度 | 战棋格优先 | 建筑/山川按可通行性、遮挡与构图服务玩法 |
| UI 元素 | 与屏幕比例关系 | 参见 HUD §3 Zone 尺寸定义 |
| 武器 | 略夸张 (1.1-1.2x) | 保证远景可辨识 |

### Level of Detail

| Distance | Detail Level | Technique |
|----------|-------------|-----------|
| 战斗格内角色 | 高 — 朝向、阵营、武器、状态可读 | 2D Sprite + 描边/投影 |
| 当前交互层 | 中高 — 可通行、可交互、可遮挡清楚 | TileMapLayer + scene tile |
| 背景层 | 中低 — 氛围、区域识别、空间深度 | 分层 2D 背景（无视差） |
| 极远景 | 意象 — 水墨远山/云雾 | 2D 纹理层/Shader |

### Visual Hierarchy (引导视线优先级)

1. **角色/NPC** — 最高对比度，暖色/亮色服装，描边最粗
2. **可交互对象** — Context Prompt + 微发光边缘
3. **路径/引导** — 自然光线、留白空间、环境色差异
4. **背景/氛围** — 低饱和、低对比、淡化处理

---

## Character Art Standards

### Silhouette Requirements

- 每个角色在纯黑剪影下必须可辨识（无需颜色/细节）
- 武器类型通过剪影区分：剑=细直、刀=弯弧、拳=无持物轮廓
- 关键 NPC 头部/头饰必须独特（辨认核心）

### Color Coding

| Category | 规则 |
|----------|------|
| 主角 | 白/浅色为主 + 一处标志色点缀 |
| 盟友 NPC | 暖色系（赭/金/暖灰） |
| 敌方 | 冷色系（暗紫/冷灰/墨黑）— 但避免"善恶=暖冷"的刻板映射，通过造型/姿态而非纯颜色传达 |
| Boss | 独立主题色（每个 Boss 一个标志色） |

### Animation Style

- 关键帧动画为主（非动捕），保留手绘感的"一拍二"节奏
- 武术动作参考传统武术形态（非现代格斗）
- Idle 动画需体现角色性格（文人=缓慢摇扇、侠客=手扶剑柄警觉）
- 攻击动画注重"蓄力→爆发→收式"三段式

### Portrait Art

- 半身像，背景透明
- 水墨画风格（可见笔触、墨色浓淡）
- 用于对话 UI、角色面板
- 表情变体：中立 / 喜 / 怒 / 哀 / 惊（最低 5 套）

### 立绘 / Sprite 双轨对齐流程 (ADR-0020 §D5)

每个角色按用途分两套资产生产，**不混用**：

| 用途 | 资产 | 头身比 | 表现 |
|------|------|--------|------|
| 对话 / 关键剧情 / UI 头像 | 立绘 + 多表情 | 6.5–7.5 | 高细节、水墨风手绘、表情切换 ≥5 套（中立/喜/怒/哀/惊）|
| 场景探索 / 战棋战斗 | Sprite / Sprite Sheet | 3–4 | Q 版、朝向 + 武器剪影清晰 |

**对齐流程（防止同一角色两套设计跑偏）**：

1. **角色设定单先行**：每个角色出一份「设定单」，确定核心识别要素——发型 / 标志色 / 武器剪影 / 服饰主元素 / 性格姿态关键词。设定单是两套资产的共同源。
2. **立绘先做，sprite 后做**：以立绘为锚定基准，sprite 必须能在 3–4 头身下保留设定单的所有核心识别要素。
3. **跨轨审查**：sprite 完成后与立绘并排 audit，确认远景 sprite 与近景立绘视觉上是「同一个人」。
4. **不允许场景出现立绘比例角色**（违反 ADR-0020 §D5）；**不允许立绘镜头出现 Q 版角色**（同上）。
5. 设定单变更必须双轨同步更新；任何一轨先于另一轨变更视为漂移，需在下一次 audit 修复。

---

## Environment Art Standards

### World Building Principles

1. **每个场景一个"画眼"** — 构图焦点（如远山/古桥/老树），引导玩家视线
2. **留白即信息** — 空旷不代表空洞，留白暗示自由或孤寂
3. **垂直层次** — 前/中/远三层必须色调区分（暗→中→淡）
4. **季节/天气影响配色** — 不同区域使用不同季节基调

### Tileset & Modularity

| Category | Grid | 变体要求 | Notes |
|----------|------|---------|-------|
| 地面 | 1 战棋格 | ≥4 变体避免重复感 | 接缝需无缝 |
| 墙体 | 1 格宽，多格高视觉 | ≥3 损坏等级 | 支持 autotile / 遮挡层 |
| 屋顶 | 自由形状 | 按建筑类型 | 位于 Overlay 层，可遮挡角色 |
| 自然物 | 非网格 | 随机旋转/缩放 | 树/石/草 |

### Lighting

| Scene Type | 主光源 | 色温 | 阴影 |
|-----------|--------|------|------|
| 白天户外 | CanvasModulate + 环境高光贴图 | 5500K-6500K | 贴图阴影/软投影 |
| 黄昏 | CanvasModulate 暖色偏移 | 3000K-4000K | 长影贴图，暖色 |
| 室内 | CanvasModulate 暖色偏移 + 灯笼/烛火贴图烘焙阴影 | 2700K-3500K | 贴图明暗对比 |
| 战斗场景 | 环境色 + 角色/范围高亮 | 偏冷或剧情色 | 高对比聚焦角色 |

> **注**: 本项目按 [ADR-0020](../../docs/architecture/adr-0020-pure-2d-wuxia-rendering-direction.md) §D2，不使用 `PointLight2D` / `DirectionalLight2D` 作为常规光源；动态 2D 光照仅在特例 VFX（如重要演出灯笼特写、洞窟火把）经 PR 评审引入。章节色调演变通过分层背景 + CanvasModulate 整体调色实现。

### Atmospheric Effects

- **雾/云** — 2D 分层雾贴图/Shader，远景必须有层次雾
- **粒子** — 落叶/飘雪/萤火虫/尘埃，用于暗示季节和情绪
- **水面** — Shader 驱动涟漪 + 倒影（简化反射）
- **墨迹** — 战斗中特效使用墨迹飞溅纹理（非写实血液）

---

## UI Art Standards

### Design Language

UI 是"宣纸上的墨迹"——半透明底板模拟纸张质感，文字如墨书，按钮如印章。

### Typography

| Level | 用途 | 字体风格 | 大小 (1080p) | 颜色 |
|-------|------|---------|-------------|------|
| H1 (标题) | 画面标题、章节名 | 书法体 (毛笔字) | 36-48sp | 墨黑 or 宣纸白 |
| H2 (副标题) | 区域标题、面板名 | 楷体 (Regular) | 24-28sp | 墨黑 |
| Body (正文) | 对话、描述、按钮 | 黑体/宋体 (Regular) | 18-20sp | 宣纸白 / 墨黑 |
| Caption (辅助) | Tooltip、时间戳 | 黑体 (Light) | 14-16sp | 烟灰 |
| Number (数值) | HP/伤害/倒计时 | 等宽数字体 | 20-32sp | 按语义色 |

**规则**：
- 正文最小 18sp（Steam Deck 720p 下等效约 12px 物理像素，满足可读性）
- 行高 1.5x 字号
- 中文排版无需 kerning，注意标点避头尾

### Button Style

| Type | 背景 | 边框 | 文字 | 示例 |
|------|------|------|------|------|
| Primary | 翠生 20% alpha | 玉白 1px | 宣纸白 | "确认"、"装备" |
| Secondary | 透明 | 玉白 1px | 玉白 | "取消"、"返回" |
| Destructive | 朱砂红 15% alpha | 朱砂红 1px | 宣纸白 | "丢弃"、"退出" |
| Disabled | 烟灰 10% alpha | 烟灰 1px 虚线 | 烟灰 | 不可用操作 |
| Focused | 当前态 + 外发光 (玉白 2px) | — | — | 焦点态叠加 |

### Icon Style

- **线条图标** — 1.5px 描边，圆角端点
- **单色** — 跟随所在语境色（白底用墨黑，暗底用宣纸白）
- **尺寸标准** — 24×24 dp (小) / 32×32 dp (中) / 48×48 dp (大)
- **状态效果图标** — 允许双色（图标色 + 边框色形成双轨）
- **所有图标必须有文字 alt** — 参见 accessibility-requirements.md

### Panel & Container Style

- 半透明暗色底板：墨黑 60-80% alpha
- 可选纸张纹理叠加（Multiply 模式，5-10% 强度）
- 圆角 4dp（非锐角，但不过度圆润）
- 边框可选：玉白 1px，用于区分层次

### HUD Visual Language

参见 [design/ux/hud.md](../ux/hud.md) — HUD 特有规则：
- 朦胧化模式下 HUD 使用更低 alpha（融入画面）
- 数值化模式下 HUD 使用标准 UI alpha（清晰可读）
- HP 条配色遵循"翠→枯"语义渐变
- 阶段指示器"观/动"使用书法体单字

---

## VFX Standards

### Particle Style

| Category | 形态 | 颜色 | 混合模式 | Notes |
|----------|------|------|---------|-------|
| 环境粒子 | 落叶/尘埃/萤火 | 低饱和自然色 | Alpha Blend | 缓慢，不引人注目 |
| 攻击特效 | 墨迹飞溅/剑气弧线 | 白+主题色 | Additive | 快速，1-3 帧关键帧 |
| 治疗/Buff | 光点上升/金色涟漪 | 金箔/翠生 | Additive | 柔和，持续 |
| 伤害/Debuff | 暗色碎片/紫雾 | 紫云/枯赭 | Alpha Blend | 短促，有压迫感 |
| 心境变化 | 墨色翻涌/光芒扩散 | 按心境正负色 | Screen | 中速 (Slow 400ms) |

### Screen Effects

| Effect | 触发 | 表现 | 持续 | Reduce Motion |
|--------|------|------|------|--------------|
| 画面震动 | 受击/暴击 | XY 偏移 ±3-8px | 200-400ms | 边框闪白替代 |
| 暗角加深 | 低 HP | 边缘暗角从 0→40% | 渐变 (Slow) | 仅颜色变化 |
| 速度线 | Burst 开始 | 径向线条 overlay | Fast 100ms | 跳过 |
| 墨渍过渡 | 场景切换 | 墨点从中心扩散覆盖 | Slow 400ms | 直接黑屏 |
| 色调偏移 | 心境极端化 | 整体色温偏暖/冷 | 渐变 (Cinematic) | 保留（非运动） |

### Impact Feedback Hierarchy

1. **轻击** — 微震 + 小墨点 + 轻音效
2. **重击** — 中震 + 大墨迹弧线 + 短暂顿帧 (50ms)
3. **暴击** — 强震 + 全屏闪白 (1帧) + 墨迹爆裂 + 慢镜顿帧 (100ms)
4. **Boss 终结** — 3 级特效 + 画面定格 + 特写镜头

---

## Asset Production Standards

### Naming Convention

```
[category]_[subcategory]_[name]_[variant].[ext]

Examples:
char_protagonist_idle_01.tres
env_mountain_rock_large_mossy.tscn
ui_icon_sword_common.svg
vfx_slash_horizontal_fire.tres
sfx_ui_confirm_01.ogg
```

### Texture Standards

| Category | Max Resolution | Format | Color Space | Compression |
|----------|---------------|--------|-------------|-------------|
| Characters | 1024×1024 per sheet | PNG (source) → .ctex | sRGB | VRAM Compressed (S3TC) |
| Environments | 2048×2048 per atlas | PNG → .ctex | sRGB | VRAM Compressed |
| UI | 512×512 (max per atlas) | SVG (vector preferred) / PNG | sRGB | Lossless |
| VFX | 512×512 | PNG → .ctex | sRGB | VRAM Compressed |
| Background | 4096×2048 max | PNG | sRGB | VRAM |

### Animation Standards

| Category | Frame Rate | Blend Time | Notes |
|----------|-----------|-----------|-------|
| 角色移动 | 12-24 FPS | 0.1-0.2s | Sprite sheet / AnimationPlayer |
| 攻击动作 | 30 FPS | 0.05s (快切入) | 注重首帧力度感 |
| UI 动画 | 60 FPS (Tween) | — | 参见 interaction-patterns §6 |
| 环境动画 | 15-30 FPS | — | 风吹草/水流等可降帧率 |
| 粒子 | 引擎 Process | — | GPUParticles2D |

### Audio Standards (简要，详见 interaction-patterns §7)

| Category | Format | Sample Rate | Channels | Max File Size |
|----------|--------|-------------|----------|---------------|
| SFX | .ogg (Vorbis) | 44.1kHz | Mono | 200KB |
| 环境音 | .ogg | 44.1kHz | Stereo | 1MB |
| BGM | .ogg | 44.1kHz | Stereo | 8MB |
| 语音 (未来) | .ogg | 22.05kHz | Mono | — |

---

## Accessibility (Visual)

### Mandatory Rules

1. **双轨编码** — 所有用颜色传达的信息必须同时有图标/形状/文字（§Color Usage Rules #1）
2. **最小文本** — 正文 ≥18sp / 辅助文字 ≥14sp (1080p 基准)
3. **对比度** — 正文 ≥4.5:1 / 大文本(≥24sp) ≥3:1 / 交互元素 ≥3:1 (WCAG 2.1 AA)
4. **焦点可见性** — 焦点框使用玉白外发光 (2px, 对比度 ≥3:1 与背景)
5. **动画可关闭** — 所有非信息性动画在 Reduce Motion 下降级或移除
6. **不使用闪烁** — 任何元素频率不得超过 3Hz（光敏性癫痫安全）

### 数值化模式视觉规范

- 数值叠加层使用等宽数字字体 + 半透明底色标签
- 不改变底层视觉元素（叠加而非替换）
- 数值颜色跟随语义色（HP=翠/枯，内力=青山蓝）

### Steam Deck Adaptation

- 720p 下字号不低于 14sp（物理约 9.3px，密度补偿后可读）
- 触摸目标 ≥48dp
- UI 元素保持绝对尺寸（Anchor+Margin），不按分辨率缩小

---

## Quality Checklist (Per Asset)

- [ ] 遵循命名规范
- [ ] 尺寸在规定预算内
- [ ] 无透明度问题 (premultiplied alpha)
- [ ] 在最低目标分辨率 (720p) 下可辨识
- [ ] 在最高目标分辨率 (4K) 下无明显像素化
- [ ] 双轨编码验证（如适用）
- [ ] Reduce Motion 替代方案已定义（如适用）

---

## Audit History

| 日期 | 版本 | 审计人 | 变更 |
|------|------|--------|------|
| 2026-06-09 | 1.0 | AI (art-director) | 全文创建 |
| 2026-06-11 | 1.1 | Codex | 明确视觉目标参考《逸剑风云决》的像素与场景融合观感；移除主 3D/PBR 与《歧路旅人》式 HD-2D 完整标准假设 |
| 2026-06-22 | 1.2 | Cursor | 视觉参考目标变更为《大侠立志传》纯 2D 路线（详见 [ADR-0020](../../docs/architecture/adr-0020-pure-2d-wuxia-rendering-direction.md)）：Reference Board 替换《逸剑风云决》→《大侠立志传》；渲染风格段移除伪 2.5D / 多层视差 / 动态 2D 光照表述；LOD 背景层移除"视差"；明确立绘 / Sprite 双轨制 |
