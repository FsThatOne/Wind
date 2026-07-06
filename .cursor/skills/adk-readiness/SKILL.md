---
name: adk-readiness
description: "评估项目的 AI 就绪度 / AI 友好度，自动识别技术栈并产出统一格式报告。当用户要求评估 AI 友好度、AI 就绪度、AI 准备度，或运行 readiness 检查时使用。"
---

# AI Readiness 统一入口

自动识别项目技术栈，调度对应评估器，产出统一格式报告到 `.ai-readiness/` 目录。

**定位：薄入口** — 只做「识别 + 调度 + 归置 + 入口页」，不做各端评估逻辑本身。
**详细适配说明分散在 `adapters/*.md` 文件中**，主 agent 按 adapter 类型选择性加载，不要一次性读全部。

---

## 用户输入

```text
$ARGUMENTS
```

先解析输入，再继续。

### 参数

- **路径（位置参数，可多个）**：要评估的模块路径；相对或绝对均可。不传则用当前目录（`[.]`）。
  - 单路径 → 单模块评估
  - 多路径 → 多模块依次评估，汇总到同一入口页
- `--quick` / `--deep`：评估深度，默认 `deep`
- `--stack <stack-id>`：跳过自动检测，直接指定技术栈（如 `backend-gdp` / `mobile-android`）
- `--evaluator <skill-name>`：直接指定评估 skill，优先级最高
- `--name <module-key>`：单模块时覆盖详情子目录名（默认取目标路径 basename）
- `--lang zh` / `--lang en`：报告语言，默认 `zh`
- `--repo-root <path>`：仓库根路径，用于 module-key 冲突时解析相对路径。默认自动推断
- `--output-root <path>`：**统一产出根目录**（最高优先级）。`.ai-readiness/` 会创建在该路径下。不传则按决策 12 规则自动选择（默认 repo 根）

### 输入示例

```
adk-readiness                                                    # 单模块（当前目录，自动检测）
adk-readiness ./feed-service --quick                             # 单模块，快速模式
adk-readiness ./feed-service --quick --output-root ./reports     # 自定义产出目录到 reports/ 下
adk-readiness ./feed-service --quick                             # 单模块，快速模式
adk-readiness ./user-svc --stack backend-gdp                     # 指定技术栈
adk-readiness apps/feed apps/profile libs/common                 # 多模块依次评估
adk-readiness ./android-app --evaluator tiktok-mobile-ai-friendliness  # 直接指定 evaluator
```

---

## 核心约定（执行前记住）

1. **所有产出放 `.ai-readiness/` 下**，不得写入目标项目的其他位置
2. **每个评估单元一个 module-key**，details 子目录永远是 `.ai-readiness/details/<module-key>/`
3. **module-key 默认 = 目标路径 basename**；多个模块 basename 冲突时，用「从 repo-root 起的相对路径，`/` 替换成 `__`」
4. **校验三件套**：`_evaluator_result.json` 存在可解析 + `reportPath` 文件存在 + 所有产出都在 outputDir 内
5. **契约字段宽松**：timestamp、score、level、summary 都可选，缺失则优雅降级（不展示或用默认值）
6. **尽力透传 language**：入口页用 `--lang`，evaluator 有 language 参数就传，不支持就算了

---

## 整体流程

```
1. 解析参数 → 目标路径列表 + 模式 + 栈 + evaluator + 语言 + 模块名
2. 确定 output_root（默认 repo 根，可被 --output-root 覆盖），统一产出目录 = `<output_root>/.ai-readiness/`
3. 对每个目标路径：
   a. 技术栈识别（被 --stack 或 --evaluator 跳过）
   b. 查配置 → evaluator skill 名 + adapter 类型 + 安装提示
   c. 尝试 Skill() 调用 evaluator + 按 adapter 类型做参数适配、产出写入 `<ai_readiness_dir>/details/<module-key>/`
   d. Skill() 不可用 → 给安装提示，询问：安装后继续 / fallback
4. 生成统一入口页 latest.md + latest.json
5. 归档历史报告
6. 向用户展示结果（含：输出位置、必要时场景化提示如何改写到子模块目录）
```

---

## Step 0：解析参数

### 0a. 提取参数

- 收集位置参数 → 目标路径列表；空则 `["."]`
- 每个路径解析为绝对路径，校验存在且是目录，不存在则报错
- 模式：`--quick` → `quick`，其余 → `deep`
- 提取 `--stack` / `--evaluator` / `--lang`（默认 `zh`）/ `--name`（仅单模块）/ `--repo-root`（自动推断兜底）

### 0b. 分配 module-key

对每个目标路径依次处理：
1. 初始 key = `os.path.basename(os.path.normpath(abs_path))`
2. 如果和前面的 key 冲突，替换为「从 repo-root 起的相对路径，`/` → `__`」
3. 单模块且用户传了 `--name`，用用户指定的值（校验：无空格、无特殊字符 `/\:*?"<>|`）

### 0c. 确定 skill 根目录

本 skill 的目录就是 `skill_root`。后续所有 `scripts/*.py` 都从这里执行。
先确认 `skill_root/scripts/stack_config.py` 存在。

### 0d. 确定 repo-root（用于 module-key 冲突时解析相对路径） & 0e. 确定 output-root（决策 12）

用辅助脚本一次性算出两个根目录（避免重复写 git / 公共祖先探测逻辑）：

```bash
python3 <skill_root>/scripts/detect_repo_root.py <abs_target_1> <abs_target_2> ... \
  [--output-root <user_explicit_output_root>]
```

解析 JSON 拿到：
- `repo_root`：用于 module-key 冲突时从 repo-root 取相对路径（可能为 null，此时用当前工作目录兜底）；**若用户显式传了 `--repo-root`，以用户传入的为准，覆盖脚本结果**
- `output_root`：`.ai-readiness/` 的父目录（显式 > git 根 > 公共祖先）；**若用户显式传了 `--output-root`，辅助脚本已经按它输出，直接用**
- `ai_readiness_dir`：`<output_root>/.ai-readiness`，所有模块共用
- `any_child_evaluation`：布尔值，Step 7 判断是否需要输出场景化提示（用户显式传了 `--output-root` 时也不需要提示 —— 用户已经知道自己放在哪）
---

## Step 1：技术栈识别

- 用户传了 `--evaluator`：**不做识别**。stack 留空，直接进入 Step 2（`find_evaluator --skill-name`），由它反查对应 stack。
- 用户传了 `--stack` 且没传 `--evaluator`：**不做识别**。直接用指定的 stack id。
- 其他情况（自动检测）：对每个目标路径分别执行：
  ```bash
  python3 <skill_root>/scripts/detect_stack.py <target_path>
  ```
  解析 JSON 拿到：`stack` / `confidence` / `adapter` / `display_name` / `evaluator` / `signals`。

**置信度处理**：
- high → 直接用
- medium → 告知用户「检测到 <display_name>（置信度中），是否确认？」。不确认则列出候选让用户选
- low → 列出候选技术栈让用户选择；用户不选或选「通用」则走 fallback

**多栈冲突（Monorepo）**：
如果检测到多个不同栈的 high-confidence 信号：
1. 告知用户检测到多技术栈
2. 列出每个栈的建议子目录
3. 询问：评估哪个子目录，还是都评估
4. 根据回答调整目标路径列表（单模块或加多个路径）

---

## Step 2：查 Evaluator 配置

**只查配置不扫文件**：根据栈或 evaluator 名，从共享配置取 evaluator skill 名、适配方式和安装提示。真正的 skill 发现与可用性判断交给 Relay 运行时 + agent 动态处理。

如果用户传了 `--evaluator`：
```bash
python3 <skill_root>/scripts/find_evaluator.py --skill-name <evaluator_name>
```
否则（有 stack id）：
```bash
python3 <skill_root>/scripts/find_evaluator.py <stack_id>
```

解析输出：
- `evaluator_skill`：目标 skill 名（可能为 null，表示没有评估器）
- `adapter`：`mobile` / `gdp` / `web` / `generic` / `fallback`
- `has_evaluator`：是否配置了对应的 evaluator skill
- `install`：`{"options": [ {"method", "command", "hint"}, ... ]}` 安装提示列表（可能 null）

**后续分支**：
1. `has_evaluator == true` → 进入 Step 3，先尝试 `Skill()` 调用
   - 调用成功 → 按 adapter 类型做参数适配 / 产出归置
   - 调用失败（skill 未安装 / 不可用）→ 走安装提示
2. `has_evaluator == false` 或 Skill 调用失败：
   - `install` 存在且 `install.options` 非空 → 逐条展示每个安装方式：
     - `method`：安装方式（`ttadk_plugin_install` / `agentbuddy` / `skills_add` / `manual`）
     - `hint`：一句话说明
     - `command`：可直接执行的 shell 命令
     推荐顺序按 options 数组顺序（第一行为推荐方式）
   - `install` 为 null → 提示「该技术栈暂无评估器，请用通用 fallback 模式」
   - 询问：执行推荐安装命令后继续 / 直接走通用 fallback
   - 选 fallback → 跳到 Step 4

---

## Step 3：按 Adapter 执行评估

对每个模块，按 **adapter 类型** 读取对应的说明文件（**选择性加载**，不要加载其他 adapter 文件），严格按其步骤执行：

| adapter | 参考文件 | 说明 |
|---------|---------|------|
| `mobile` | `adapters/mobile.md` | 适配 `tiktok-mobile-ai-friendliness` |
| `gdp` | `adapters/gdp.md` | 适配 `ai-friendly-evaluate-backend-gdp` |
| `web` | `adapters/web.md` | 适配 `ai-friendly-evaluate`（Web 前端） |
| `generic` | `adapters/generic.md` | 按统一契约直接调用（新 evaluator 或用户指定的未知 evaluator） |
| `fallback` | `fallback/SKILL.md` | 通用文件级 fallback 评估（见 Step 4） |

所有 adapter 最终都必须把：
- 主报告 + 所有维度文件 → 写入 `<ai_readiness_dir>/details/<module-key>/`
- 标准 `_evaluator_result.json` → 写到 outputDir 根目录

### 3a. 准备 outputDir（在执行 adapter 步骤前创建）

```
<ai_readiness_dir>/details/<module-key>/
```

创建该目录（含中间目录）。`<ai_readiness_dir>` 由 Step 0e 确定，**同一批评估的所有模块共用一个**，不再按目标路径散落。
- 单模块 / monorepo 多模块（同一 git 根）→ 全部写入同一个 `<output_root>/.ai-readiness/`
- 跨多个独立 repo → 每个 repo 的 `<ai_readiness_dir>` 各自独立，入口页放第一个 repo 的 `<ai_readiness_dir>/latest.md`，并在结果说明里提示其他 repo 的位置

### 3b. 校验产出

每个模块评估完成后检查（失败则报错，不通过则不会进入入口页生成）：
- `outputDir/_evaluator_result.json` 存在且可解析为 JSON
- `reportPath` 字段存在，且 `<outputDir>/<reportPath>` 文件存在
- 不在 outputDir 之外的目标项目目录内散落评估产物

不要调用 `validate_contract.py`（它是开发自测工具）。字段缺失（timestamp、score 等）属正常，入口页会优雅降级。

---

## Step 4：通用 Fallback 评估

触发条件：
- stack 为 `generic`（无法识别技术栈）
- evaluator 不可用且用户选择了「fallback」

**执行方式**：由主 agent 直接按 `fallback/SKILL.md` 执行（不走 Skill 工具）。传入参数：
```
targetPath=<target_path> outputDir=<outputDir> mode=<mode> language=<lang>
```

Fallback 产出：
- `outputDir/report.md`：极轻量基线报告
- `outputDir/_evaluator_result.json`：标准 result JSON
- score 为 **0–6 的原始整数**（不是百分制），入口页直接展示为「X/6」

报告必须在显眼位置标注：「通用模式，结果仅供参考，建议安装对应技术栈的评估器获得准确结果」。

---

## Step 5：生成统一入口页（决策 13：合并本次 + 现存所有模块）

**先构建合并模块列表**——不要只传本次评估的模块给 generate_entry：
1. 扫描 `<ai_readiness_dir>/details/` 下的所有子目录
2. 对每个子目录：检查 `_evaluator_result.json` 存在 → 视为一个「现存模块」
3. 把每个现存模块分成两类：
   - **本次评估的模块**（`is_this_run=true`）：用户本次传入、刚评估完的模块
   - **历史现存模块**（`is_this_run=false`）：details 里有、但本次未重跑的模块

### 单模块场景
```bash
python3 <skill_root>/scripts/generate_entry.py \
  --output-dir <ai_readiness_dir> \
  --result-json <outputDir>/_evaluator_result.json \
  --stack <stack_id> \
  --evaluator-skill <evaluator_skill_name> \
  --module-key <module_key> \
  --mode <quick|deep> \
  --is-this-run true \
  --language <zh|en>
```
（如有其它现存模块，仍用多模块 `--module` 形式合并传递。）

### 多模块场景（推荐，哪怕只有一个模块——统一格式）
为每个模块（本次 + 历史现存）拼装一个 `--module` 参数串：

```bash
python3 <skill_root>/scripts/generate_entry.py \
  --output-dir <ai_readiness_dir> \
  --language <zh|en> \
  --module "stack=<sid>,evaluator=<skill>,key=<mkey>,result=<abs_result_json_path>,mode=<mode>,this_run=1" \
  --module "stack=<sid2>,evaluator=<skill2>,key=<mkey2>,result=<abs_path2>,mode=<mode2>,this_run=0" \
  ...
```

`--module` 字段说明：
- 必需：`stack` / `evaluator` / `key` / `result`（绝对路径到 `_evaluator_result.json`）
- 可选：`mode`（默认 `deep`）/ `this_run`（`1` = 本次评估，`0` 或省略 = 历史现存）
- 缺失 `this_run` 时 generate_entry 视为历史模块处理

脚本产出：
- `latest.md`：单模块展示内联摘要；多模块展示汇总表（含「评估时间」列，历史结果弱化样式 + "上次评估 YYYY-MM-DD" 标记）+ 各模块详情
- `latest.json`：结构化元数据，每个模块带 `is_this_run` 字段

---

## Step 6：归档历史报告（决策 13：归档所有现存模块快照）

对每个 `.ai-readiness/` 目录执行（**不传 `--module-key`**，让脚本自动扫描 details 下的所有现存模块）：

```bash
python3 <skill_root>/scripts/manage_history.py archive \
  --output-dir <ai_readiness_dir>
```

**扫描规则**：脚本会遍历 `<output-dir>/details/` 下所有子目录，凡是存在 `_evaluator_result.json` 的模块都纳入本次归档（不需要显式列出，覆盖本次 + 历史现存所有模块）。只有调试场景才显式传 `--module-key` 缩小归档范围。

归档内容（全部写入 `<ai_readiness_dir>/history/`）：
- `latest.md` → `<date>.md`（支持同一天多次运行，文件名自动加 `-2` / `-3` …）
- `latest.json` → `<date>.json`
- 每个模块的 `_evaluator_result.json` → `<date>--<module-key>--result.json`

分级保留策略：最近 7 天每天 1 份 → 8–30 天每 3 天 1 份 → 31–90 天每周 1 份 → 91–365 天每月 1 份 → 1–2 年每季度 1 份。总上限 50 份，超限清理最旧的。

---

## Step 7：向用户展示结果

### 单模块摘要

- 技术栈 + 评估器名
- 分数 / 等级（没有就不展示；fallback 展示为 X/6）
- 一句话摘要（如有）
- 报告位置：`<ai_readiness_dir>/latest.md` + 主报告 `<outputDir>/<reportPath>`

### 多模块摘要

- 汇总表：模块名 / 技术栈 / 分数 / 等级 / 跳转链接
- 入口页：`<ai_readiness_dir>/latest.md`

### 下一步建议

- Fallback 模式 → 提示 `install` 里的安装命令
- 深度模式 → 提示可 `--quick` 快速复查
- 提示历史报告在 `history/` 目录
- **场景化提示（决策 12）**：当任一目标路径的真实目录 ≠ `<output_root>`（即评估的是子包），额外打印：
  - 中文：「本次结果已统一写入 `<output_root>/.ai-readiness/`；如需放到子模块目录，可加 `--output-root <子模块路径>` 重跑」
  - 英文：「Results are written to `<output_root>/.ai-readiness/`. Use `--output-root <sub-path>` to redirect to a sub-module directory.」
  - 若全部目标路径均 = `<output_root>`（即评估的就是 repo 根本身）→ **不打印**，避免噪声

---

## 错误处理速查

| 场景 | 处理 |
|------|------|
| 目标路径不存在 / 非目录 | 报错，提示有效路径 |
| 栈检测 low confidence | 列候选让用户选；不选则 fallback |
| 多栈冲突（Monorepo） | 列候选 + 建议子目录，询问评估哪一个或全部 |
| Evaluator 未安装 | 展示 install command；询问安装后继续 / fallback |
| Evaluator 执行中途失败 | 保留部分产出，报错；多模块时其他模块继续 |
| 产出校验失败（缺 result.json / 缺报告） | 报错 + 告知已产生文件的路径 |
| outputDir 写入权限失败 | 报错说明权限问题 |
| Adapter 适配不兼容 | 降级到 fallback，提示用户 |
