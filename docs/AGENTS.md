# AI Agents — EXEC_MAGICA

How each decision-making agent works and what its parameters mean. How the parameters
were **tuned and frozen** → [MODEL_TUNING.md](MODEL_TUNING.md); how the agents **rank** →
[LADDER.md](LADDER.md).

## Overview

Every agent implements a single interface:

```csharp
GameAction ChooseAction(GameState state, List<GameAction> legalActions, PlayerSide actorSide);
```

The engine asks the active agent for one action per decision (play a card, attack,
end turn). Agents are interchangeable — the engine never knows which one is playing.

**Observation model (imperfect information).** No agent sees the full game state. Each
receives a **masked observation**:

- **Visible:** own hand/field/graveyard/HP/mana; the opponent's field, graveyard, HP, mana
  (face-up); and only the **counts** of the opponent's hand and deck.
- **Hidden:** the opponent's hand-card identities, and both decks' contents and draw order.

Consequently, search agents cannot read the true hidden state — they must **sample** it
(determinization, see MCTS below). The full perfect-information state is logged for
analysis, but it is never handed to an agent.

---

## Random

**What.** The baseline floor: no evaluation at all.

**How.** Each decision it picks **uniformly at random among the legal non-end-turn
actions**, and ends the turn only when no other action is available (it never passes
prematurely). Used both as a standalone agent and as the MCTS random rollout policy.

**Parameters.**

| param | meaning |
|---|---|
| seed | RNG seed (reproducibility only) |

Random gives the zero point for the Elo ladder and a sanity floor every other agent must
clear.

---

## Greedy

**What.** A fast, myopic heuristic agent — one-ply lookahead, no opponent modeling.

**How.** For each legal action, Greedy applies it to a deep copy of the state via the
real engine, scores the **resulting** state, and plays the highest-scoring action
(reservoir tie-break, deterministic for a fixed seed). One action ahead; no opponent reply.

The score is a weighted **own-minus-opponent** difference on the resulting state:

$$
V = w_{\text{heroHP}}(\text{myHP}-\text{oppHP}) + w_{\text{atk}}(\text{myAtk}-\text{oppAtk})
+ w_{\text{hp}}(\text{myHP}_\text{board}-\text{oppHP}_\text{board})
+ w_{\text{min}}(\text{myMinions}-\text{oppMinions})
+ w_{\text{hand}}(\text{myHand}-\text{oppHand})
$$

A terminal state scores $\pm 10^6$ (win/loss), so a lethal action is always taken and a
losing one avoided. The evaluation reads only **public** quantities (hero HP, board stats,
hand **counts**) — so Greedy respects imperfect information **without determinization**.

**Parameters (frozen weights).**

| weight | meaning | frozen |
|---|---|---|
| heroHpWeight | own − enemy hero HP (anchor) | 1.0 |
| attackWeight | Σ board attack | 2.0 |
| hpWeight | Σ board HP | 1.0 |
| minionCountWeight | number of minions | 1.0 |
| handCountWeight | hand size (card advantage) | 1.0 |

**Limitation.** Being one-ply and opponent-blind, Greedy plays tactically but cannot plan
multi-step lines — which is exactly the gap MCTS closes.

---

## MCTS

**What.** Monte-Carlo Tree Search: builds a search tree by simulation to find strong
moves under uncertainty. All MCTS variants here are **ISMCTS** (information-set MCTS) to
handle hidden information.

**How.** Each **iteration** re-samples the hidden world (determinization), then runs
selection → expansion → rollout → backpropagation within it:

1. **Selection** — descend by UCB until an action with no child yet:

$$
\text{UCB}(i) = \underbrace{v_i}_{\text{exploit}} + C\sqrt{\frac{\ln a_i}{n_i}}
$$

where $q_i = \text{rootWins}_i/n_i$, $v_i = q_i$ if the node's chooser is the **root**
else $1-q_i$ (the opponent minimizes the root's win probability), $n_i$ = visits, and
$a_i$ = **availability** — how many iterations the action was legal. Availability (not
parent visits) is the ISMCTS correction: some actions are legal only in some
determinizations. Unvisited children are explored first.

2. **Expansion** — add a child for one untried action.
   
3. **Rollout** — play out with the rollout policy up to `maxRolloutActions`. Reaching
   terminal scores **1 / 0** (root win/loss); **hitting the cap first scores 0.5** (no
   signal) — which is why rollout depth matters.
   
4. **Backpropagation** — add the result and a visit up the path.

After `iterations`, the root move is picked by `finalSelection` (**MaxVisits** = most
visited; MaxValue = highest win rate).

**Determinization (information set).** With `knowsOpponentDeck = true` (the frozen mode),
the agent **assumes the opponent's decklist is known**: each iteration it deep-copies the
true state, reshuffles its own deck order, and **re-deals the opponent's hidden cards**
(hand + deck) randomly — `handCount` to hand, the rest to deck. Hidden opponent plays are
keyed by card **type**, not instance, so the search reasons over the **information set** of
possible hand/deck partitions, never the specific current hand. *(Known-decklist is a
modeling assumption.)*

| param | meaning | frozen |
|---|---|---|
| explorationC | UCB exploration constant | 1.41 (≈ √2) |
| rolloutPolicy | leaf-evaluation strategy | per family |
| maxRolloutActions | rollout length cap (code default 200) | 40 |
| finalSelection | root move-pick rule | MaxVisits |
| iterations | total search budget | per ladder budget |
| determinize | ISMCTS determinization on/off | true |
| knowsOpponentDeck | true = known decklist (re-deal hidden); false = global-pool sampling (TODO) | true |
| parallelize | root parallelization (aggregate trees) | false |

The **rollout policy** is what distinguishes the families — they are otherwise identical:

### MCTS · Greedy rollout

Playouts choose moves with the **Greedy heuristic**. Each rollout is realistic, so leaf
values are high-quality — but each iteration is expensive (~3.5 ms/iter). Deeper rollouts
help (reaching near-terminal matters).

### MCTS · Random rollout

Playouts choose moves **uniformly at random**. Each rollout is noisy but ~14× cheaper
(~0.2 ms/iter), so far more simulations fit the same time. Rollout depth has a **sweet
spot ~40**: too short truncates before a signal forms, running to terminal adds variance
faster than signal. Under a wall-clock budget this cheap-and-plentiful approach is highly
competitive (see [MODEL_TUNING.md](MODEL_TUNING.md) → Comparison).

### MCTS · ML rollout 🚧 *(planned)*

A **learned value network** replaces playout-based leaf evaluation — aiming for the
quality of a good rollout at near-instant cost (AlphaZero-style). The motivation: random
rollouts are cheap-but-noisy and greedy rollouts are accurate-but-expensive; a learned
evaluator targets both at once. Training data comes from the serialized self-play records
(see [DATA_FORMAT.md](DATA_FORMAT.md)).

---

*Rankings and head-to-head results for these agents → [LADDER.md](LADDER.md).*