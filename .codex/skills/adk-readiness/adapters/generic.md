# Adapter: Generic（原生支持统一契约的 evaluator）

本 adapter 适用于以下场景：
1. 新接入的 evaluator skill，**原生支持** adk-readiness 统一契约
2. 用户通过 `--evaluator` 直接指定了一个未知 evaluator skill，不知道内部实现

## 统一契约回顾

evaluator 必须接受（或通过传参方式实现）这几个字段：

| 参数 | 类型 | 说明 |
|------|------|------|
| `targetPath` | string | 评估目标绝对路径 |
| `outputDir` | string | 产出目录绝对路径，所有产出必须放这里 |
| `mode` | `quick` / `deep` | 评估深度 |
| `language` | `zh` / `en` | 报告语言 |

evaluator 跑完后，在 `outputDir` 下必须有：
- 主报告 Markdown 文件
- `_evaluator_result.json`（标准 result JSON，至少包含 `evaluator` / `mode` / `reportPath`）

---

## 适配步骤

### 1. 尝试直接调用

优先通过 `Skill()` 工具：

```
Skill(
  skill=<evaluator_skill>,
  args="targetPath=<target_path> outputDir=<output_dir> mode=<mode> language=<lang>"
)
```

如果 `Skill()` 调用不可用（skill 未安装）：
1. 在 repo-root 下搜索 evaluator 的 SKILL.md 文件
   - `<repo-root>/ai-friendly/<evaluator_skill>/SKILL.md`
   - `<repo-root>/plugins/*/skills/<evaluator_skill>/SKILL.md`
2. 读取后按 SKILL.md 按契约传参，手动执行完整流程

### 2. 校验 & 容错

正常情况：Skill() 调用完成后，outputDir 下有 `_evaluator_result.json` 和报告文件。

**异常情况处理**：

| 异常 | 处理 |
|------|------|
| evaluator 不接受契约参数 | 读取其 SKILL.md，根据文档调整传参方式，或根据其产出路径移动文件到 outputDir |
| 没有生成 `_evaluator_result.json` | 在 outputDir 中找 Markdown 主报告，尽量提取 score / level / 摘要，自己生成最小化的 `_evaluator_result.json`（至少 evaluator / mode / reportPath 三个必填） |
| 摘要提取失败 | 只写三个必填字段，其他字段留空或省略 |
| 评估执行失败 | 保留部分产出，报错；多模块时其他模块继续 |
| 输出散落在 outputDir 之外 | 尽力查找散落的报告文件，移动到 outputDir |

生成最小化 result JSON 的模板：

```json
{
  "evaluator": "<skill name or 'unknown'>",
  "version": "unknown",
  "mode": "<quick or 'deep'>",
  "timestamp": "<now ISO>",
  "score": null,
  "level": null,
  "summary": "",
  "reportPath": "<主报告相对 outputDir 的路径>",
  "dataPath": null
}
```
