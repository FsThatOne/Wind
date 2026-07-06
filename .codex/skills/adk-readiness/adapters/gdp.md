# Adapter: GDP（后端 Go / GDP 架构）

适配对象：`ai-friendly-evaluate-backend-gdp` skill

## 背景

GDP evaluator 原生是「用户交互 + 问卷模式」：用户要先回答 intake 问卷，才能跑评估。报告语言参数用 `chinese` / `english` 而非 `zh` / `en`。评估过程依赖 evaluator 自带的 Node 脚本（`runtime-workspace.js` / `validate-intake.js` / `calc-score.js` 等）。

本 adapter 负责：
1. 自动填写 intake 问卷（合理默认值），跳过用户交互
2. 创建 runtime 工作区并执行校验
3. 按 GDP evaluator 的严格步骤契约执行完整评估
4. 把 runtime 里的报告和附录文件归置到统一 outputDir
5. 从主报告提取摘要，生成标准 `_evaluator_result.json`

---

## 适配步骤

### 1. 验证目标路径

检查目标目录下存在 `go.mod`。没有就直接报错中止，说明「目标路径不是有效的 Go 模块根目录」。

### 2. 自动填写 Intake 问卷

把以下字段写入临时文件 `intake-answers.json`（稍后放入 runtime 目录）：

| 字段 | 值 | 说明 |
|------|----|------|
| `target_path` | `<target_path>` | 传入的目标路径 |
| `project_role` | `auto` | 让 evaluator 根据代码结构自动判断基建 / 业务 |
| `gdp_strict` | `strict`（有 `.gdp/` 目录时） / `relaxed`（否则） | GDP 严格度 |
| `evaluation_goal` | `AI 友好度基线评估（通过 adk-readiness 统一入口调用）` | 仅作记录 |
| `report_language` | `chinese` / `english` | 由 `--lang zh/en` 映射 |
| `mode` | `single` | 单模块模式 |
| `output_path` | 留空或填 runtime_dir | 产出最终会归置到 outputDir |

### 3. 定位 evaluator 的文件系统根目录

GDP evaluator 的 Node 脚本必须从其 skill 目录下执行。按以下顺序确定 `<gdp_skill_path>`：

1. 如果 evaluator skill 已通过 Skill 工具注册、可正常调用，尝试解析其安装路径
2. 否则在 repo-root 下搜索：
   - `<repo-root>/ai-friendly/ai-friendly-evaluate-backend-gdp/`
   - `<repo-root>/plugins/*/skills/ai-friendly-evaluate-backend-gdp/`
3. 找不到 → 报错中止，提示用户安装（使用 `install` 字段给出的命令）

### 4. 创建 runtime workspace

在 evaluator skill 根目录执行：
```bash
node <gdp_skill_path>/scripts/runtime-workspace.js \
  --action create \
  --target-path "<target_path>"
```
从 stdout 中解析 `runtime_dir`（通常是 OS 临时目录下的一个随机路径）。后续所有临时文件都写入这里。

### 5. 写入预填 intake + 校验

1. 把第 2 步生成的 `intake-answers.json` 复制到 `<runtime_dir>/`
2. 执行校验：
   ```bash
   node <gdp_skill_path>/scripts/validate-intake.js \
     --input <runtime_dir>/intake-answers.json \
     > <runtime_dir>/intake-context.json
   ```
3. 如果校验失败（非 0 退出码 / 输出含 error）：
   - 尝试用更宽松的默认值重填一次（如 gdp_strict 改成 relaxed）
   - 再次失败就报错中止

### 6. 调用 evaluator 主体

GDP skill 的执行流程是严格契约（Step 2 单模块执行契约），必须按顺序完整执行：

1. 读取 evaluator 的参考文档（加载到上下文）：
   - `<gdp_skill_path>/references/gdp-backend-ai-friendly-scoring.md`
   - `<gdp_skill_path>/references/rubric-quick-map.md`
   - `<gdp_skill_path>/references/shared-validation.md`
   - `<gdp_skill_path>/references/shared-evidence-collection.md`
   - `<gdp_skill_path>/references/shared-scoring-and-calculation.md`
   - `<gdp_skill_path>/references/shared-report-artifacts.md`
   - `<gdp_skill_path>/references/shared-runtime-cleanup.md`
2. 按 `shared-validation.md` 执行校验和作用域设置
3. 按 `shared-evidence-collection.md` 采集证据
4. 按 `shared-scoring-and-calculation.md` 执行评分与短板修正
5. 按 `shared-report-artifacts.md` 使用 `report_language`（chinese / english）生成报告
6. 始终按 `shared-runtime-cleanup.md` 执行清理

**注意事项**：
- `report_language` 由 adapter 传入，不要让 evaluator 再次询问用户
- 此步涉及 LLM 评分调用，耗时较长，耐心等待
- 执行中途如果某个维度失败，不要中止整体评估——记录失败维度，继续跑完，保留部分结果

### 7. 归置产出

完成后，从以下位置收集文件：

| 原路径 | 目标位置 | 说明 |
|--------|---------|------|
| `<runtime_dir>/summary.md` | `<outputDir>/evaluation-report.md` | 主报告，重命名 |
| `<runtime_dir>/appendix-*.md` | `<outputDir>/appendix-*.md` | 附录文件，保留原名 |
| `<runtime_dir>/*.json`（维度数据、计算中间产物） | `<outputDir>/` 同名文件 | 原始数据 |
| GDP skill 默认输出目录（如果有） | `<outputDir>/` | 兜底收集 |

操作要点：
- 用「复制 + 清理」而非「移动」——避免 runtime 清理流程执行失败时丢失产出
- 完成后手动确认 `<runtime_dir>` 已被清理（shared-runtime-cleanup 约定），没有残留

### 8. 提取摘要 & 生成 result JSON

```bash
python3 <skill_root>/scripts/extract_gdp_summary.py \
  --report <outputDir>/evaluation-report.md \
  --output <outputDir>/_evaluator_result.json \
  --mode <quick|deep>
```
