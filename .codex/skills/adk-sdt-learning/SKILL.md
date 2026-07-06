---
name: adk-sdt-learning
description: "为 adk-sdt / qa-sdt 的 ff、clarify、implement 阶段建设和维护持续学习知识系统。适用于从用户纠正、测试失败、clarify 反馈、业务规则、历史 SDT 报告中沉淀经验，或用户希望让 SDT 越用越好用时。"
---

# ADK SDT 持续学习

本 skill 用于帮助 `adk-sdt` / `qa-sdt` 在反复使用中持续变好：捕获业务测试经验，编译为可复用知识，并通过索引让未来 SDT 任务按需读取，而不是把所有相关 skill 和知识一次性塞进上下文。

## 用户输入

```text
$ARGUMENTS
```

执行前 **必须** 先理解用户输入。

## Skill 查找兜底

解析 `adk-sdt-learning` 或其他 SDT skill 时，先使用当前宿主的 skill 机制；找不到时按 skill 名依次查项目根目录下的 `.claude/skills/<skillName>/SKILL.md`、`.cursor/skills/<skillName>/SKILL.md`。

## 核心模型

将 SDT 学习视为“原始证据 + experience overlay”的轻量知识系统：

- `Clippings/`：只追加的原始证据层，来源包括用户纠正、SDT 报告、失败用例、clarify 决策、代码/spec 发现和业务规则。这是不可变的真相来源。
- `experience/`：可维护的结构化经验层，从 clippings 编译而来，代表未来 SDT 运行时应复用的最新综合认知。稳定事实也作为轻量经验页或表格段落写入 experience，不另建独立事实层。
- 用户自维护知识库：可作为外部原始知识源，例如仓库内 submodule、团队规范目录或业务知识库目录。`experience/` 可以引用、摘要和记录这些知识的使用结果，但不应默认复制或改写其全文。

关键不变量：`Clippings/` 记录“发生过什么”；用户知识库记录“团队已经维护的业务知识”；`experience/` 记录“这些知识如何被 SDT 理解、路由、验证并复用”。

## 读取 `.sdt_preference.yaml`

所有可迁移配置一律读取仓库根目录下的 `.sdt_preference.yaml`。进入 Bootstrap、Query、Ingest 或 Lint 前：

1. 使用 git 仓库根目录定位 `.sdt_preference.yaml`。
2. Query / Ingest 前先运行轻量初始化检查，不要由模型手工扫描目录判断：

   ```bash
   python3 $SKILL_DIR/scripts/validate_learning_repo.py --repo-root <repo-root> --check initialized --json
   ```

3. 按脚本 JSON 结果处理：
   - `initialized: true`：继续原操作。
   - `initialized: false` 且 `bootstrap_required: true`：自动执行一次脚本 Bootstrap，再重试原操作。
   - `initialized: false` 且 `bootstrap_required: false`：停止并提示用户修复脚本返回的配置或结构错误。
4. 自动 Bootstrap 固定使用脚本，不要由模型手工创建文件：

   ```bash
   python3 $SKILL_DIR/scripts/bootstrap_learning_repo.py --repo-root <repo-root> --json
   ```

5. 如果文件不存在，Bootstrap 脚本会创建；Query / Ingest / Lint 只有在轻量检查要求 Bootstrap 时才自动 Bootstrap。
   - 自动 Bootstrap 成功后，不要把“经验库未初始化”视为缺失经验；继续读取新建的空索引或写入 clipping。
   - 只有 Bootstrap 失败（无写权限、仓库根目录无法定位、配置冲突等）时，才提示用户手动初始化或提供配置。
6. 从 `sdt_learning` 读取 `learning_root`、`record_query_usage`、`query_usage_confirmation` 等配置。
7. 从 `sdt_learning.external_sources.<name>` 读取用户知识库或 submodule 配置。

不要再依赖环境变量或隐式目录作为可迁移配置来源；临时参数只能覆盖本次执行，不能替代 `.sdt_preference.yaml`。

## 操作类型

根据 `$ARGUMENTS` 判断操作：

- **Query**：默认使用纯 Query 模式。在 `adk-sdt-ff`、`adk-sdt-clarify`、`adk-sdt-implement`、`qa-sdt-ff`、`qa-sdt-clarify` 或 `qa-sdt-implement` 前后读取 `experience/INDEX.md`，只选择 2-5 个最相关页面，总结当前 SDT 任务可复用的规则。
- **Record**：在一轮 SDT 结束时，统一记录本轮实际使用过的经验和最小使用日志；不要在每次 Query 后立即记录。
- **Ingest**：只在用户纠正、执行失败、测试用例 clarify 决策或高价值 SDT 回答之后沉淀新经验；普通命中不需要 Ingest。
- **Guided Ingest**：当用户主动要求“记住 / 学习 / 以后遇到要这样做”时，立即执行一轮简短 Ingest；不要等待 SDT 轮次收尾。
- **Lint**：按需检查学习仓库结构、死链、未编译 clipping 和明显矛盾规则；不作为每轮 SDT 默认动作。
- **Bootstrap**：在新仓库中初始化目录结构，并创建空的 index/log 文件。

执行 Query 时，如果轻量初始化检查通过，可直接读取索引；不需要为了 Bootstrap 细节预加载 `references/continuous-learning-workflow.md`。执行 Record / Ingest / Lint 或需要操作细节时，再读取 workflow。

## 纯 Query 模式

这是 SDT 前置检索的默认模式。除非用户明确要求 Ingest / Lint / Bootstrap / Record，否则 Query 必须按本节执行，不加载完整 workflow。

1. 运行轻量初始化检查；只有 `bootstrap_required: true` 时调用 Bootstrap 脚本一次。
2. 读取 `.sdt_preference.yaml` 得到 `learning_root`。
3. 读取 `experience/INDEX.md`。
4. 按当前 feature、PSM、页面/API、平台、测试类型、失败签名和即将询问的问题，从 `experience/indexes/` 选择 0-1 个最相关分片；有明确高频命中时可同时读 `indexes/hot.md`。
5. 从分片中选择 0-5 个候选经验页；无命中时立即返回“无相关经验”，不要继续扫描全库。
6. 只读取候选经验页，做最小适用性检查：`何时使用` 匹配、`status` 不是 `deprecated` / `needs-review`、未发现与当前 spec/code/task 冲突。
7. 返回紧凑指导和本轮待记录候选；不要写任何文件，不更新 hit count，不追加 log，不执行 Ingest。

### 纯 Query 检索预算

纯 Query 必须遵守以下预算；任何一项达到上限都必须停止检索并返回当前结果：

- 最多读取 1 个主索引：`experience/INDEX.md`。
- 最多读取 1 个普通分片索引；如果有明确高频信号，可额外读取 `indexes/hot.md`。
- 最多读取 5 个经验页。
- 最多读取 1 个外部知识源章节；只有被命中的经验页明确引用且当前任务确实需要时才读取。
- 不得使用 `Glob`、`rg`、shell 搜索或语义搜索扫描 `experience/articles/`、`experience/concepts/`、`experience/topics/`。
- 如果主索引和选中的分片索引没有命中，必须立即返回“无相关经验”，不得尝试其他分片。
- 如果候选经验页适用性验证失败，返回低置信或无相关经验，不得扩大搜索范围。

纯 Query 必须返回 `search_stopped_reason`：

- `matched`：找到并应用了相关经验。
- `no_index_match`：索引没有命中。
- `budget_exhausted`：达到读取预算。
- `not_applicable`：候选经验不适用于当前任务。
- `not_initialized`：经验库初始化失败或不可用。

纯 Query 输出必须包含：

- `learning_root`
- `read_pages`
- `search_stopped_reason`
- `guidance`
- `unresolved_conflicts`
- `round_record_candidate`：本轮结束时可记录的经验页和适用结果；如果无命中则为空。

## Guided Ingest

当用户主动要求学习经验时，走 Guided Ingest，而不是 SDT 轮次末尾的自动 Ingest。触发表述包括：“记住这个经验”、“学一下”、“以后遇到这种情况要...”、“把这条规则沉淀下来”。

执行规则：

1. 先判断学习类型：稳定事实、复用规则、流程/策略、失败复盘、clarify 决策或领域规则。
2. 向用户给出一句简短确认：将记录为什么类型、触发条件是什么、目标页面是什么。用户已经明确要求立即记录且内容无歧义时，可以直接执行。
3. 写入内容保持最小：`何时使用`、`规则`、`对 SDT 的影响`、`证据`、`status`。
4. 不把一句用户纠正扩写成大型知识文章；无法确定适用范围时，标记为 `candidate` 并写清不确定性。
5. 如果只是稳定事实，写成 experience 中的轻量表格段落，不新建独立事实层。

Guided Ingest 可以立即写入 `Clippings/`、目标 `experience/` 页面、相关索引和 `log.md`。它仍必须遵守写入边界：默认只写 `learning_root`，不回写外部知识库。

## SDT 轮次学习契约

一轮 SDT 指从进入 `adk-sdt` / `qa-sdt` 或某个阶段 skill，到本次 case 生成、clarify 或 implement/report 结束为止。

- 轮次中可以多次执行纯 Query，但只能累积 `round_record_candidate` 和高价值学习信号，不能即时 Record 或 Ingest。
- 轮次结束时执行一次轻量学习收尾：Record 本轮实际使用过的经验；只有存在用户纠正、测试失败、clarify 决策或报告洞察时才 Ingest。
- 轮次收尾完成后，如果本轮发生过 Record 或 Ingest，检查 `experience/maintenance.json`；`last_linted_at` 为空或超过 30 天时，自动执行一次 Lint 并更新维护状态。
- 纯 Query 永远不触发 Lint，即使 Lint 已超过 30 天。
- 如果上下文很长，阶段 skill 必须在最终总结前显式检查“SDT 轮次学习收尾是否完成”。未完成时先执行收尾，再给最终答复。
- 用户明确拒绝记录、纠正经验不适用，或当前上下文未实际使用该经验时，不要记录成功使用；可以记录为 `not_applicable`、`correction` 或跳过。

## SDT 集成规则

- 在 SDT 运行中，将本 skill 作为上下文提供者，而不是替代 `adk-sdt`、`adk-sdt-ff`、`adk-sdt-clarify`、`adk-sdt-implement`、`qa-sdt-ff`、`qa-sdt-clarify` 或 `qa-sdt-implement`。
- 对普通用户隐藏内部 skill 选择逻辑。对外呈现业务规则和测试洞察，不暴露内部路由细节。
- 当重复 SDT 失败说明缺少可复用能力时，优先提出小而明确的 skill/profile 更新，不要把宽泛规则塞进每个 SDT prompt。
- 优先使用新鲜、有证据引用的经验，而不是泛泛启发式。如果经验与当前 spec/code 冲突，标记冲突并先提问或调查，不要直接套用。
- `experience/INDEX.md` 只作为经验路由入口，不作为全量知识库。Query 时先通过主索引定位分片索引，再从分片中选择少量经验页。
- `hot.md` 是可选的人工/维护入口，不参与复杂排名；经验命中后仍必须检查适用条件、证据、状态和当前 spec/code。
- 用户知识库与 `experience/` 是补充关系，不是排他关系。Query 优先读取 `experience/` overlay；只有命中的经验页明确引用且当前任务需要时，才读取 1 个对应外部章节。
- 外部知识源默认只读。Ingest 默认只能写 `learning_root` 下的 `Clippings/` 与 `experience/`；除非用户明确要求并再次确认，否则不要写入用户知识库或 submodule。

## 输出要求

Query 操作需返回：

- 使用的经验库根目录。
- 读取过的相关经验页。
- 检索停止原因：`search_stopped_reason`。
- 面向当前 feature 或测试任务的可复用 SDT 指导。
- 未解决的决策项或冲突。
- 本轮结束时的 `round_record_candidate`；不要在 Query 后立即写入。

Record 操作需返回：

- 本轮实际使用过的经验页。
- 每页记录的适用结果：`successful_application`、`correction`、`conflict` 或 `not_applicable`。
- 更新过的 `last_used_at` / `successful_applications`（如存在）和 `log.md` 内容。

Ingest 操作需返回：

- 新增 clipping 路径。
- 更新过的 `experience/` 页面。
- `INDEX.md` 和 `log.md` 的更新内容。
- 需要人工裁决的事项。

Lint 操作需返回：

- 按严重程度分组的问题。
- 建议修复方式。
- 已更新的文件（如有）。

校验脚本：

```bash
python3 $SKILL_DIR/scripts/validate_learning_repo.py --repo-root <repo-root>
python3 $SKILL_DIR/scripts/validate_learning_repo.py --repo-root <repo-root> --check initialized --json
python3 $SKILL_DIR/scripts/bootstrap_learning_repo.py --repo-root <repo-root> --json
```
