# Rating Ladder — EXEC_MAGICA

Head-to-head strength of the frozen agents. Configs → [MODEL_TUNING.md](MODEL_TUNING.md);
rating method → [METRICS.md](METRICS.md).

## Eligibility
Each agent competes at a configuration whose **mean think-time ≤ 2 s/move** on the
reference machine — strength under a bounded, practical per-move budget, not unlimited compute.

## Ladder
*4 decks (Aggro/Control/Midrange/Token) · 160 games/pair · fatigue-on · Bradley–Terry / Elo,
Random anchored at 0 · bootstrap 95% CI.*

| Rank | Agent | Elo | 95% CI | Win % | Think/move |
|:----:|-------|----:|:------:|------:|:----------:|
| 🥇 | MCTS · Greedy rollout (350 it) | **587** | 501–685 | 72.5 | 1.54 s |
| 🥈 | MCTS · Random rollout (3200 it) | 568 | 492–657 | 69.8 | 0.58 s |
| 🥉 | Greedy (tuned) | 455 | 377–532 | 53.1 | <1 ms |
| 4 | Random (baseline) | 0 | — | 4.6 | <1 ms |

**Tiers:** {MCTS Greedy ≈ MCTS Random} ≫ Greedy ≫ Random. The two MCTS agents are
**statistically tied** (overlapping CIs, 53–47 head-to-head); the random-rollout agent
matches the greedy-rollout one at **~⅓ the think-time** (0.58 s vs 1.54 s).

## Matchup matrix (row win % vs column)
| | mcts-greedy | mcts-random | greedy | random |
|---|---|---|---|---|
| **mcts-greedy** | — | 53 | 69 | 96 |
| **mcts-random** | 47 | — | 65 | 98 |
| **greedy** | 31 | 35 | — | 93 |
| **random** | 4 | 3 | 7 | — |

No intransitivity — a clean strength hierarchy.