# Rating Ladder — EXEC_MAGICA <!-- omit in toc -->

Head-to-head strength of the frozen agents at a common **1 s/move** budget. Agent configs →
[MODEL_TUNING.md](MODEL_TUNING.md); rating method → [METRICS.md](METRICS.md); per-generation strength →
[GENERATIONS.md](GENERATIONS.md). Numbers below are the final run (card-set `2026-06-29`), reproduced
verbatim by the command at the bottom.

## Table of Contents <!-- omit in toc -->
- [What it measures](#what-it-measures)
- [Conditions](#conditions)
- [Benchmark environment](#benchmark-environment)
- [Standings](#standings)
- [Matchup matrix (row score % vs column)](#matchup-matrix-row-score--vs-column)
- [Regenerating](#regenerating)


## What it measures
Strength at a **common time budget of 1 s/move** (`Budget.Time`). Every agent gets the same wall-clock
per decision; the `iters/move` column reports how much search each actually did in that second.

## Conditions
6 decks (AggroPreset, ControlPreset, MidrangePreset, RandomDeck, RandomPreset, TokenPreset) ·
100 games/pair/deck · alternating start · mirror matchups · fatigue-on · **Bradley–Terry → Elo**,
`random` anchored at 0, bootstrap 95% CI. No intransitivity — a clean strength hierarchy.

## Benchmark environment
Time-matched results are **hardware-dependent** — on other hardware, 1 s/move buys proportionally
more/fewer iterations, so the machine-anchored quantity is **`iters/move`**. Inference is **CPU-only**
(hand-written C# forward pass, no GPU).

| | reference machine |
|---|---|
| CPU | **Intel Core i7-14700KF 3.4GHz/33MB** — 28 logical cores |
| RAM | **32 GB** (no swapping — game states are small) |
| OS / runtime | Windows 10 Pro (10.0.19045) · .NET 8.0.29 · server GC on |

## Standings
*anchor `random` = 0 · 6 600 games/model · think/move ≈ 0.78 s (forced 1-move states counted at 0 ms)*

| rank | agent | Elo | 95% CI | win% | iters/move | think/move |
|---|---|---|---|---|---|---|
| 1 | nnmcts-gen3 | **877** | 852–903 | 78.5 | 8 543 | 779 ms |
| 2 | nnmcts-gen2 | 860 | 832–884 | 76.9 | 7 102 | 779 ms |
| 3 | nnmcts-gen1 | 856 | 828–883 | 76.6 | 9 053 | 784 ms |
| 4 | nnmcts-gen0 | 794 | 767–820 | 70.2 | 8 461 | 775 ms |
| 5 | mcts-random | 741 | 718–767 | 64.5 | 14 678 | 766 ms |
| 6 | mcts-greedy | 722 | 697–746 | 62.3 | 2 420 | 785 ms |
| 7 | nn-gen3 | 501 | 477–524 | 38.0 | — | <1 ms |
| 8 | nn-gen2 | 497 | 470–521 | 37.5 | — | <1 ms |
| 9 | greedy | 492 | 467–519 | 37.1 | — | <1 ms |
| 10 | nn-gen1 | 459 | 433–486 | 33.7 | — | <1 ms |
| 11 | nn-gen0 | 314 | 291–339 | 20.8 | — | <1 ms |
| 12 | random | 0 | 0–0 | 3.9 | — | <1 ms |

**Tiers:** NN+MCTS (gen0–3) ≫ plain MCTS (Random ≈ Greedy rollout) ≫ standalone NN / Greedy ≫ Random.
The four NN+MCTS generations show a big first-generation jump then a plateau (gen1–3 within ~20 Elo);
the standalone policy crosses tuned Greedy at gen2.

## Matchup matrix (row score % vs column)
| vs | nnmcts-gen3 | nnmcts-gen2 | nnmcts-gen1 | nnmcts-gen0 | mcts-random | mcts-greedy | nn-gen3 | nn-gen2 | greedy | nn-gen1 | nn-gen0 | random |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| nnmcts-gen3 | — | 53 | 55 | 62 | 69 | 70 | 90 | 88 | 90 | 90 | 96 | 99 |
| nnmcts-gen2 | 47 | — | 51 | 58 | 66 | 70 | 89 | 89 | 90 | 91 | 95 | 99 |
| nnmcts-gen1 | 45 | 49 | — | 60 | 66 | 70 | 88 | 87 | 90 | 91 | 97 | 99 |
| nnmcts-gen0 | 38 | 42 | 40 | — | 60 | 62 | 85 | 84 | 82 | 86 | 94 | 99 |
| mcts-random | 31 | 34 | 34 | 40 | — | 55 | 82 | 80 | 81 | 81 | 91 | 100 |
| mcts-greedy | 30 | 30 | 30 | 38 | 45 | — | 77 | 80 | 86 | 80 | 92 | 99 |
| nn-gen3 | 10 | 11 | 12 | 15 | 18 | 23 | — | 53 | 50 | 57 | 74 | 95 |
| nn-gen2 | 12 | 11 | 13 | 17 | 20 | 20 | 47 | — | 49 | 56 | 74 | 95 |
| greedy | 10 | 10 | 10 | 18 | 19 | 14 | 50 | 51 | — | 53 | 76 | 96 |
| nn-gen1 | 10 | 9 | 9 | 14 | 19 | 20 | 43 | 44 | 47 | — | 65 | 90 |
| nn-gen0 | 4 | 5 | 3 | 6 | 9 | 7 | 26 | 26 | 24 | 35 | — | 86 |
| random | 1 | 1 | 1 | 1 | 0 | 1 | 5 | 5 | 4 | 10 | 14 | — |

## Regenerating
```bash
cd bench && dotnet run -c Release -- ladder.json
```
Append-only (`Runs/Ladder/matchups.jsonl`) with skip-existing resume; `standings.json` and the
auto-generated `Runs/Ladder/LADDER.md` are derivatives — **this page is the curated, published snapshot.**