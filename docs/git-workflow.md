# Git Workflow & Commit Convention（权威源）

> **Status**: Active — Sprint 6 起生效
> **Owner**: 全体 contributor
> **关系**：本文件是 commit 规范的**权威源**。`CONTRIBUTING.md §Commit Format` 是简化版，与本文有出入时以本文为准。
> **配套**：`.gitmessage`（模板）、`scripts/git/commit-msg-lint.sh`（手动 lint）、`scripts/git/tests/commit-msg-lint-smoke.sh`（smoke）

---

## 1. 适用范围

本规范适用于本仓库所有 git commit。Sprint 6 起所有新 PR 必须满足本规范；历史 commit 不追溯。

落地的 retro Action：
- Sprint 5 retrospective Action #4 — `阶段性提交` / `init(test):` / 空消息 / 仅 emoji 等 bad commit 反复出现 → 引入手动 lint。

---

## 2. Commit Message 格式

```
<type>(<scope>): <subject>

<body — optional>

<footer — optional>
```

- **首行（subject）必填**；其余可选。
- subject 与 body 之间必须留一空行（如有 body）。
- footer 用于 BREAKING CHANGE / TR-ID / Refs。

---

## 3. type 白名单（必须英文）

| type | 含义 |
|------|------|
| `feat` | 新功能 |
| `fix` | bug 修复 |
| `docs` | 文档（GDD / ADR / story / qa-plan / README ...） |
| `test` | 测试代码 |
| `chore` | 杂项（构建 / 依赖 / 工具 / 配置） |
| `refactor` | 不改外部行为的代码改动 |
| `perf` | 性能优化 |
| `build` | 构建系统 / 外部依赖 |
| `ci` | CI 配置 / 流水线 |

---

## 4. Lint Policy（用户决议 2026-06-18）

| 项 | 规则 | 行为 |
|---|---|---|
| type 关键字 | 必须英文（白名单 9 个） | 不匹配 → reject |
| scope | 选填；中文 / 英文均可 | 缺 scope → accept |
| description | 必填；中文 / 英文均可 | 空 → reject |
| subject 长度 | 推荐 ≤ 72 字符 | > 72 → warn but accept |
| subject 末尾标点 | 推荐无句号 | 句号结尾 → warn but accept |
| body | 自由（含 multi-line） | 不校验 |
| TR-ID 后缀 | 推荐 `(TR-xxx-NNN)` | 缺失 → accept（不强制） |
| 仅 emoji | subject 不含任何 ASCII 可见字符（emoji-only） | reject |
| 黑名单 | `阶段性提交` / `init(test):` | reject |

> **emoji 规则缺省（A1）**：本 sprint 仅拒**纯 emoji**；含 emoji + 文字（如 `feat(x): 🚀 cu-006`）accept。如需更严格走 follow-up issue。

---

## 5. 8 cases 表

### Accept

| # | message |
|---|---|
| 1 | `feat(combat-ui): cu-006 godot integration (TR-combat-ui-006)` |
| 2 | `feat(战斗UI): cu-006 godot 集成` |
| 3 | `fix: correct timescale stack` |
| 4 | `docs(adr-0011): 实机集成段落补全` |

### Reject

| # | message | 拒绝原因 |
|---|---|---|
| 5 | `阶段性提交` | 黑名单 |
| 6 | `init(test):` | 黑名单 + 缺 description |
| 7 | （空字符串） | 空 subject |
| 8 | `🚀` | 仅 emoji |

### Edge cases（warn but accept）

- multi-line body → accept
- subject > 72 char → accept + `[warn] subject length ... > 72`
- subject 句号结尾 → accept + `[warn] subject ends with period`

---

## 6. 启用 `.gitmessage` 模板

仓库本地一次性配置：

```bash
git config commit.template .gitmessage
```

之后 `git commit`（不带 `-m`）会自动展开模板，含 type 白名单、示例、规则速查。

> 该配置**不**写入 `.git/config` 全局；每个 contributor 在 clone 后各自启用。

---

## 7. 手动 Lint 调用

```bash
# 1. 直接传字符串
bash scripts/git/commit-msg-lint.sh -m "feat(x): hi"

# 2. 从 stdin（pipeline 友好）
git log -1 --pretty=%B | bash scripts/git/commit-msg-lint.sh

# 3. 从文件（git commit-msg hook 风格，方便日后挂 hook）
bash scripts/git/commit-msg-lint.sh /path/to/COMMIT_EDITMSG

# 4. Smoke test（本地一键验证脚本是否健康）
bash scripts/git/tests/commit-msg-lint-smoke.sh
```

Exit code：
- `0` — pass（含 warning）
- `1` — reject
- `2` — 用法错误

---

## 8. Out of Scope（本 sprint 不做）

- **不安装** `.git/hooks/commit-msg`（per `production/sprints/sprint-6.md` 风险表 line 63；hook 留待下一 sprint，届时直接挂上 `commit-msg-lint.sh` 即可，无需改脚本）。
- **不引入** husky / commitlint / lefthook / pre-commit / Node / Python 工具链。
- **不集成** CI pipeline 校验。
- **不追溯** 历史 commit。
- **不完全禁** emoji（仅拒纯 emoji；如需严格走 follow-up）。

---

## 9. References

- [`production/qa/qa-plan-sprint-6-2026-06-18.md` §S6-Commit-Lint](../production/qa/qa-plan-sprint-6-2026-06-18.md)
- [`production/sprints/sprint-6.md` L26 / L63](../production/sprints/sprint-6.md)
- [`CONTRIBUTING.md` §Commit Format（简化版，以本文为准）](../CONTRIBUTING.md)
- [Conventional Commits](https://www.conventionalcommits.org/)
- 跨平台脚本约束：[`CONTRIBUTING.md` L35-39](../CONTRIBUTING.md)
