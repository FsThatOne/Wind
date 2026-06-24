# Dialogue Compiler

将 `.dlg` 纯文本对话脚本编译为 DialogueSchema 兼容的 YAML 文件。

## 快速开始

```bash
python3 tools/dialogue_compiler.py my_dialogue.dlg -o feng-zhi/assets/data/dialogues/chapter_00/my_dialogue.yaml
```

省略 `-o` 则输出到终端（方便预览）。

## .dlg 语法

### 基本节点

```
# dialogue_id

旁白：环境描写或场景叙述。
内心：主角的内心独白。
对白（师姐）：师姐说的话。
```

- `旁白：` → `type: narration`
- `内心：` → `type: inner_monologue`
- `对白（角色名）：` → `type: speech`，自动填充 `speaker` 字段

节点按书写顺序自动串联，最后一个节点 `next: END`。

### 选择分支

```
选择：你望着桌上的茶杯——
  - 端起茶杯喝一口。
    旁白：温热的茶水滑过喉咙。
  - 放下茶杯转身离开。
    内心：算了，不渴。
```

- `选择：` 后跟提示文字
- `- 选项文字` 缩进 2 格
- 选项下方再缩进写后续节点（支持多个）
- 每个分支末尾自动 `next: END`

### 事件指令

用 `>` 标记事件，附加到上一个节点或选项：

```
旁白：你翻看了架上的药材。
> flag shelf_examined true
> mindset resolve -1
```

| 指令 | 格式 | 生成的 YAML |
|------|------|-------------|
| 设置标记 | `> flag key value` | `type: quest_flag` |
| 心境偏移 | `> mindset axis delta` | `type: mindset_shift` |

心境轴（axis）可选值：`resolve`、`worldly`、`morality`。

在选择分支内使用时，事件附加到该选项：

```
选择：如何处置？
  - 收下银两。
    > mindset worldly 1
  - 拒绝好意。
    > mindset resolve 1
```

### 日/夜条件

用 `[日]` 或 `[夜]` 前缀标记条件节点：

```
[日] 旁白：阳光从洞口照进来，一切看得清清楚楚。
[夜] 旁白：借着油灯微光，洞壁上的影子摇曳不定。
```

- `[日]` 节点自动添加 `conditions: [{source: flag, key: variant, op: eq, value: day}]`
- `[夜]` 节点自动成为上一个 `[日]` 节点的 `fallback`
- 两者的 `next` 都指向后续的下一个普通节点（互斥分支）

### 完整示例

```
# wine_pickup_01

旁白：角落里三坛酒静静立着，封泥上的墨迹已经干透。
内心：师姐说，好酒要等。等得住的人，才配喝第一口。

选择：你伸手去取酒坛——
  - 先端详封泥上的字迹。
    旁白：封泥上歪歪扭扭写着——庄主六十大寿，此坛留饮。
    > flag wine_examined true
  - 直接抱起酒坛，准备带走。
    内心：酒坛沉甸甸的，抱在怀里像是抱住了三年的光阴。
    > mindset resolve -1

[日] 内心：日光从洞口斜射进来，照在封泥上的墨迹格外清晰。
[夜] 内心：借着微弱的灯火，封泥上的字迹若隐若现。

旁白：你将酒坛小心抱起，向洞口走去。
```

## 注意事项

- 文本中避免使用中文引号 `""`，用破折号 `——` 代替
- `：` 和 `:` 均可作为节点类型分隔符
- `（）` 和 `()` 均可用于对白角色名
- 空行和 `---` 会被忽略，可用于视觉分组
- 生成的 YAML 可直接放入 `feng-zhi/assets/data/dialogues/` 目录使用
