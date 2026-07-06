# Adapter: Mobile（Android / iOS）

适配对象：`tiktok-mobile-ai-friendliness` skill

## 背景

Mobile evaluator 的原生输入是 `--module` / `--module-path` / `--platform` / `--tiktok-root`，**不接受**统一契约的 `targetPath` / `outputDir`，并且默认输出到 `<module-path>/.ai-friendliness/` 而不是我们约定的 outputDir。

本 adapter 负责：
1. 把统一契约翻译成 evaluator 能理解的参数
2. 把 evaluator 产出从默认位置归置到 outputDir
3. 从最终 Markdown 报告里提取摘要信息，生成标准 `_evaluator_result.json`

---

## 适配步骤

### 1. 推断平台

结合 stack id 和目标目录特征判断：

| 线索 | 平台 |
|------|------|
| stack id 是 `mobile-android` | → Android |
| stack id 是 `mobile-ios` | → iOS |
| 目录有 `build.gradle` / `*.kt` / `*.java` | → Android |
| 目录有 `*.swift` / `*.m` / `*.h` / `.podspec` | → iOS |

如果 stack id 已有明确值（用户传了 `--stack` 或检测置信度高），直接使用。
否则结合文件特征判断。全都判断不出 → 报错让用户用 `--stack` 显式指定。

### 2. 推断模块名

- 取目标目录 basename
- Android：该 basename 即 Gradle 短名（通常对应 `include ':'` 里的名字）
- iOS：如果有 `.podspec` 文件，优先取 podspec 中的模块名，否则用目录名

### 3. 推断 tiktok-root

从目标路径逐层往上找，第一个命中的作为 TikTok 工程根：
- Android：遇到 `settings.gradle` / `settings.gradle.kts` 或 `.git` 根
- iOS：遇到 `Podfile` 或 `.xcworkspace` 或 `.git` 根

找不到就退化为目标路径的父目录，并向用户警告"tiktok-root 推断可能不准确，建议手动指定"。

### 4. 调用 evaluator

**优先用 Skill 工具**：
```
Skill(
  skill="tiktok-mobile-ai-friendliness",
  args="--module <模块名> --module-path <target_path 绝对路径> --platform <Android|iOS> --tiktok-root <tiktok_root 绝对路径>"
)
```

如果 Skill 工具不可用（skill 未安装），退化为：
1. 在 repo-root 下搜索 evaluator 的 SKILL.md 文件：
   - `<repo-root>/ai-friendly/tiktok-mobile-ai-friendliness/SKILL.md`
   - `<repo-root>/plugins/*/skills/tiktok-mobile-ai-friendliness/SKILL.md`
2. 读取后按其步骤手动执行，传入同样的参数。

**语言参数**（如果 evaluator 支持语言选项）：
- `--lang zh` → 传 `--language zh`
- `--lang en` → 传 `--language en`

### 5. 归置产出

Mobile evaluator 默认输出目录是 `<module-path>/.ai-friendliness/`。

操作：
1. 确认该目录存在且非空
2. **移动**其中所有文件和子目录到 `outputDir`，保留目录结构
3. 移动完成后：
   - 检查原 `.ai-friendliness/` 目录是否为空，非空则清理剩余文件并 rmdir
   - 检查 `outputDir` 下应至少有 `ai_friendly_report.md`（主报告）以及若干 `*.json` 维度文件

**如果 evaluator 后续版本支持显式指定输出目录参数**，应优先用参数而不是事后移动。

### 6. 提取摘要 & 生成 result JSON

执行：
```bash
python3 <skill_root>/scripts/extract_mobile_summary.py \
  --report <outputDir>/ai_friendly_report.md \
  --output <outputDir>/_evaluator_result.json \
  --platform <android|ios> \
  --mode <quick|deep>
```

该脚本产出标准 `_evaluator_result.json`，字段包括：
- `evaluator` = `mobile-<platform>`
- `score` / `level` / `summary` 从报告里提取
- `reportPath` = `ai_friendly_report.md`
- `timestamp` 从报告里提取或用当前时间
