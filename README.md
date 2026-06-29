# 《风止》 — FengZhi

> 2D 像素武侠 RPG · 行气战棋 · 叙事驱动 · 6 结局 / 16 终幕脚本

你是风止山庄的最后弟子。一夜之间师门尽灭，你披白衣下山，沿着三个仇人的踪迹一路追查 ——
江南烟雨、塞北风雪、戈壁孤城。但越追越发现，这不是一桩简单的师门血案：
有人在用整个江湖做局，而你的师门，不过是开局的一颗弃子。

---

## 项目概览

| 维度 | 值 |
|---|---|
| **类型** | 2D 像素武侠 RPG / 叙事驱动 / 行气战棋 |
| **平台** | PC (Steam) v1.0；后续考虑 Switch / 移动端 |
| **引擎** | Godot **4.7-stable** + C# (.NET 8+) |
| **本体目录** | `feng-zhi/`（Godot 工程，应用名 `FengZhi`） |
| **主场景** | `feng-zhi/StartCave.tscn` — 起始山洞 |
| **预估周期** | 8–14 个月（个人 + AI 协作） |
| **货币化** | 买断制 |
| **当前阶段** | Production（lean review） |
| **设计文档入口** | [`design/gdd/game-concept.md`](design/gdd/game-concept.md) |

更详细的核心玩法、剧情背景与设计哲学，见 [`design/gdd/game-concept.md`](design/gdd/game-concept.md) 与
[`design/gdd/systems-index.md`](design/gdd/systems-index.md)。

---

## 目录结构

```text
feng-zhi/                    Godot 4.7 主工程（场景、脚本、addons、资产引用）
src/                         C# 业务源码（Foundation / Combat / Map / UI 等）
  └── FengZhi.Foundation/    引擎无关的领域层（Port/Adapter 边界）
tests/                       NUnit 测试套件，按模块镜像 src/
assets/                      原始资产（sprite 工作目录、生成产物、音频、shader）
design/                      GDD、UX、艺术圣经、可访问性需求
  ├── gdd/                   设计文档（systems-index 为入口）
  ├── art/art-bible.md       视觉规范 + ADR-0020 双轨流程
  └── ux/                    HUD / 输入 / 反馈设计
docs/architecture/           架构文档：master architecture、ADR、追溯索引
  └── adr-*.md               21 份架构决策（最新 ADR-0021: Character Animation Port）
production/                  Sprint / 里程碑 / Gate / QA 证据 / 会话状态
  ├── stage.txt              当前阶段（Production）
  ├── review-mode.txt        评审强度（lean）
  ├── sprints/               Sprint 计划
  ├── gate-checks/           阶段门检记录
  └── qa/evidence/           QA 证据链
.claude/ .codex/ .agents/    AI 协作配置（agents / skills / hooks / rules）
```

---

## 开发环境

### 必备

- **Godot 4.7-stable**（C# build） — [下载](https://godotengine.org/download/)，详见
  [`docs/engine-reference/godot/VERSION.md`](docs/engine-reference/godot/VERSION.md)
- **.NET SDK 8.0+** — `dotnet --version` 应 ≥ 8.0
- **Git** — trunk-based 开发，工作主干 `develop`，发布主干 `master`

### 推荐

- `jq` — Git hook 校验依赖
- Python 3 — JSON 校验 / 资产管线脚本

### 启动

```bash
dotnet restore                                       # 还原 .NET 依赖
scripts/dev/launch-godot.sh                          # 在编辑器中打开 feng-zhi 主工程
scripts/dev/launch-godot.sh feng-zhi --run           # 直接运行（替代 F5）
scripts/dev/launch-godot.sh path/to/project          # 任意 project.godot 目录
scripts/dev/launch-godot.sh --list                   # 查看已知 project 别名
```

> macOS 上请走 `scripts/dev/launch-godot.sh`（兼容 `Godot_mono.app`，绕开 mcp_godot v0.1.1 的 LaunchServices 启动 bug）。
> 其它平台或自定义 Godot 路径：`GODOT_BIN=/path/to/Godot.app scripts/dev/launch-godot.sh ...`。

### 测试

```bash
dotnet test                                  # 全部 NUnit 测试
dotnet test --filter Category=Foundation     # 仅 Foundation 层
```

---

## 关键架构决策

| ID | 主题 | 状态 |
|---|---|---|
| ADR-0002 | 双焦点（光标 + 选中）输入模型 | Accepted |
| ADR-0019 | 2D 武侠战棋渲染方向 | **Superseded by 0020** |
| ADR-0020 | 纯 2D 武侠渲染方向（PointLight2D 不作常规光源） | Accepted |
| ADR-0021 | 角色动画 Port（`ICharacterAnimator`，隔离 AnimatedSprite2D 等后端） | Accepted |

完整索引：[`docs/architecture/adr-INDEX.md`](docs/architecture/adr-INDEX.md) ·
追溯矩阵：[`docs/architecture/traceability-index.md`](docs/architecture/traceability-index.md) ·
最新评审：[`docs/architecture/architecture-review-2026-06-22.md`](docs/architecture/architecture-review-2026-06-22.md)

---

## 战斗模型：行气战棋

回合分两个阶段：

- **观 (Stance)** — 观气、取位、读破绽；决策窗口，零节奏压力
- **动 (Strike)** — 出招、连招、决胜瞬间；旧称 "Burst"，已淘汰该词汇

详见 [`design/gdd/systems/combat-system.md`](design/gdd/systems/combat-system.md) 与
[`design/ux/hud.md`](design/ux/hud.md)。

---

## 音频系统

遵循 ADR-0009 分层架构：BGM（背景音乐）/ Ambient（环境音）/ SFX（音效）三轨独立。Autoload 入口为 `AudioDirector`。

### BGM vs Ambient 区别

| 维度 | BGM（背景音乐） | Ambient（环境音） |
|---|---|---|
| 本质 | 有旋律的音乐（古琴、笛、箫） | 无旋律的环境声（雨声、风声、蝉鸣） |
| 功能 | 传达情绪、标识状态（探索/战斗/演出） | 构建空间沉浸感（"闭眼就知道在哪"） |
| 并发数 | 任意时刻 1 首主播放（crossfade 短暂重叠） | 最多 3 层同时叠加（地形+天气+时辰） |
| 切换方式 | crossfade（等功率曲线，800ms/1500ms） | 各层独立淡入淡出（2000ms），不互斥 |
| Override 栈 | 有（最多3层：探索→战斗→演出，Pop 恢复） | 无（不进栈，独立运行） |
| 战斗时 | 切到战斗自适应 BGM（6段水平分层） | 整体压到 20% 音量（不消失） |
| 留白 | 可为 SILENCE（刻意无音乐） | 始终运行（即使 BGM 静音，环境音也在） |
| 典型例子 | 江南主旋律、战斗紧张曲、灭门悲伤曲 | 竹林风声、小雨声、午时蝉鸣、夜晚虫鸣 |

### 场景 BGM / 环境音配置（零代码）

每个场景在 Godot 编辑器中通过 Inspector 配置即可自动切换音频：

1. 选中场景根节点（TMX 场景继承 `SceneGameBase`，IsoRoom 场景如炼丹房使用各自脚本）
2. 在 Inspector 中找到以下 Export 属性：
   - **Scene Bgm Id**：BGM 文件名（不含路径和扩展名），对应 `res://assets/audio/bgm/{id}.ogg`
   - **Terrain Ambient Id**：地形环境音文件名，对应 `res://assets/audio/ambient/{id}.ogg`
3. 填写后保存场景，进入该场景时 BGM/环境音会自动 crossfade 切换

**特殊值**：
- `Scene Bgm Id = "SILENCE"`：刻意留白，当前 BGM 淡出至静音
- `Scene Bgm Id` 留空：不切换 BGM，上一场景的 BGM 继续播放（适用于同区域子场景，如室内/室外共用同一首 BGM）
- `Terrain Ambient Id` 留空：不切换地形环境音，上一场景的环境音继续播放

**切换流程**：
```
SceneTransitionManager.TransitionTo() → 画面淡黑 → 切场景
→ 新场景 _Ready() → ApplySceneAudio(bgmId, terrainId)
→ BgmManager: 同曲续播检测 → 若不同则 1500ms 淡出旧 + 800ms 淡入新
→ AmbientManager: 地形层 2000ms 淡入淡出；天气/时辰层保持不变
→ 画面淡入，新 BGM 在玩家看到场景的同时淡入中
```

### 代码 API（高级场景/战斗/演出）

```csharp
var audio = GetNode<AudioDirector>("/root/AudioDirector");

// 场景进入（通常不需要手动调用，Export 属性自动处理）
audio.ApplySceneAudio("jiangnan_main", "ambient_bamboo_wind");

// 战斗/演出：Push/Pop Override 栈（最多3层嵌套）
audio.PushOverrideBgm("combat_intense", AudioState.Combat);   // 压入战斗 BGM
audio.CrossfadeCombatSegment("combat_climax", 500, 300);     // 战斗内段落切换（小节线对齐后）
audio.PopOverrideBgm();                                       // 弹出栈顶，恢复前一首 BGM

// Motif overlay（女主主旋律叠加层，不进入 Override 栈，不替换当前 BGM）
audio.PlayMotif("heroine");
audio.StopMotif();

// 天气/时辰环境音（由天气/时辰系统在状态变化时调用）
audio.SetWeatherAmbient("ambient_rain");      // 传 null 停掉天气层
audio.SetTimeOfDayAmbient("ambient_cicadas"); // 传 null 停掉时辰层

// 状态触发（对话/菜单/过场自动压低 BGM 音量）
audio.Trigger(AudioTriggers.OpenMenu);
audio.Trigger(AudioTriggers.EndDialogue);
```

### 资源路径约定

```
feng-zhi/assets/audio/
├── bgm/          # BGM 文件（无缝循环）：jiangnan_main.ogg/.mp3、combat_intense.ogg/.mp3 等
├── ambient/      # 环境音文件（无缝循环）：ambient_bamboo_wind.ogg/.mp3、ambient_rain.ogg/.mp3 等
└── sfx/          # 音效文件（短音效，无需循环）：footstep.ogg/.mp3、sword_hit.ogg/.mp3 等
```

**支持格式**：`.ogg`（OGG Vorbis）和 `.mp3` 均可，优先加载 `.ogg`（无缝循环更干净、体积更小）。
- 开发阶段可直接使用 MP3，无需转码，放入目录即自动识别
- 正式发布前建议将循环 BGM/环境音转成 `.ogg`，避免 MP3 帧间隙导致的循环点微小爆音
- 同一 trackId 两种格式并存时优先使用 `.ogg`
- SFX（音效）一般较短不循环，两种格式无明显差异

音频文件需自行准备并放入对应目录，放入后即自动生效，无需额外配置。

---

## AI 协作工作流

项目在 Claude Code / Codex 双轨下运行，配置入口：

- [`CLAUDE.md`](CLAUDE.md) · [`AGENTS.md`](AGENTS.md) — 顶层 AI 协作约定
- `.claude/agents/` · `.codex/agents/` — Agent 定义（Directors / Leads / Specialists）
- `.claude/skills/` · `.codex/skills/` — `/start`、`/dev-story`、`/gate-check` 等 slash 命令
- `.claude/hooks/` — commit / push / asset / session 自动校验
- `.claude/rules/` — 路径作用域的编码规范（`src/gameplay/**`、`src/ui/**`、`design/gdd/**` 等）

**协作原则**：Question → Options → Decision → Draft → Approval。  
Agent 永远不会未授权写入文件 —— 每次落盘前都会先问 "May I write this to [path]?"。

---

## 当前状态（2026-06-22）

- **Stage**：Production
- **Review mode**：lean（仅 phase gate 触发完整 director panel）
- **最近 gate**：[Technical Setup → Pre-Production 回炉评审](production/gate-checks/gate-tech-setup-to-pre-production-2026-06-22.md) — `CONCERNS`
- **遗留 CONCERNS**：Vertical Slice 需按 ADR-0020 重做、`hud.md` 机制名语义重写、Feature 6 / Presentation 4 范围再切

---

## 贡献

私人项目。如果你只是路过想留言，欢迎在 Issue 中留下脚印；  
但本仓库 **不接受外部 PR**，因为剧情 / 玩法设计属于个人创作。

## 许可

代码：MIT（见 [LICENSE](LICENSE)）。  
设计文档、剧情设定、美术资产：保留全部权利，未经授权不得复制、改编、再分发。
