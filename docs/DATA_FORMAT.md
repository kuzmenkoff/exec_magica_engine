# Data Format — EXEC_MAGICA

How a game is serialized for analysis and ML training. The engine's **event log**
is the source of truth; everything here is derived from it plus harness metadata.
On-disk contract is **`schemaVersion: 1`** (frozen 2026-06-13). Metric definitions
live in [METRICS.md](METRICS.md).

---

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
  "decisions": [ /* the ML dataset rows — see below */ ],
  "events":    [ /* full GameEvent log; written only when the logEvents toggle is on */ ]
}
```

`perSideMetrics` are precomputed conveniences; the `events` log lets anything be
recomputed from scratch.

---

## Decision record — the unit of the ML dataset

One row per decision an agent made.

```jsonc
{
  "turn": 5,
  "actionIndex": 21,
  "actorSide": "Player",
  "thinkTimeMs": 4.1,
  "stateFeatures": { /* see below */ },
  "legalActions": [ /* encoded actions, see below */ ],
  "chosenActionIndex": 3,   // policy label: index into legalActions
  "valueTarget": 1          // 1 if actorSide ultimately won, else 0 (stamped from outcome)
}
```

`chosenActionIndex` is the label for **policy (behavior-cloning)** training;
`valueTarget` is the label for **value-network** training.

---

## State features

> **Status:** the session record (outcome, per-side metrics, card impact, events) is
> produced now. The **decision rows and state features below are the ML dataset spec** —
> they are emitted by a dedicated **dataset extractor** added with the ML phase, not yet
> populated by the runner.

What an agent / the dataset sees about a position.

**Numeric groups**
- **self / opponent** (same fields each): `hp, maxHp, mana, manaPool, fatigueCounter,
  handCount, deckCount, fieldCount, graveyardCount, sumFieldAttack, sumFieldHP`
- **board:** 7 slots per side — `{ attack, hp, keywords }`; an empty slot is all zeros
- **global:** `turnNumber, whoseTurn`

**Keyword bitmask** — the dataset **feature encoding** (one bit per keyword, bitwise OR;
*not* the raw `KeywordType` enum values, which are sequential):

| keyword | bit |
|---|---|
| Provocation | 1 |
| Shield | 2 |
| Charge | 4 |
| Rush | 8 |
| DoubleAttack | 16 |
| Lifesteal | 32 |

**Action encoding**

```jsonc
{
  "type": "PlayCard|AttackCard|AttackHero|EndTurn",
  "sourceInstanceId": 1234,
  "targetType": "None|Card|Hero",
  "targetInstanceId": 5678,     // when targetType == Card
  "targetHeroSide": "Enemy",    // when targetType == Hero
  "fieldIndex": null
}
```

A fixed-width numeric encoding (for a policy head) is derived from this structured
form when needed.

---

## Hidden information

Two different things treat hidden information separately:

**Dataset / training feature view** masks the opponent's hand and deck **identities** —
only their **counts** are exposed, so a value/policy network never sees hidden cards.
- Visible: own everything; opponent HP / mana / field / graveyard (face-up) + hand/deck counts.
- Hidden (to the feature view): opponent hand-card identities and deck order.

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
└── <UTC>__<P-model>-vs-<E-model>__<P-deck>-vs-<E-deck>/
    ├── sessions.jsonl                  # one full session record per line
    └── summary.json                    # run config + BatchSummary
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
| state features, legal actions, labels, masking | dataset extractor |
| win rate + CI, mana efficiency, card impact | offline metrics aggregator |