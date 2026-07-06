# S9-002 Playtest Session Evidence

> Sprint: 9  
> Task: Playtest Session (硬性 deadline Day 3)  
> Date prepared: 2026-07-04  
> Status: Manual playtest pending  
> Owner: User / invited playtester

## Scope

本文件用于记录一次不少于 30 分钟的序章试玩。目标不是验收单个系统，而是确认当前序章垂直体验是否足够可试玩，并捕捉阻碍继续推进 Production 的体验问题。

## Current Automated Preflight

- `dotnet build feng-zhi/FengZhi.csproj` — PASS, 0 warning, 0 error
- `dotnet test tests/Foundation/Foundation.Tests.csproj` — PASS, 1884/1884
- `godot --headless --path feng-zhi --quit-after 8 res://scenes/sect_compound/SectCompound.tscn` — PASS
- `godot --headless --path feng-zhi --quit-after 8 res://scenes/test/DialogueSmokeTest.tscn` — PASS

## Required Manual Run

- Duration: at least 30 minutes
- Start: Main Menu → New Game
- Route: play naturally through chapter_00 as far as possible
- Must check:
  - ESC pause menu appears in the current camera viewport, not at a fixed world-map position
  - Opening dialogue and portrait presentation are readable
  - Objective HUD is helpful without feeling like a full quest log
  - Cave insight interactions are discoverable
  - Senior brother misunderstanding reads as “he suspects why only the player survived,” not identity/conspiracy
  - Misunderstanding tension signal and relief signal are perceptible through text
  - No P0/P1 blocker prevents continued play

## Result Template

- Playtester:
- Date:
- Duration:
- Build / branch:
- Reached point:
- Verdict: PASS / PASS WITH NOTES / FAIL

## Notes

- Positive:
  - TBD
- Confusing:
  - TBD
- Bugs:
  - TBD
- Top 3 fixes before next playtest:
  1. TBD
  2. TBD
  3. TBD

## Sign-off

Manual playtest is not yet complete. Sprint 9 can only close as full playtest complete after this section is filled with a real run result, or it must be explicitly marked `PROVE(tech-only)` in retro.
