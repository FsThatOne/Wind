# SDT 持续学习工作流

本文定义 `adk-sdt-learning` 使用的持久化学习系统。

## 适用范围

本工作流为以下 SDT 技能提供持续学习上下文：

- `adk-sdt`
- `adk-sdt-ff`
- `adk-sdt-clarify`
- `adk-sdt-implement`
- `qa-sdt-ff`
- `qa-sdt-clarify`
- `qa-sdt-implement`

它只提供知识召回、经验沉淀和知识库治理，不替代这些 SDT 阶段技能本身。

## `.sdt_preference.yaml` 配置

所有可迁移配置一律写在 git 仓库根目录的 `.sdt_preference.yaml` 文件内。该文件使用 YAML 格式。

示例：

```yaml
sdt_learning:
  learning_root: .experience
  record_query_usage: confirm
  query_usage_confirmation: true
  default_write_policy: learning-root-only
  owner: qa-platform
  domain_tags: payment, order
  external_sources:
    team_knowledge:
      path: third_party/sdt-knowledge
      type: submodule
      read_only: true
      keywords: payment, refund, order
      write_policy: read-only
      owner: QA Platform
```

字段规则：

- `learning_root`：必填，SDT learning 可写根目录，推荐 `.experience`。
- `record_query_usage`：`never | confirm | always`，默认 `confirm`。横向交付默认使用 `confirm`。
- `query_usage_confirmation`：是否在 SDT 轮次结束前告知用户将记录本轮 Query 使用结果，默认 `true`。用户纠正或拒绝时不写；用户未纠正并进入轮次收尾时，视为默认同意并自动记录。
- `default_write_policy`：默认 `learning-root-only`，表示 Ingest 只能写 `learning_root`。
- `sdt_learning.external_sources.*`：用户自维护知识库或 submodule。默认 `read_only: true` 且 `write_policy: read-only`。

除非用户本轮明确要求临时覆盖，否则不得使用环境变量或隐式目录替代 `.sdt_preference.yaml`。

### 轻量初始化检查

Query / Ingest 前使用脚本判断 learning repository 是否已初始化，避免模型手工扫描目录：

```bash
python3 $SKILL_DIR/scripts/validate_learning_repo.py --repo-root <repo-root> --check initialized --json
```

脚本只检查 `.sdt_preference.yaml`、`learning_root` 和必要目录/索引骨架，不扫描经验页、外部知识源或执行 Lint 级治理。

JSON 结果处理：

- `initialized: true`：继续 Query / Ingest。
- `initialized: false` 且 `bootstrap_required: true`：执行一次脚本 Bootstrap，再重试原操作。
- `initialized: false` 且 `bootstrap_required: false`：停止并报告 `issues`，不要猜测修复配置冲突。

Bootstrap 固定使用幂等脚本，不要由模型手工创建目录或索引文件：

```bash
python3 $SKILL_DIR/scripts/bootstrap_learning_repo.py --repo-root <repo-root> --json
```

## 目录结构

```text
<SDT_EXPERIENCE_ROOT>/
├── Clippings/
│   └── YYYY-MM-DD-<source>-<slug>.md
└── experience/
    ├── INDEX.md
    ├── log.md
    ├── maintenance.json
    ├── indexes/
    │   ├── hot.md
    │   ├── by-domain.md
    │   ├── by-platform.md
    │   ├── by-signal.md
    │   ├── external-sources.md
    │   └── pending-decisions.md
    ├── articles/
    ├── concepts/
    └── topics/
```

在 Bootstrap 或 Ingest 时创建缺失目录。不要重写或删除已有 clippings。

## 文件职责

- `Clippings/`：原始事实层。只能新增文件。尽量保留用户、报告、spec 和失败信息中的原始表述。
- `experience/articles/`：从一个或多个 clipping 编译出的经验文章，通常绑定到某个 feature、服务、页面、API 或失败模式。
- `experience/concepts/`：可复用的定义和业务规则，例如状态机、权限、灰度规则、数据约束或可观测锚点。
- `experience/topics/`：面向任务的 playbook，例如“后端 API SDT 数据准备”或“Web E2E 登录门禁”。
- `experience/INDEX.md`：主路由入口，只保留检索规则、高频入口摘要和分片索引链接，不承载全量条目。
- `experience/indexes/`：分片索引，按业务域、平台、信号类型、待裁决项和高频经验拆分，避免主索引无限增长。
- `experience/indexes/external-sources.md`：记录用户自维护知识库、submodule 或团队规范目录的入口与用途。
- `experience/log.md`：时间导向、只追加的学习操作时间线。
- `experience/maintenance.json`：运行维护状态，记录最近一次 Lint 时间和结果；它不是可迁移配置。

## 用户知识库兼容模型

用户自维护知识库与 `experience/` 是补充关系，不是排他关系：

- 用户知识库是原始知识源，通常由用户或团队维护，例如仓库 submodule、业务知识库目录、团队测试规范目录。
- `experience/` 是 SDT 经验层，负责记录外部知识的路由索引、SDT 视角摘要、适用条件、使用/验证结果和相关 clipping。
- 默认不要把外部知识全文复制进 `experience/`。只在经验页中记录 `source_path`、`source_section`、`source_commit` 和面向 SDT 的可执行摘要。
- 默认不要改写用户知识库。外部知识源一律按只读处理，除非 `.sdt_preference.yaml` 中显式允许写入，且用户在当前轮再次确认目标路径与风险。
- submodule 视为独立 git repo。即使允许写入，也必须提示用户这会改动另一个仓库，后续需要单独提交。

外部知识引用示例：

```markdown
## 外部知识来源

- source_type: user-knowledge-base
- source_path: `third_party/sdt-knowledge/payment/refund-rule.md`
- source_commit: `abc1234`
- source_section: `退款状态机`
- source_owner: QA Platform
```

## 主索引容量约束

`experience/INDEX.md` 的目标是长期保持在 100 行以内。它只允许承载：

- 使用规则。
- 高频入口摘要 Top 10。
- 分片索引链接。
- 最近更新摘要 Top 10。
- 关键待裁决摘要 Top 5。

容量规则：

- 超过 150 行：Lint 必须提示主索引开始膨胀，并建议迁移到 `experience/indexes/`。
- 超过 300 行：Lint 必须判定为结构违规，要求拆分或迁移。300 行是熔断线，不是正常容量目标。
- 任何时候只要 `INDEX.md` 开始承载全量经验条目，都视为结构违规，即使没有超过 300 行。

## Bootstrap

Bootstrap 必须通过脚本执行：

```bash
python3 $SKILL_DIR/scripts/bootstrap_learning_repo.py --repo-root <repo-root> --json
```

脚本行为：

1. 定位 git 仓库根目录。
2. 如果 `.sdt_preference.yaml` 不存在，创建默认配置；如果已存在，只读取 `sdt_learning.learning_root`，不覆盖用户配置。
3. 从 `sdt_learning.learning_root` 解析 `SDT_EXPERIENCE_ROOT`。相对路径按仓库根目录解析。
4. 创建缺失目录和缺失索引文件。
5. 已有文件一律保留；配置缺失、路径冲突或无法写入时返回错误 JSON，由调用方停止并提示用户修复。

如果缺少 `experience/INDEX.md`，脚本用以下内容创建。

```markdown
# SDT 经验索引入口

## 使用规则

- 先读本文件判断应进入哪个分片索引。
- 再读取 1 个最相关的 `indexes/*.md`。
- 最后只读取 2-5 个经验页。
- 高频经验来自用户确认或执行结果支持，可视为已确认经验；但应用到新场景前仍必须验证适用条件、证据和当前 spec/code。

## 高频入口

见 `indexes/hot.md`。

## 分片索引

- 按业务域：`indexes/by-domain.md`
- 按平台：`indexes/by-platform.md`
- 按信号类型：`indexes/by-signal.md`
- 外部知识源：`indexes/external-sources.md`
- 高频经验：`indexes/hot.md`
- 待裁决项：`indexes/pending-decisions.md`
```

如果缺少 `experience/indexes/hot.md`，脚本用以下内容创建。

```markdown
# 高频经验索引

| 经验页 | 关键词 | 最近使用 | 状态 | 备注 |
| --- | --- | --- | --- | --- |
```

如果缺少 `experience/indexes/by-domain.md`，脚本用以下内容创建。

```markdown
# 按业务域索引

| 业务域/PSM/产品线 | 关键词 | 入口页面 | 状态 |
| --- | --- | --- | --- |
```

如果缺少 `experience/indexes/by-platform.md`，脚本用以下内容创建。

```markdown
# 按平台索引

| 平台 | 关键词 | 入口页面 | 状态 |
| --- | --- | --- | --- |
```

如果缺少 `experience/indexes/by-signal.md`，脚本用以下内容创建。

```markdown
# 按信号类型索引

| 信号类型 | 关键词 | 入口页面 | 状态 |
| --- | --- | --- | --- |
```

如果缺少 `experience/indexes/external-sources.md`，脚本用以下内容创建。

```markdown
# 外部知识源索引

| 知识源 | 路径 | 类型 | 关键词 | 读取策略 | 写入策略 |
| --- | --- | --- | --- | --- | --- |
```

如果缺少 `experience/indexes/pending-decisions.md`，脚本用以下内容创建。

```markdown
# 待裁决项

| 事项 | 上下文 | Owner | 开始时间 | 关联页面 |
| --- | --- | --- | --- | --- |
```

如果缺少 `experience/log.md`，脚本用以下内容创建。

```markdown
# SDT 经验日志

| 日期 | 操作 | 来源 | 更新页面 | 备注 |
| --- | --- | --- | --- | --- |
```

如果缺少 `experience/maintenance.json`，脚本用以下内容创建。

```json
{
  "last_linted_at": null,
  "last_lint_result": null
}
```

## Query

当业务上下文可能影响测试设计或执行时，在应用 SDT 前使用 Query。SDT 前置检索默认使用纯 Query 模式；纯 Query 的规则、检索预算和 `search_stopped_reason` 已写在 `SKILL.md` 中，执行时不需要加载本 workflow。以下内容用于解释完整语义和 Record / Ingest 收尾，不应让每次检索都走完整流程。

1. 运行轻量初始化检查；只有脚本返回 `bootstrap_required: true` 时才自动执行 `bootstrap_learning_repo.py` 一次，再继续 Query。若脚本返回非 Bootstrap 可修复错误，停止并提示用户修复初始化问题。
2. 读取 `.sdt_preference.yaml`，解析 `learning_root` 和 `record_query_usage`。
3. 读取 `experience/INDEX.md`，只用它判断下一步应读哪个分片索引。
4. 按 feature 名、PSM、页面名、API 名、组件、领域关键词、平台、测试类型、失败签名和即将询问的问题类型，从 `experience/indexes/` 中选择 0-1 个最相关分片；如果有明确高频命中，可同时读取 `indexes/hot.md`。
5. 从分片索引召回经验页。`indexes/hot.md` 中的经验已经过用户确认或执行结果支持，可作为优先经验，但不要跨场景无条件套用。
6. 对每个召回经验页执行适用性验证：
   - `何时使用` 是否匹配当前 spec/code/task。
   - `证据` 是否存在且可追溯到 clippings。
   - `状态` 不是 `deprecated` 或 `needs-review`。
   - 当前 spec/code 没有与经验规则冲突。
   - `last_verified_at` 不过旧；过旧时只能作为低置信建议。
7. 如果经验页引用外部知识，并且当前任务需要更多上下文，再按 `source_path` 读取原文对应章节；不要默认读取整个外部知识库。
8. 只读取并应用验证通过的 2-5 个经验页。
9. 将规则综合成 SDT 指导。
10. 如果选中的页面过期、互相矛盾或证据不足，明确说明不确定性，并回退到当前 spec/code 证据。
11. 达到纯 Query 检索预算或没有索引命中时必须停止，返回 `search_stopped_reason`；不得扩大到全库扫描。

SDT 指导应覆盖：必须新增、删除或优先执行的测试用例；会影响测试的数据准备、环境或灰度约束；只能人工执行、容易 flaky、被阻塞或需要特殊可观测手段的检查；以及会改变执行方式的已知失败模式或历史修复。

Query 不做后置写入。它只返回 `round_record_candidate`，由一轮 SDT 结束时的 Record 操作统一处理。

不要加载无关经验页。索引的目标是减少上下文，而不是扩大上下文。找不到索引命中时，说明当前索引体系没有覆盖该任务；Query 应返回无命中，后续由轮次收尾的 Ingest / Record / Lint 修补经验或索引。

## SDT 轮次学习收尾

一轮 SDT 指从进入 `adk-sdt` / `qa-sdt` 或任一阶段 skill，到本次 case 生成、clarify 或 implement/report 结束为止。

轮次内规则：

1. 每次需要经验时只执行纯 Query。
2. 将 Query 返回的 `round_record_candidate` 暂存到本轮学习状态。
3. 将用户纠正、clarify 决策、测试失败、报告结论等高价值信号暂存为本轮 Ingest 候选。
4. 轮次中不写 hit count、不追加 `log.md`、不创建 clipping，除非用户明确要求立即 Ingest。
5. 轮次中不执行 Lint。

轮次结束前必须执行一次学习收尾检查：

1. 如果存在实际使用过的 `round_record_candidate`，执行 Record。
2. 如果存在高价值学习信号，执行 Ingest。
3. 如果本轮发生过 Record 或 Ingest，读取 `experience/maintenance.json`；`last_linted_at` 为空或距离当前日期超过 30 天时，自动执行一次 Lint。
4. 如果用户明确拒绝记录、经验未被实际使用或当前上下文证明不适用，不要增加成功命中；可记录为 `not_applicable`、`correction`、`conflict` 或跳过。
5. 收尾完成后，再向用户输出最终 SDT 总结。

### Record

Record 只记录本轮已实际使用的经验，不沉淀新知识。

根据 `record_query_usage` 决定是否写入使用记录：

- `never`：不写。
- `confirm`：向用户展示将更新的经验页、计数字段和日志内容；如果用户纠正、拒绝或要求调整，则先处理反馈且不自动写；如果用户没有纠正并继续到本轮收尾，视为默认同意并自动写入。
- `always`：仅当用户或团队已明确接受该策略时可用；横向默认不使用。

满足写入条件后记录结果：

- 若用户确认、case/report 被接受，或执行结果支持该经验，记为 `successful_application`。
- 若用户纠正、case 被删除/改写，或执行失败证明前提不成立，记为 `correction` 或 `conflict`。
- 若当前上下文不满足适用条件，记为 `not_applicable`，不要增加成功计数。

## 状态与轻量 Record

默认保留 5 个状态，但不做复杂热度排名：

- `candidate`：新沉淀或尚未被充分验证。
- `active`：已被成功使用，后续 Query 可正常召回。
- `trusted`：跨 feature 多次验证、低纠错、仍然有效。
- `needs-review`：被用户强纠正，或多次命中但效果不稳定；Query 不应默认应用。
- `deprecated`：被当前 spec/code、用户纠正或执行结果证明不应继续使用。

`indexes/hot.md` 是可选的维护入口，只放团队明确想优先召回的经验；不要在每轮 SDT 中重算 hot 排名。

每个结构化经验页都应维护以下元数据：

```markdown
## 元数据

- successful_applications: 0
- last_used_at:
- last_verified_at:
- status: candidate | active | trusted | needs-review | deprecated
```

Record 规则：

- Query 召回某经验页时不写文件。
- SDT 轮次结束且满足 `record_query_usage` 写入条件后，才更新 `last_used_at`、`successful_applications` 和 `log.md`。
- 用户纠正或当前任务证明经验不适用时，不增加 `successful_applications`；必要时把 `status` 改为 `needs-review` 或 `deprecated`。
- Ingest 只创建或更新经验内容，不负责维护 hot 排名。

## Ingest

Ingest 有两种入口：

- **Auto Ingest**：SDT 轮次末尾执行，保守、低频，只沉淀本轮高价值学习信号。
- **Guided Ingest**：用户主动要求学习经验时执行，立即、简短、可确认。

在出现高价值学习信号后使用 Ingest：

- 用户纠正了生成的 case、task、report 或 SDT 假设。
- `adk-sdt-clarify` 解决了有争议的用例。
- `adk-sdt-implement` 暴露了失败模式、flaky 测试、缺失的数据规则、环境约束或有价值诊断。
- 重复出现的 SDT workaround 暗示应沉淀为可复用规则、helper、模板或 skill-profile 变更。

### Guided Ingest

当用户明确说“记住这个经验”、“学一下”、“以后遇到这种情况要...”或要求沉淀规则时，可以不等 SDT 轮次结束，立即执行 Guided Ingest。

Guided Ingest 流程：

1. 判断输入更适合写成稳定事实、复用规则、流程/策略、失败复盘、clarify 决策还是领域规则。
2. 简短确认目标：记录类型、触发条件、目标页面。用户表达非常明确时，可以直接写入。
3. 保持最小写入：`何时使用`、`规则`、`对 SDT 的影响`、`证据`、`status`。
4. 信息不确定时标记为 `candidate`，不要扩写成宽泛经验。
5. 稳定事实写成 experience 页内的轻量表格段落，不创建独立事实层。

执行 Ingest 前，先运行轻量初始化检查。只有脚本返回 `bootstrap_required: true` 时才自动执行 `bootstrap_learning_repo.py` 一次，再继续写入；若脚本返回非 Bootstrap 可修复错误，停止并提示用户修复初始化问题。

### 写入边界

- 默认只允许写入 `sdt_learning.learning_root` 下的 `Clippings/`、`experience/` 和 `log.md`。
- 外部知识源默认只读。即使用户知识库以 submodule 形式存在于当前仓库，也不能默认写入。
- 轻量模式下，不自动回写外部知识源；如果确实需要，把建议写入 `pending-decisions.md`，由用户或维护者后续处理。

### Step 1：创建 Clipping

Ingest 在 `Clippings/` 下创建新的 clipping，结构如下：

```markdown
---
date: YYYY-MM-DD
source: user-correction | clarify | implement-report | code-discovery | spec-discovery | other
feature_dir: <path-or-empty>
platform: Backend | Frontend | Client | Unknown
status: raw
---

# <简短事实标题>

## 原始信号

<引用或总结原始纠正、报告、失败或发现。重要信息尽量保留原话。>

## 上下文

- 相关 spec/code/report：
- 相关测试用例：
- 相关任务 ID：
- 环境：

## 候选学习点

<SDT 下次可能复用的内容。必须明确标记不确定性。>
```

### Step 2：编译 Experience

判断该 clipping 应更新已有页面，还是创建新页面。

- feature 相关或事故相关经验放入 `articles/`。
- 稳定业务规则和定义放入 `concepts/`。
- 可复用流程或 playbook 放入 `topics/`。

每个结构化页面应包含：

```markdown
# <Title>

## 元数据

- successful_applications: 0
- last_used_at:
- last_verified_at:
- status: candidate

## 何时使用

<哪些信号出现时，SDT 应读取此页。>

## 规则

- <具体可复用规则>

## 对 SDT 的影响

- 用例生成：
- 澄清：
- 执行/报告：

## 证据

- `../../Clippings/<file>.md`

## 外部知识来源

- source_type:
- source_path:
- source_commit:
- source_section:
- source_owner:

## 新鲜度

- 最近验证：
- 状态：candidate | active | trusted | needs-review | deprecated
```

### Step 3：更新交叉引用

1. 从结构化页面回链到 clippings。
2. 在相关 articles、concepts、topics 之间补充链接。
3. 更新 `experience/INDEX.md`：
   - 只更新主路由、高频入口摘要或分片索引链接。
   - 不把全量页面列表塞进 `INDEX.md`。
4. 更新相关 `experience/indexes/*.md`：
   - 新页面进入对应业务域、平台、信号类型分片。
   - 如果经验页引用用户知识库，可更新 `indexes/external-sources.md`。
   - 需要人工判断的事项进入 `indexes/pending-decisions.md`。
5. 向 `experience/log.md` 追加一行。

```markdown
| YYYY-MM-DD | Ingest | <clipping> | <pages> | <short note> |
```

## Lint

当用户要求检查学习库、较大范围推广 SDT 前，或多次 Ingest 后，按需运行 Lint。Lint 是维护动作，不是每轮 SDT 的默认步骤。

自动 Lint 触发规则：

- 纯 Query 不检查也不触发 Lint。
- 用户主动要求 Lint / 检查 / 清理 / 维护学习库时，直接运行 Lint。
- SDT 轮次收尾后，如果本轮发生过 Record 或 Ingest，且 `maintenance.json.last_linted_at` 为空或超过 30 天，自动运行一次 Lint。
- Lint 完成后更新 `maintenance.json` 的 `last_linted_at` 和 `last_lint_result`。

检查：

- 超过 14 天且没有被任何结构化页面引用的 clippings。
- 缺少 `Evidence` 或 `Freshness` 的结构化页面。
- 指向不存在文件的链接。
- 未出现在 `experience/INDEX.md` 中的页面。
- `INDEX.md` 中记录但实际已不存在的页面。
- `INDEX.md` 超过 150 行，说明主索引开始膨胀，需要迁移内容到 `experience/indexes/`。
- `INDEX.md` 超过 300 行，必须判定为结构违规；300 行是熔断线，不是正常容量目标。
- `INDEX.md` 开始承载全量经验条目，即使未超过 300 行，也必须判定为结构违规。
- 单个分片索引超过 500 行，应继续拆分，例如 `indexes/domain/order.md`。
- `indexes/hot.md` 中有 `deprecated` 页面。
- `indexes/external-sources.md` 中记录的路径不存在，或 submodule 未初始化。
- 经验页记录了 `source_path`，但缺少面向 SDT 的摘要。
- 结构化经验页仍使用 `confidence` 字段，或同时存在 `confidence` 与 `status`。
- 超过 30 天的待裁决项。
- 同一领域、API、页面或测试类型下互相矛盾的 active 规则。
- 过于宽泛、会迫使 SDT 读入太多无关上下文的页面。

可以直接修复安全的机械问题，例如为现有页面补 index 行。对于语义冲突，应报告为待裁决项，不要猜测。

可执行校验：

```bash
python3 $SKILL_DIR/scripts/validate_learning_repo.py --repo-root <repo-root>
python3 $SKILL_DIR/scripts/validate_learning_repo.py --repo-root <repo-root> --check initialized --json
python3 $SKILL_DIR/scripts/bootstrap_learning_repo.py --repo-root <repo-root> --json
```

## Skill 自进化

当学习结果暴露重复流程缺口时，按以下类型分类改进：

- **知识更新**：业务规则应进入 `experience/`。
- **模板更新**：重复出现的输出形态变化应进入 SDT 模板。
- **Profile 更新**：某个领域需要不同的下游测试 skill 或映射。
- **新 skill**：某个重复操作需要独立、聚焦、可触发的工作流。
- **Hook**：某个捕获点应在用户纠正、clarify 会话或报告生成后自动运行。

优先选择最小且持久的改动。如果经验页或模板更新已经足够，不要新增 skill。
