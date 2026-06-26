# Testing — EXEC_MAGICA

## Purpose
Prove the correctness of the pure Logic Layer **before** any decision-making
algorithm is measured on top of it. Without a green core, comparing models
(Random, Greedy, MCTS, ML) has no scientific value: it is impossible to tell
whether an observed difference comes from the algorithm or from an engine bug.

## Framework
Unity Test Framework (NUnit 3), **EditMode** — the entire Logic Layer is tested
without the game loop.

Run in editor: `Window → General → Test Runner` → EditMode tab → Run All.

Run from CLI:
\`\`\`
Unity.exe -runTests -batchmode -projectPath . -testPlatform EditMode -testResults results.xml -logFile -
\`\`\`

## Assembly layout
- `LogicLayer.asmdef` — core production code (references UnityEngine).
- `LogicLayer.Tests.EditMode.asmdef` — tests; references LogicLayer.

Tests live in `Assets/_Project/Tests/EditMode/`, mirroring the layer structure.

## Naming convention
`Method_Scenario_ExpectedResult`, e.g.
`ApplyAction_AttackHeroWhileEnemyHasProvocation_ReturnsFailure`.
A test name should read as a behavioural specification.

## Coverage plan
| Tier | Target | Status |
|------|--------|--------|
| 0 | `GameState.GetDeepCopy()` — copy independence (critical for MCTS) | ✅ 5/5 passing |
| 1 | `GameEngine.ApplyAction` — every action type: happy path + rejections | ✅ 16/16 passing |
| 2 | `EffectEngine` — every EffectType + ResolveDeaths + OnDeath chains | ✅ 18/18 passing |
| 3 | Keywords: Provocation / Shield / Charge / Rush / DoubleAttack / Lifesteal | ✅ 9/9 passing |
| 4 | `CoreLegalActionGenerator` — all legal actions present, no illegal ones | ✅ 8/8 passing |
| 5 | `GameRules` / turn flow: mana 1→10, clamp, draw, counters | ✅ 5/5 passing |
| 6 | Full-playout determinism with a fixed seed | ✅ 3/3 passing |

## Test data
`GameStateTestFactory` (LogicLayer/Debug) builds controlled scenarios and is the
foundation for most tests.

## Known invariants under test
- `GameState.CardDatabase` is intentionally shared by reference across copies
  (immutable reference data) — see `GetDeepCopy_CardDatabaseIsSharedByReference`.
- `CardEffect` instances are shared by reference inside a card's `Effects` list
  but are treated as immutable; the list itself is copied. If any effect ever
  mutates its own fields at runtime, `CardInstance.GetDeepCopy()` must perform a
  per-element deep copy of `Effects`.

## Git / Unity notes
- Every `.asmdef` and `.cs` has a `.meta` file with a GUID — always commit it.
- `Library/`, `Temp/`, `obj/` are git-ignored.

## Results log
- 2026-06-13 — Tier 0 complete: `GameStateDeepCopyTests` (5 tests) green in EditMode.
  Verifies object-graph independence, copy↔original isolation, `Effects` list
  independence, and intentional `CardDatabase` reference sharing.
- 2026-06-13 — Tier 1 complete: `GameEngine.ApplyAction` covered for AttackCard (4),
  AttackHero (4), EndTurn (6) and PlayCard (6) — happy paths, rejections, lethal,
  Provocation/Rush blocking, mana spend/restore/growth and card draw.
- 2026-06-13 — Tier 2 complete: EffectEngine covered — DealDamage/Heal (5),
  Buff/Debuff/AddKeyword (6), Destroy/Silence incl. OnDeath interaction (4),
  Summon/DrawCards/OnDeath chains (3).
- 2026-06-13 — Tier 3 complete: keyword mechanics covered — Shield consumes the
  first hit (combat + spell) (2), Lifesteal heals the owner on card/hero attack
  and is suppressed by a target's Shield (3), DoubleAttack allows two attacks and
  refreshes next turn, Charge hits the hero same turn, Rush hits a card same turn (4).
- 2026-06-13 — Tier 4 complete: CoreLegalActionGenerator covered — exactly one
  EndTurn always offered; empty set on game over; ready attacker offers AttackCard
  and AttackHero; enemy Provocation restricts targets and blocks the hero; Rush
  attacks cards but not the hero; targeted spell offers play on an enemy card but
  not the hero; unaffordable cards are omitted; exhausted board with empty hand
  yields only EndTurn.
- 2026-06-13 — Tier 5 complete: GameRules constants frozen, deck-size validation,
  mana pool clamped to max across turns, empty-deck draw is a safe no-op (3+2 tests).
- 2026-06-13 — Refactor: AllCards.Shuffle now uses System.Random; the Logic Layer
  is free of UnityEngine.Random (the GameState simulation path was already seedable).
- 2026-06-13 — Tier 6 complete: seeded random playouts are reproducible (identical
  scalar outcome and board composition for a fixed seed) and the runner does not
  mutate the caller's initial state (3 tests).
- 2026-06-13 — Phase 1.2 complete: 64 EditMode tests green across GetDeepCopy,
  GameEngine, EffectEngine, keywords, legal action generation, game rules and
  full-playout determinism
- 2026-06-13 — Phase 2.1: GreedyActionPolicy (1-ply lookahead heuristic) baseline gate.
  Greedy vs Random on a synthetic mirror deck, 100 games, sides and starting player
  alternated, fixed seeds: 95.0% win rate (95/100, all decisive). Acceptance (>70%) met.
- 2026-06-14 — Phase 2.2: ISMCTS policy (MctsActionPolicy) — action-keyed tree, UCB1 with
  availability counts, root-perspective value with per-node chooser flip, per-iteration
  determinization (opponent hand/deck resampled, public zones excluded), configurable
  budget/rollout/parallelism. EditMode gate: MCTS (perfect-info, Greedy rollout) beats
  Random. Root parallelization benchmarked on 28 cores: NEGATIVE scaling (x0.19 at 28
  threads) — allocation-bound workload + Unity Mono/Boehm GC; disabled by default. Formal
  win rate vs Greedy + iterations curve deferred to Phase 3.1.
- 2026-06-14 — Phase 1.1a fatigue rule: empty-deck draw deals escalating self-hero damage
  (1, 2, 3, ...), can be lethal, FatigueCounter survives GetDeepCopy; FatigueTests (4) green;
  Tier-5 empty-deck test updated from "no-op" to fatigue. AI-vs-AI deckout games now
  terminate naturally instead of hitting MaxActionsReached.
- 2026-06-14 — Phase 2.2 / 3.1 milestone (batch runner): MCTS (ISMCTS, determinize=true,
  400 iters, Greedy rollout) vs Greedy 81% CI[72,87]; vs Random 98% CI[93,99]; Greedy vs
  Random 83% CI[74,89]. 100 games each, Midrange mirror, alternating start. Ordering
  MCTS > Greedy > Random, all significant. Cost: MCTS ~1.3-1.5 s/decision vs Greedy ~0.2 ms.
- 2026-06-14 — Batch parallelism finding: game-level thread parallelism (MaxParallelGames)
  is config-gated, default 1. Greedy vs Random, 200 games, 28 cores: sequential 952 ms vs
  parallel(28) 7649 ms — ~8x SLOWER. Outcomes identical (194/6) -> correct & deterministic;
  think-time inflated ~200x. Same Mono/Boehm GC contention on deepcopy-heavy workloads as
  intra-MCTS parallelism (Greedy/MCTS are allocation-bound). Scaling path = process-level
  parallelism (separate batchmode processes, merge index.jsonl).