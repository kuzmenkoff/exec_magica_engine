# Testing — EXEC_MAGICA <!-- omit in toc -->

The engine ships with **77 automated unit tests** ([NUnit](https://nunit.org/) /
Unity Test Framework, EditMode). They run **headless** — no scene, no play mode — because the
whole `LogicLayer` is pure C#. Every test builds a controlled `GameState`, applies actions
through the real `GameEngine`, and asserts on the resulting state and `GameEvent` stream.

> Rules being tested: [RULES.md](RULES.md) · agents: [AGENTS.md](AGENTS.md) ·
> API: [live reference](https://kuzmenkoff.github.io/exec_magica_engine/).

## Contents <!-- omit in toc -->

- [Running the tests](#running-the-tests)
- [Test suite](#test-suite)
- [What the tests guarantee](#what-the-tests-guarantee)
- [Scope](#scope)

## Running the tests

**In the editor:** `Window → General → Test Runner → EditMode → Run All`.

**From the command line:**
```bash
Unity -batchmode -runTests -projectPath . -testPlatform EditMode -testResults results.xml -quit
```

The tests live in `Assets/_Project/Tests/EditMode` (assembly `LogicLayer.Tests.EditMode`,
which references only `LogicLayer`). No Unity Asset Store content or visual assets are needed.

## Test suite

| Area | Files | Tests | Covers |
|---|---|---:|---|
| **Legal actions** | `CoreLegalActionGeneratorTests` | 8 | Which actions are offered: mana and field-cap filtering, target requirements, Provocation gating, `EndTurn` always present. |
| **Effects** | `EffectEngineDamageHeal/BuffDebuff/DestroySilence/SummonDraw` | 18 | Each `EffectType` resolved by playing the carrier card: damage/heal (with max-HP clamp), buff/debuff, destroy, silence, summon, draw. |
| **Engine actions** | `GameEnginePlayCard/AttackCard/AttackHero/EndTurn` | 20 | Applying actions: mana spend, hand→field/graveyard moves, mutual combat damage and deaths, turn hand-off (mana ramp, refresh, draw). |
| **Keywords** | `KeywordAttack/Lifesteal/Shield` | 9 | Charge / Rush / DoubleAttack attack rules, Lifesteal healing, Shield absorption. |
| **Rules** | `FatigueTests`, `GameEngineTurnFlowTests`, `GameRulesTests` | 9 | Escalating (and lethal) fatigue, turn/mana-pool flow, rule constants and deck-size validity. |
| **State** | `GameStateDeepCopyTests` | 5 | `GetDeepCopy()` independence — mutating a copy never touches the original. |
| **Simulation** | `GameSimulationDeterminismTests` | 3 | Seeded playouts are reproducible and never mutate the caller's state. |
| **AI** | `GreedyVsRandomBaselineTests`, `MctsBaselineTests` | 3 | Greedy beats Random; ISMCTS beats Random (> 70% on a synthetic mirror match); MCTS decision-speed benchmark. |
| **Telemetry** | `BatchRunnerTests`, `SessionWriterTest` | 2 | Batch summary aggregation; run output (`summary.json` / `sessions.jsonl` / `index.jsonl`). |
| | **Total** | **77** | |

## What the tests guarantee

- **Rules correctness** — every keyword, effect, combat and turn rule has a dedicated
  scenario (e.g. *Rush can't hit the hero*, *Shield absorbs one hit*, *Silence removes
  OnDeath*, *fatigue can be lethal*).
- **Reproducibility** — a fixed seed yields a byte-identical playout (same winner, board and
  instance ids), the prerequisite for the reproducible AI ladder.
- **State isolation** — deep copies are fully independent, so AI search and self-play can
  branch the state safely without corrupting the live game.
- **AI sanity floor** — search and heuristic agents are gated against Random, so a regression
  that weakens an agent fails the build.
- **Telemetry integrity** — batch aggregation and run serialization match the documented
  [data format](DATA_FORMAT.md).

## Scope

These tests cover the **headless engine** (`LogicLayer`). The visual client — the playable
reference game on [itch.io](https://mizantrop4real.itch.io/exec-magica) — is **not part of
this repository** and is verified through manual play-testing.