# ADK SDT Learning

`adk-sdt-learning` 是面向 SDT 测试流程的持续学习 skill。它帮助 `adk-sdt` / `qa-sdt` 在多次使用后沉淀业务测试经验，让后续的 case 生成、用例澄清和测试执行能复用已确认的业务规则、失败模式和测试策略。

这套能力来自 **SDT Continuous Learning Engineering** 的设计：SDT 不应该每次都像新人一样从零读 spec、读代码、推导测试策略，而应该能像资深测试同学一样，把历史纠正、业务规则、执行失败和组织经验持续沉淀下来。

适用阶段：

- `adk-sdt`
- `adk-sdt-ff`
- `adk-sdt-clarify`
- `adk-sdt-implement`
- `qa-sdt-ff`
- `qa-sdt-clarify`
- `qa-sdt-implement`

它只提供知识召回、经验沉淀和知识库治理，不替代 SDT 阶段 skill 本身。

## 为什么需要

SDT 的核心不是“会读代码”，而是能模拟资深测试工程师的决策过程。资深测试同学真正有价值的地方，往往是长期积累的业务规则、历史坑位、环境约束、数据准备经验和失败诊断经验。

如果没有持续学习机制，SDT 每次运行都会重新从 spec 和代码中推导，效果很难随着使用次数增长。`adk-sdt-learning` 要解决的是：

- SDT 第一次使用时只能依赖模型能力和用户输入。
- SDT 使用 100 次、1000 次后，即使模型没升级，也应该因为组织经验沉淀而变得更好。
- 横向测试团队沉淀下来的经验，应能低成本复用到不同业务方仓库，而不是散落在聊天记录、报告和个人脑子里。

## 组织语境

在这套设计里，**Central Spec Repo** 不只是 spec 仓库，而是 SDT 的 **Capability OS（能力操作系统）**：它承载测试流程、业务知识、经验索引和能力演进入口。

`adk-sdt-learning` 是这个 Capability OS 中的 **Business Rule Capture System（业务规则捕获系统）**。它把原本只存在于测试同学脑中的经验，通过用户纠正、Clarify、测试执行和报告回流，持续沉淀为可检索、可验证、可治理的 SDT 经验。

对横向测试团队来说，它的价值是：

- 把“业务同学纠正过的规则”变成下次 SDT 可复用的经验。
- 把“测试失败后才知道的坑”变成后续 case 生成和执行前的检查项。
- 把“团队知识库里的业务规则”编译成 SDT 可执行摘要，而不是让 agent 每次全量阅读。
- 把“是否真的有效”交给用户确认和执行结果反馈，而不是只靠命中次数。

## 名词解释

- **SDT**：Spec Driven Testing，围绕需求文档生成、澄清和执行测试的流程。
- **FF / Fast Forward**：从 spec 快速生成测试分析、测试用例和执行任务的阶段。
- **Clarify**：对测试用例进行复核、澄清和修正的阶段。
- **Implement**：按 `test/task.md` 执行测试、更新进度并生成报告的阶段。
- **Capability OS**：以 Central Spec Repo 为核心的能力操作系统，承载流程、知识、模板、经验和 skill 演进。
- **Business Rule Capture System**：业务规则捕获系统，用低成本方式把用户纠正、测试结论和业务规则沉淀成 SDT 可复用经验。
- **Clippings**：原始证据层，只追加，不改写，记录用户纠正、失败报告、clarify 结论等一手证据。
- **experience**：SDT overlay 学习层，把 clippings 和用户知识库编译成可检索、可验证、可复用的经验。稳定事实也作为轻量经验页或表格段落写入 experience，不另建独立事实层。
- **Query / Ingest / Lint**：持续学习的三类操作，分别负责轻量召回、少量沉淀和按需维护。
- **hot**：可选的优先召回入口，由维护者或明确反馈手动维护，不做默认自动排名。

## 能力概览

- **Query**：在 SDT 执行前或执行中，用固定预算召回少量相关经验，作为测试设计和执行上下文。
- **Ingest**：只在用户纠正、clarify 决策、测试失败、报告分析或高价值诊断后，把经验沉淀到学习库。
- **Lint**：按需检查学习库结构、索引膨胀、死链和明显矛盾。
- **Bootstrap**：初始化 `.sdt_preference.yaml` 和 learning repository 目录结构。

## 核心机制

这套 learning 不是另建一个封闭知识库，而是“原始知识源 + SDT overlay”：

```text
用户知识库 / submodule / 团队规范
  -> 原始业务知识，由业务方或团队维护

Clippings/
  -> 用户纠正、失败报告、clarify 结论等原始证据

experience/
  -> 面向 SDT 的可执行摘要、索引、适用条件和使用验证记录
```

关键原则：

- `Clippings/` 记录“发生过什么”，只追加，不改写。
- 用户知识库记录“团队已经维护的业务知识”，默认只读。
- `experience/` 记录“这些知识如何被 SDT 理解、路由、验证并复用”。
- `INDEX.md` 只是路由入口，不承载全量经验；全量索引拆到 `experience/indexes/`。
- `hot.md` 是可选优先入口；新场景使用任何经验前仍要检查适用条件、证据、新鲜度和当前 spec/code。

## 设计亮点

- **低成本捕获经验**：不要求业务方先整理完整知识库，用户纠正、测试失败、clarify 结论都可以成为 clipping。
- **原始事实和结构化经验分离**：`Clippings/` 保留事实，`experience/` 负责综合，避免经验被改写后失去证据来源。
- **兼容用户自维护知识库**：业务方可以通过 submodule 或目录提供知识库，SDT learning 只记录路径、章节、commit 和 SDT 摘要，默认不复制全文、不改写原库。
- **主索引不膨胀**：`INDEX.md` 只作为路由入口，分片索引放在 `experience/indexes/`，避免越用越大。
- **轻量状态治理**：保留 `candidate | active | trusted | needs-review | deprecated`，但不做复杂热度排名或 30/7 天计数。
- **横向可迁移配置**：所有业务方差异通过 `.sdt_preference.yaml` 表达，方便横向团队交付和排查。
- **可执行校验**：提供 `validate_learning_repo.py` 检查结构、索引、配置和状态字段。

## 配置文件

所有可迁移配置都放在仓库根目录的 `.sdt_preference.yaml`：

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

字段说明：

- `learning_root`：SDT learning 可写根目录，推荐 `.experience`。
- `record_query_usage`：Query 使用记录策略，推荐 `confirm`。
- `query_usage_confirmation`：SDT 轮次结束前是否告知用户将记录本轮 Query 使用结果，推荐 `true`。用户纠正或拒绝时不写；用户未纠正并进入轮次收尾时，视为默认同意并自动记录。
- `default_write_policy`：默认写入边界，推荐 `learning-root-only`。
- `external_sources`：用户自维护知识库、submodule 或团队规范目录，默认只读。

## 目录结构

初始化后建议结构：

```text
.experience/
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

`INDEX.md` 长期目标是 100 行以内。超过 150 行需要预警，超过 300 行视为结构违规。任何时候只要 `INDEX.md` 开始承载全量经验条目，都应迁移到分片索引。

## 使用方式

### 1. 初始化

通常无需用户手动初始化。`adk-sdt` / `qa-sdt` 调用 Query 或 Ingest 时，先通过轻量脚本判断 learning repository 是否已初始化：

```bash
python3 plugins/ttadk/core/skills/adk-sdt-learning/scripts/validate_learning_repo.py --repo-root . --check initialized --json
```

只有脚本返回 `bootstrap_required: true` 时，才自动执行幂等 Bootstrap 脚本，创建缺失的 `.sdt_preference.yaml` 和 `.experience/` 结构：

```bash
python3 plugins/ttadk/core/skills/adk-sdt-learning/scripts/bootstrap_learning_repo.py --repo-root . --json
```

Bootstrap 脚本只补缺失文件，已有配置、索引和经验页不会被覆盖。

只有 `adk-sdt-learning` skill 未安装时，SDT 才会跳过经验查询并继续原流程；如果 Bootstrap 因权限、仓库根目录无法定位或配置冲突失败，应先修复初始化问题再继续。

### 2. SDT 前召回经验

在运行 `adk-sdt-ff` / `qa-sdt-ff` 等阶段前，使用纯 Query 读取当前 feature 相关经验。纯 Query 不执行完整 workflow，不写文件，只会先读 `experience/INDEX.md`，再读 0-1 个分片索引，最后只读取少量相关经验页。没有命中时立即返回，不扫描全库。

纯 Query 有固定检索预算：最多 1 个主索引、1 个普通分片、可选 `hot.md`、5 个经验页，以及 1 个必要的外部知识章节。达到预算、索引无命中或候选经验不适用时，Query 必须停止并返回 `search_stopped_reason`，不得通过 `Glob`、`rg`、shell 搜索或语义搜索扫描整个 `experience/`。

Query 返回的 `round_record_candidate` 只在本轮 SDT 结束时使用，不在 Query 后立即更新命中或日志。

### 3. SDT 轮次结束时记录使用结果

一轮 SDT 结束前，统一执行一次学习收尾。若本轮实际使用了经验，按 `record_query_usage` 决定是否 Record：`confirm` 模式下 agent 会展示准备更新的经验页、计数字段和日志内容。用户纠正、拒绝或要求调整时不会自动写；如果用户没有纠正并继续到本轮收尾，则视为默认同意，agent 会自动记录命中和应用结果。

如果本轮发生过 Record 或 Ingest，收尾后检查 `experience/maintenance.json`。`last_linted_at` 为空或超过 30 天时，自动执行一次 Lint 并更新维护状态。纯 Query 不检查也不触发 Lint。

### 4. 沉淀新经验

当用户纠正 case、clarify 得出关键结论、测试执行暴露失败模式，或报告中出现可复用诊断时，不在轮次中途立即 Ingest；先作为本轮学习信号暂存，轮次结束时统一 Ingest：

- 新增 `Clippings/` 原始证据。
- 更新或创建 `experience/articles/`、`experience/concepts/`、`experience/topics/`。
- 更新相关分片索引和 `log.md`。

如果用户主动要求“记住这个经验 / 学一下 / 以后遇到这种情况要...”，使用 Guided Ingest：立即判断记录类型，简短确认触发条件和目标页面，然后写入最小经验内容。主动学习不需要等到 SDT 轮次结束，但仍只写 `learning_root`，不回写外部知识库。

### 5. 定期校验

使用内置脚本检查 learning repository：

```bash
python3 plugins/ttadk/core/skills/adk-sdt-learning/scripts/validate_learning_repo.py --repo-root .
python3 plugins/ttadk/core/skills/adk-sdt-learning/scripts/validate_learning_repo.py --repo-root . --check initialized --json
python3 plugins/ttadk/core/skills/adk-sdt-learning/scripts/bootstrap_learning_repo.py --repo-root . --json
```

脚本会检查：

- `.sdt_preference.yaml` 是否存在且配置合法。
- `learning_root` 目录结构是否完整。
- `maintenance.json` 是否存在且是合法 JSON。
- `INDEX.md` 是否膨胀或承载全量条目。
- 分片索引是否过大。
- 经验页是否缺少元数据、证据、新鲜度或 `status`。
- 是否仍有废弃的 `confidence` 字段。
- 外部知识源索引中记录的路径是否存在。

## 写入安全

默认只允许写入 `learning_root` 下的 `Clippings/`、`experience/` 和 `log.md`。

外部知识源默认只读。轻量模式下不自动回写用户知识库或 submodule；如果确实需要，把建议记录到 `pending-decisions.md`，由用户或维护者后续处理。

## 状态模型

经验页统一使用 `status` 字段：

```text
candidate -> active -> trusted
              ↓        ↓
          needs-review -> deprecated
```

- `candidate`：新沉淀经验。
- `active`：成功应用过且当前仍可用。
- `trusted`：跨 feature 多次验证，低纠错，仍然有效。
- `needs-review`：被用户强纠正，或多次命中但效果不稳定。
- `deprecated`：被当前 spec/code 证明过期。

`hot.md` 只是可选优先入口。后续如果被纠正或证明过期，应标记为 `deprecated` 并移出高频列表。
