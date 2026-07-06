# Adapter: Web（前端）

适配对象：`ai-friendly-evaluate` skill

## 背景

Web evaluator 的交互模式与 GDP evaluator 高度相似：需要用户先填写 intake 问卷，问卷通过后才开始评估。核心脚本也是 Node 写的（`runtime-workspace.js` / `validate-intake.js` / `calc-score.js` 等），评估流程是严格契约（Step 2 执行契约），产出为 `summary.md` + `appendix-*.md`。

差异点：
- 目标路径必须是包含 `package.json` 的前端包根目录（单项目 repo 可用 `.`，monorepo 用子项目路径）
- 默认报告语言是 **english**（GDP 默认是 chinese）
- Rubric 是 Web 版（D1-D9，权重不同），但输出结构和 GDP 一致
- 等级是 **A / B / C / D**（与 GDP 相同）

本 adapter 负责：
1. 自动填写 intake 问卷，跳过用户交互
2. 创建 runtime 工作区并执行校验
3. 调用 evaluator 的严格执行契约（加载 rubric → 校验 → 证据采集 → 评分 → 报告生成）
4. 把 runtime 里的产出归置到统一 outputDir
5. 从主报告提取摘要，生成标准 `_evaluator_result.json`

---

## 适配步骤

### 1. 验证目标路径

检查 `<target_path>/package.json` 存在：
- 不存在 → 直接报错中止，说明「目标路径不是有效的前端包根目录（缺少 package.json）」
- 存在 → 继续

### 2. 自动填写 Intake 问卷

写入临时文件 `intake-answers.json`，字段如下：

| 字段 | 值 | 说明 |
|------|----|------|
| `target_path` | `"."`（相对于 repo root 传入） | evaluator 需要的是仓库相对路径。主 agent 先确定 repo-root 后换算。如果 target_path 本身就是 repo-root，写 `"."`。 |
| `evaluation_goal` | `"AI 友好度基线评估（通过 adk-readiness 统一入口调用）"` | 仅作记录 |
| `output_path` | 留空或传 runtime 目录 | 最终产出会被归置到 outputDir，不需要在这里指定 |
| `project_role_preference` | `"auto"` | 让 evaluator 根据依赖特征自动判断基建 / 业务 |
| `report_language` | `"chinese"` 或 `"english"` | 由 `--lang zh/en` 映射 |
| `notes` | 空字符串或 `"adk-readiness unified entry, auto-filled intake"` | 可选 |

注意：evaluator 还会读取 `confirmation_required`，validate-intake 会把它设为 true。本 adapter 的策略是：**跳过确认**——因为已经是显式调用了。读取并修改 `intake-context.json`，把 `confirmation_required` 设为 `false`，避免 evaluator 停下来询问用户。

### 3. 定位 evaluator 的文件系统根目录

需要执行 evaluator 自带的 Node 脚本（runtime-workspace.js / validate-intake.js 等），所以必须拿到它的物理路径。

定位顺序：
1. 如果 evaluator 已通过 Skill() 注册可用，尝试解析其安装路径（常见位置：
   - `~/.ttadk/store/plugins/*/skill/ai-friendly-evaluate/<version>/`
   - `~/.cursor/skills/ai-friendly-evaluate/`
   - `~/.trae/skills/ai-friendly-evaluate/`
   - `<repo-root>/plugins/*/skills/ai-friendly-evaluate/`）
2. 在 repo-root 下搜索开发副本：
   - `<repo-root>/ai-friendly/ai-friendly-evaluate/`
   - `<repo-root>/plugins/*/skills/ai-friendly-evaluate/`
3. 任何路径都找不到 → 报错中止，提示用户使用 install 配置里的命令进行安装

确定后记为 `<web_skill_path>`。

### 4. 创建 runtime workspace

```bash
node <web_skill_path>/scripts/runtime-workspace.js \
  --action create \
  --target-path "<target_path>"
```
从 stdout 解析 `runtime_dir`（OS 临时目录下的随机路径）。

### 5. 写入预填 intake + 校验 + 强制跳过确认

1. 把第 2 步生成的 `intake-answers.json` 复制到 `<runtime_dir>/`
2. 执行：
   ```bash
   node <web_skill_path>/scripts/validate-intake.js \
     --input <runtime_dir>/intake-answers.json \
     > <runtime_dir>/intake-context.json
   ```
3. 退出码非 0 或输出包含 `error` → 尝试宽松默认值（如把 `project_role_preference` 改回 `auto`，去掉空字段）重试一次，再次失败就报错中止
4. **强制跳过确认**：读取 `intake-context.json`，把 `confirmation_required` 改为 `false`，覆盖写回

### 6. 调用 evaluator 主体

按其「Per-Package Execution Contract (Strict)」执行，顺序不能变：

1. 加载参考文档（到上下文）：
   - `<web_skill_path>/references/lark-ai-friendly-scoring-v0.2.md` — 核心 Rubric
   - `<web_skill_path>/references/rubric-quick-map.md` — 维度权重速查表
   - `<web_skill_path>/references/shared-validation.md`
   - `<web_skill_path>/references/shared-evidence-collection.md`
   - `<web_skill_path>/references/shared-scoring-and-calculation.md`
   - `<web_skill_path>/references/shared-report-artifacts.md`
   - `<web_skill_path>/references/shared-runtime-cleanup.md`

2. 按 `shared-validation.md` 执行校验与作用域设置
3. 按 `shared-evidence-collection.md` 采集证据
4. 按 `shared-scoring-and-calculation.md` 执行评分与短板修正
5. 按 `shared-report-artifacts.md` 使用 `report_language`（english / chinese）生成报告
6. 始终按 `shared-runtime-cleanup.md` 清理 runtime

**注意**：
- 此步涉及多次 LLM 调用（证据采集 + 评分），耗时较长
- 单个维度失败不要中止整体评估——记录失败维度，保留部分结果继续

### 7. 归置产出

完成后，从以下位置收集产物：

| 原路径 | 目标位置 | 说明 |
|--------|---------|------|
| `<runtime_dir>/summary.md` | `<outputDir>/evaluation-report.md` | 主报告，重命名 |
| `<runtime_dir>/appendix-*.md` | `<outputDir>/appendix-*.md` | 附录（评分详情、计算、上下文、风险） |
| `<runtime_dir>/*.json` | `<outputDir>/` 同名文件 | 维度评分、计算中间数据 |
| evaluator 默认输出目录 | `<outputDir>/` | 兜底收集 |

操作要点：
- 用「复制 + 清理」而不是「移动」——保留 shared-runtime-cleanup 的清理流程
- 完成后确认 `<runtime_dir>` 已被清理

### 8. 提取摘要 & 生成 result JSON

```bash
python3 <skill_root>/scripts/extract_web_summary.py \
  --report <outputDir>/evaluation-report.md \
  --output <outputDir>/_evaluator_result.json \
  --mode <quick|deep>
```
