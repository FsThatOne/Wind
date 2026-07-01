# 《风止》 — Codex 协作配置

《风止》（FengZhi）是一款 2D 像素武侠 RPG，由个人 + 一组协调的 Codex subagent
共同开发。每个 agent 拥有专属领域，强制分层与质量门控。项目概览见 [README.md](README.md)。

## Technology Stack

- **Engine**: Godot 4.7-stable
- **Language**: C# (.NET 8+, primary), C++ via GDExtension (native plugins only)
- **Version Control**: Git with trunk-based development
- **Build System**: .NET SDK + Godot Export Templates
- **Asset Pipeline**: Godot Import System + custom resource pipeline

> **Note**: Engine-specialist agents exist for Godot, Unity, and Unreal with
> dedicated sub-specialists. Use the set matching your engine.

## Project Structure

@.Codex/docs/directory-structure.md

## Engine Version Reference

@docs/engine-reference/godot/VERSION.md

## Technical Preferences

@.Codex/docs/technical-preferences.md

## Coordination Rules

@.Codex/docs/coordination-rules.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question -> Options -> Decision -> Draft -> Approval**

- Agents MUST ask "May I write this to [filepath]?" before using Write/Edit tools
- Agents MUST show drafts or summaries before requesting approval
- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for full protocol and examples.

> **First session?** If the project has no engine configured and no game concept,
> run `/start` to begin the guided onboarding flow.

## Coding Standards

@.Codex/docs/coding-standards.md

## Context Management

@.Codex/docs/context-management.md

## Must Follow
1. 用中文和我沟通, 用中文编写注释, 用中文编写文档.
2. 随时随地可以跟我头脑风暴(/brainstorm), 我会根据你的建议进行调整.
3. 问我问题时, 选项一定要有一个你推荐的选项, 我会根据你的推荐进行判断. 最好是能简单说明推荐原因.
4. 每次推进任务前（包括设计、实现、修复、盘点、可行性判断和继续开发）, 必须先查阅并对照相关 GDD（design/gdd/）、ADR（docs/architecture/）以及 design/、docs/、production/ 中的相关设计/规格/证据文档（如 quick-specs、ux specs、engine-reference、systems-index、story、QA evidence、session-state 等）。必须先说明或内化这些文档给出的约束，再继续方案或代码；不可凭记忆、当前实现或临时判断跳过设计对照.
5. 优先使用 claude-code-game-studio 提供的 skill 和 agent 来完成任务（如 /design-system, /create-architecture, /dev-story, /code-review 等）, 充分利用工作流工具链而非纯手工操作.
