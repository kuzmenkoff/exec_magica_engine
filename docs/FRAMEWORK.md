# EXEC_MAGICA as a framework <!-- omit in toc -->

This repository's engine is **frontend-agnostic**: a pure-C# rules core that runs headless.
You drive it through one small contract and render the result however you like — a Unity
client, a console app, or a research harness. The playable reference game is just one
frontend built on exactly this API.

> Full type-level docs: **[API reference](https://kuzmenkoff.github.io/exec_magica_engine/)** ·
> game rules: [RULES.md](RULES.md) · agents: [AGENTS.md](AGENTS.md).

## Contents <!-- omit in toc -->

- [The engine contract](#the-engine-contract)
- [Quick start: a game loop](#quick-start-a-game-loop)
- [Setting up a match](#setting-up-a-match)
- [Plugging in an AI agent](#plugging-in-an-ai-agent)
- [Rendering from the event stream](#rendering-from-the-event-stream)
- [Adding your own cards](#adding-your-own-cards)
- [Headless self-play](#headless-self-play)
- [Running experiments at scale (.NET runner)](#running-experiments-at-scale-net-runner)
- [Where to go next](#where-to-go-next)

## The engine contract

Everything goes through three types:

| Type | Role |
|---|---|
| `GameState` | The full, serializable snapshot of a match (both players, zones, whose turn). |
| `GameEngine` | Wraps a `GameState`; exposes legal actions and applies actions. |
| `IGameActionPolicy` | A decision-maker (human input or AI) that picks one action. |

The loop is always the same: **ask the engine for legal actions → choose one → apply it →
render the emitted events.**

```csharp
GameEngine engine = new GameEngine(initialState);

List<GameAction> legal = engine.GetLegalActions();      // for the active side
GameStepResult result  = engine.ApplyAction(action);    // mutates state, returns events

// result.Success      -> was the action legal?
// result.ErrorMessage -> why not (if it failed)
// result.Events        -> the GameEvent stream to render
```

For a Unity frontend there is a thin wrapper, **`UnityGameEngineAdapter`**, with the same
shape (`Initialize(state)`, `State`, `GetLegalActions([side])`, `ApplyAction(action)`).
It only forwards to `GameEngine` — use whichever you prefer; headless code can use
`GameEngine` directly.

## Quick start: a game loop

```csharp
GameEngine engine = new GameEngine(initialState);

while (!engine.State.IsGameOver)
{
    PlayerSide side = engine.State.ActiveSide;

    List<GameAction> legal = engine.GetLegalActions(side);
    if (legal.Count == 0) break;

    // `policy` is your AI, or an action you built from player input.
    GameAction action = policy.ChooseAction(engine.State, legal, side);
    if (action == null) break;

    GameStepResult result = engine.ApplyAction(action);
    if (!result.Success) continue;     // illegal -> state unchanged

    Render(result.Events);             // your view layer
}

PlayerSide? winner = engine.State.Winner;   // null = draw / ongoing
```

That is the entire integration surface. No Unity types are required to run it.

## Setting up a match

A `GameState` is built from two decks and a card database via `GameStateBuilder`:

```csharp
// 1. Load the card database (JSON -> AllCards) once.
AllCards db = CardJsonLoader.LoadAllCards(File.ReadAllText("AllCards.json"));

// 2. Build two decks (an AllCards whose `cards` list holds 30 Card definitions).
AllCards playerDeck = RuntimeDeckLoader.LoadPreset("Midrange", db);  // or RandomDeck(db, seed)
AllCards enemyDeck  = RuntimeDeckLoader.LoadPreset("Aggro", db);

// 3. Create the initial state (seeded for reproducibility).
GameState state = GameStateBuilder.CreateInitialState(
    playerDeck, enemyDeck, db, playerFirst: true, shuffleSeed: 42);
```

`CreateInitialState` fills and shuffles both decks, draws starting hands, gives the Coin to
the second player and starts the first turn. A fixed `shuffleSeed` makes the whole game
reproducible. Rule constants (deck size, hand/field caps, starting HP/mana) live in
[`GameRules`](RULES.md).

## Plugging in an AI agent

Any decision-maker implements one method:

```csharp
public interface IGameActionPolicy
{
    GameAction ChooseAction(GameState state, List<GameAction> legalActions, PlayerSide actorSide);
}
```

Three implementations ship with the engine (see [AGENTS.md](AGENTS.md)):

```csharp
IGameActionPolicy random = new RandomActionPolicy(seed: 1);
IGameActionPolicy greedy = new GreedyActionPolicy(seed: 1);
IGameActionPolicy mcts   = new MctsActionPolicy(new MctsConfig {
    BudgetMode   = MctsConfig.Budget.Time,
    TimeBudgetMs = 1500,
    RolloutPolicy = MctsConfig.Rollout.Greedy
});
```

For a **human** player, skip the policy: present `engine.GetLegalActions(side)` in your UI
and build the chosen `GameAction` directly (the `GameAction.PlayCardOnCard`, `AttackHero`,
`EndTurn`, … factories make this easy). Your own ML policy is just another
`IGameActionPolicy`.

## Rendering from the event stream

`ApplyAction` never touches your view. Instead it returns a `GameStepResult` whose `Events`
list describes exactly what happened, as plain data:

```csharp
foreach (GameEvent e in result.Events)
{
    switch (e.Type)
    {
        case GameEventType.CardPlayed:   PlayCardAnimation(e.SourceInstanceId); break;
        case GameEventType.DamageDealt:  ShowDamage(e.TargetInstanceId, e.Value); break;
        case GameEventType.HeroDamaged:  ShowHeroHit(e.TargetHeroSide, e.Value); break;
        case GameEventType.CardDied:     RemoveCard(e.SourceInstanceId); break;
        // ... ~35 event types; see GameEventType
    }
}
```

Each event carries `TurnNumber`, `ActionIndex`, `ActorSide`, optional source/target ids, a
numeric `Value` (meaning depends on `Type`) and a human-readable `Message`. The same stream
is what the telemetry and replay tooling consume (see [DATA_FORMAT.md](DATA_FORMAT.md)).

## Adding your own cards

Cards are **data**, not code. A card is a JSON object; its abilities are a list of effects,
each a `Trigger` + `Type` + `Target` (+ value). No engine changes are needed to add new
cards that reuse the existing effect types:

```json
{
  "entities": [
    {
      "id": 1001,
      "Title": "Emberling",
      "Class": "ENTITY",
      "Attack": 2, "HP": 3, "MaxHP": 3, "ManaCost": 2,
      "Keywords": [ "Charge" ],
      "Effects": [
        { "Trigger": "OnPlay", "Type": "DealDamage", "Target": "SelectedEnemyCharacter", "Value": 2 }
      ]
    }
  ]
}
```

Available `Trigger` / `Type` / `Target` / `Keyword` values are listed in [RULES.md](RULES.md).
Only a brand-new **effect *type*** (a mechanic that doesn't exist yet) requires code — add it
to the `EffectType` enum and handle it in `EffectEngine`.

## Headless self-play

For experiments, skip rendering entirely and run thousands of games with `BatchRunner`:

```csharp
BatchResult result = BatchRunner.Run(
    makePlayerDeck:   seed => RuntimeDeckLoader.RandomDeck(db, seed),
    makeEnemyDeck:    seed => RuntimeDeckLoader.RandomDeck(db, seed),
    database:         db,
    makePlayerPolicy: seed => new GreedyActionPolicy(seed),
    makeEnemyPolicy:  seed => new MctsActionPolicy(new MctsConfig { Seed = seed }),
    playerModel:      greedyModel.BuildModelInfo(),
    enemyModel:       mctsModel.BuildModelInfo(),
    playerDeckName:   "Random", enemyDeckName: "Random",
    config:           new BatchConfig { Games = 200, AlternateStart = true });

SessionWriter.WriteRun("Runs", result);   // -> summary.json + sessions.jsonl + index.jsonl
```

`BatchResult.Summary` gives win rate (with Wilson 95% CI), end-reason breakdown, average
length, think time and per-card impact. This is the exact pipeline behind the project's
[ladder](LADDER.md) and [metrics](METRICS.md).

The same call also runs on the standalone **.NET runner** for large parallel experiments — see below.

## Running experiments at scale (.NET runner)

The engine is pure C# with no Unity dependency, so the **same `LogicLayer` also compiles as a
standalone .NET console** (`bench/`), not just inside Unity. This matters for throughput: Unity's
Mono/Boehm GC serializes allocations, so game-level parallelism scales *negatively* there; the .NET
runner uses **server GC** (per-core heaps) and scales **~linearly (~10× on 28 cores)** — with
bit-identical results.

Agents are described as **data** (`AgentSpec`) and rebuilt in either runtime by `AgentFactory`, so a
matchup defined in the Unity Editor runs unchanged on the .NET runner:

```csharp
AgentSpec spec           = model.ToAgentSpec();          // from any OpponentModelDefinition, or hand-built
IGameActionPolicy policy = AgentFactory.Build(spec, seed);
```

The runner reads a JSON `BenchRunSpec` and supports several modes:

| mode | what it does |
|---|---|
| `generate` | self-play data generation (teacher = MCTS or NN+MCTS) → `Runs/SelfPlayData/gen<N>/` |
| `batch` | one matchup A vs B, full telemetry via `SessionWriter` — identical output to the in-process path |
| `ladder` | round-robin of an agent roster → Bradley–Terry / Elo (`Runs/Ladder/`) |
| `duel` · `match` · `ceiling` | head-to-heads, strength-vs-time, compute-scaling |

The three Editor windows — **Self-Play Data**, **Batch**, **Ladder** — each offer a **Run (.NET)**
button that serializes the current settings and launches the runner in a separate process: Unity stays 
responsive, and the run uses the core count set in the window (**0 = all cores**).

## Where to go next

- **[API reference](https://kuzmenkoff.github.io/exec_magica_engine/)** — every public type and method.
- **[RULES.md](RULES.md)** — rules, keywords, effects, deck presets.
- **[AGENTS.md](AGENTS.md)** — how the Random / Greedy / MCTS agents work.
- **[METRICS.md](METRICS.md)** · **[DATA_FORMAT.md](DATA_FORMAT.md)** — measurement and run schema.
