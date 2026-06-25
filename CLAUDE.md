# 《风止》 — Claude Code 协作配置

《风止》（FengZhi）是一款 2D 像素武侠 RPG，由个人 + 一组协调的 Claude Code subagent
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

@.claude/docs/directory-structure.md

## Engine Version Reference

@docs/engine-reference/godot/VERSION.md

## Technical Preferences

@.claude/docs/technical-preferences.md

## Coordination Rules

@.claude/docs/coordination-rules.md

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

@.claude/docs/coding-standards.md

## Context Management

@.claude/docs/context-management.md

## Must Follow
1. 用中文和我沟通, 用中文编写注释, 用中文编写文档.
2. 随时随地可以跟我头脑风暴(/brainstorm), 我会根据你的建议进行调整.
3. 问我问题时, 选项一定要有一个你推荐的选项, 我会根据你的推荐进行判断. 最好是能简单说明推荐原因.
4. 做代码设计和实现时, 必须先查阅相关 GDD（design/gdd/）、ADR（docs/architecture/）以及 design/ 和 docs/ 目录下的其他相关文档（如 quick-specs、ux specs、engine-reference、systems-index 等）, 确保实现符合设计规约和架构约束. 不可跳过设计对照直接写代码.
5. 优先使用 claude-code-game-studio 提供的 skill 和 agent 来完成任务（如 /design-system, /create-architecture, /dev-story, /code-review 等）, 充分利用工作流工具链而非纯手工操作.