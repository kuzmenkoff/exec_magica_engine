# Data Format — EXEC_MAGICA <!-- omit in toc -->

How a game is serialized for analysis and ML training. The engine's **event log**
is the source of truth; everything here is derived from it plus harness metadata.
On-disk contract is **`schemaVersion: 1`** (frozen 2026-06-13). Metric definitions
live in [METRICS.md](METRICS.md).

## Table of Contents <!-- omit in toc -->
- [Versioning](#versioning)
- [Reproducibility](#reproducibility)
- [Session record — one JSON document per game](#session-record--one-json-document-per-game)
- [Self-play dataset — the ML training data](#self-play-dataset--the-ml-training-data)
- [Hidden information](#hidden-information)
- [Persistence layout](#persistence-layout)
- [Who produces what](#who-produces-what)


## Versioning

- `schemaVersion` is an integer, currently **1**.
- Adding **optional** fields with safe defaults → **no** bump.
- Removing/renaming a field or changing its meaning → **bump**.
- The frozen card set is part of the contract; data is tied to it via `cardSetRevision`.

## Reproducibility

A session records its `seed`; the simulation is deterministic for a fixed seed, so
games replay exactly. **Caveat:** an agent on a wall-clock budget (e.g. MCTS
`budgetMode = Time`) runs a machine-dependent number of iterations and is **not**
reproducible — use **iteration budgets for data collection**, time budgets only for
live/demo play. Each agent's budget mode and params are stored in `players[].params`,
so every record is self-describing.

---

## Session record — one JSON document per game

```jsonc
{
  "schemaVersion": 1,
  "sessionId": "uuid-v4",
  "seed": 12345,
  "startedAtUtc": "2026-06-13T10:00:00Z",
  "cardSetRevision": "2026-06-13",      // ties data to the frozen card set

  "players": {
    "Player": { "modelId": "MCTS",   "params": { "iterations": 1000 } },
    "Enemy":  { "modelId": "Greedy", "params": {} }
  },
  "decks": { "Player": "Midrange", "Enemy": "Control" },
  "startingSide": "Player",

  "outcome": {
    "winner": "Player",                 // null for Draw / MaxActionsReached
    "reason": "HeroLethal",             // see METRICS.md § end reason
    "totalTurns": 18,
    "totalActions": 73,
    "durationMs": 142.6
  },

  "perSideMetrics": {
    "Player": { "avgManaEfficiency": 0.81, "meanThinkMs": 5.2,  "medianThinkMs": 3.0 },
    "Enemy":  { "avgManaEfficiency": 0.77, "meanThinkMs": 48.1, "medianThinkMs": 40.0 }
  },

  "cardImpact": { "1042": 12.0, "3005": 6.0 },  // per-card impact (CardId → score), see METRICS.md
  "events": [ /* full GameEvent log; written only when the logEvents toggle is on */ ]
}
```

`perSideMetrics` are precomputed conveniences; the `events` log lets anything be
recomputed from scratch.

---

## Self-play dataset — the ML training data

Separate from the telemetry session records above: `SelfPlayDataGenerator` plays
MCTS-vs-MCTS games and writes **one JSONL row per decision**, used to distil the network (see [AGENTS.md](AGENTS.md) → *Neural*), consumed by
[`ml/train_generations.ipynb`](../ml/train_generations.ipynb). Written in chunks (one file per N games) under
`Runs/SelfPlayData/gen<N>/`.

```jsonc
{
  "features": [ /* fixed-length float vector, PUBLIC info only — see below */ ],
  "side": "Player",                              // acting side; this row's point of view
  "policy": [ [type, src, tgtZone, tgtIdx, visits], ... ],  // MCTS visit distribution π
  "z": 1,            // outcome from `side` POV: +1 win / 0 draw / -1 loss (stamped at game end)
  "t": 7,            // decisions remaining until the game ends (for optional value discount z·γ^t)
  "cardIds": [ ... ] // per-slot CardId, parallel to the feature slots (for future card embeddings)
}
```

Unlike behaviour cloning, the **policy target is the full visit distribution** (a soft
AlphaZero-style target), and the **value target is the signed outcome** $z$ (optionally
discounted by game length). The same network is trained with masked cross-entropy on $\pi$
and MSE on $z$.

### Feature vector (encoder) <!-- omit in toc -->

Produced by `StateEncoder` from the **acting player's point of view**, **public information
only** — the opponent's hand is a **count**, never identities. A fixed-length flat float
vector of per-card slots + global scalars:

- **per-card slot** (hand, own field, opponent field): normalized mana/attack/HP/maxHP,
  keyword one-hots, and flags (isSpell, occupied, canAttack, remaining-attacks, silenced,
  has-effects);
- **globals:** hero HP, mana/pool/pending, fatigue, deck/hand/field counts, turn number.
- **effect block** (v2+): the slot's primary card effect — trigger / type / target one-hots + values;
- **unseen-pool block** (v3): the opponent's and own remaining cards, summed **per mana bucket**
  (order-free) — the network's view of the hidden pool (see *Hidden information*).

> **Versioned & evolving.** Size and layout are tagged by `StateEncoder.LayoutVersion`:
> **v1-400** (stats + keywords), **v2-1216** (+ structured card **effects** per slot), **v3-1616**
> (+ **unseen-card-pool** summaries — see *Hidden information*). A dataset is compatible only with a
> network trained on the **same** layout version. The **row schema is unchanged** across them
> (`schemaVersion` stays 1) — only the `features` length differs.

### Action space (policy head) <!-- omit in toc -->

Every action maps to a **fixed flat index**, mirrored in C# (`ActionEncoding`) and Python
(`action_index`). An action is first described as `[type, src, tgtZone, tgtIdx]` using **slot
indices consistent with the feature encoding**:

| field | values |
|---|---|
| type | 0 EndTurn · 1 PlayCard · 2 AttackCard · 3 AttackHero |
| tgtZone | 0 none · 1 myField · 2 oppField · 3 myHero · 4 oppHero |
| src / tgtIdx | positions within the hand / field slot lists |

The flat layout (227 indices): `End | Play(noTarget) | Play→myField | Play→oppField |
Play→myHero | Play→oppHero | AttackCard | AttackHero`. The policy head emits one logit per
index; illegal actions are masked before softmax.

> **C#↔Python contract.** `StateEncoder` ↔ Python `FEATURES`, and `ActionEncoding.Index` ↔
> `action_index`, must agree on feature size and action layout — otherwise training and
> in-engine inference disagree. **Changing the encoding requires regenerating the dataset.**

---

## Hidden information

Two different things treat hidden information separately:

**Dataset / training feature view.** v1/v2 expose only the opponent's hand/deck **counts** — no card
identities. **v3** additionally gives the network the **unseen-card pool**: the multiset of still-unseen
cards (opponent deck + hand, and own deck), summarized **order-free** by mana bucket. This is the *same
legal information the MCTS uses* (known decklist minus what's been played) — it slightly closes the
information gap between the network and its search teacher, **without** ever revealing the opponent's
*current* hand or draw order.

**MCTS agent info model (`knowsOpponentDeck = true`)** uses a **known-decklist** assumption:
the agent knows *which* cards the opponent runs and sees what's already been played, but not
*which* of the unplayed cards are in hand right now. Each search iteration it **re-deals**
the opponent's unplayed cards (hand + deck) into a random partition and reshuffles draw
order, reasoning over the **information set** of possible hands. This is exactly the card
counting a human could do against a known deck — not perfect-information cheating.

*(`knowsOpponentDeck = false` — not knowing the decklist, sampling from the global card
pool — is a harder setting left as future work.)*

---

## Persistence layout

A batch run writes one self-describing folder; a top-level index enables cross-run
queries without opening individual files.

```
Runs/
├── index.jsonl                         # master query table (one line per finished run)
├── <UTC>__<P-model>-vs-<E-model>__<P-deck>-vs-<E-deck>/
│   ├── sessions.jsonl                  # one full session record per line
│   └── summary.json                    # run config + BatchSummary
└── SelfPlayData/
    └── gen<N>/
        ├── meta.json            # generation, teacher, layout version, card-set rev, seeds
        └── gen<N>_0000.jsonl    # chunked decision rows (one file per N games), resume-aware
```

- **`sessions.jsonl`** — JSON Lines; append-friendly during the run, stream-readable
  for ML. Every line is self-describing (both models + params, decks, seed, outcome).
- **`index.jsonl`** (flat query table): `{ runId, startedAtUtc, playerModel, enemyModel,
  playerDeck, enemyDeck, matchup, games, playerWins, enemyWins, draws, playerWinRate,
  ciLow, ciHigh, schemaVersion, folder }`.
- The folder name is for human browsing only — **queries never parse it**.

**Querying** (filter the flat table by any field):

```bash
# all runs involving MCTS
jq -c 'select(.playerModel=="MCTS" or .enemyModel=="MCTS")' Runs/index.jsonl
```
```python
df = pd.read_json("Runs/index.jsonl", lines=True)
df[(df.playerModel == "MCTS") | (df.enemyModel == "MCTS")]
```

> The output root (`Runs/`) lives **outside `Assets/`** (or Unity would import it)
> and is git-ignored — run outputs are not committed.

---

## Who produces what

| Datum | Produced by |
|---|---|
| event stream, final state, turns/actions, winner | pure `GameEngine` |
| `thinkTimeMs`, mean/median think | harness (`Stopwatch` around `ChooseAction`) |
| end reason | harness (post-loop classification) |
| seed, startingSide, players, decks, sessionId | harness (session setup) |
| self-play features / visit-distribution policy / value targets | `SelfPlayDataGenerator` (StateEncoder + MCTS `Decide`) |
| win rate + CI, mana efficiency, card impact | offline metrics aggregator |