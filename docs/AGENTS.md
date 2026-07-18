# AI Agents — EXEC_MAGICA <!-- omit in toc -->

How each decision-making agent works and what its parameters mean. How the parameters
were **tuned and frozen** → [MODEL_TUNING.md](MODEL_TUNING.md); how the agents **rank** →
[LADDER.md](LADDER.md).

## Table of Contents <!-- omit in toc -->
- [Overview](#overview)
- [Random](#random)
- [Greedy](#greedy)
- [Neural (NN)](#neural-nn)
- [MCTS](#mcts)
  - [MCTS · Random rollout](#mcts--random-rollout)
  - [MCTS · Greedy rollout](#mcts--greedy-rollout)
  - [MCTS · Neural (NN-guided)](#mcts--neural-nn-guided)


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

## Neural (NN)

**What.** A learned policy+value network, distilled from MCTS self-play. Used **standalone**
here — one network evaluation per move, **no search**.

**How.** The public observation is encoded into a fixed-length feature vector (per-card slots
— stats and keywords — plus global scalars), fed through a small multilayer perceptron
that splits into two heads:

- **policy head** — a score for every action in a fixed flat action space (play card *i* at
  target *t*, attack, end turn); illegal actions are masked out.
- **value head** — a single position estimate in $[-1, 1]$ (how good the state is for the
  player to move).

Standalone, the agent plays the **highest-scoring legal action** from the policy head
(optionally sampling from the masked softmax). Like Greedy, it reads only **public**
information, so it respects imperfect information **without determinization**. Inference is a
plain in-engine forward pass over exported weights — no ML-runtime dependency.

**Training (distillation).** The network is trained offline to **imitate MCTS**, AlphaZero-style:

- **policy target** — the MCTS **visit distribution** $\pi$ over actions (masked cross-entropy);
- **value target** — the game **outcome** $z \in \{-1, 0, +1\}$ from each state's point of view (MSE).
- **target softening** — before training, the visit distribution is raised to $1/T$ and renormalized
  ($T>1$ = softer); the optimal $T$ trades prior sharpness for learnability (see the generations log).

Training data is the serialized self-play dataset (see [DATA_FORMAT.md](DATA_FORMAT.md)); the
trained weights are exported and loaded by the engine.

The distillation is run per generation by [`ml/train_generations.ipynb`](../ml/train_generations.ipynb)
(load dataset → train policy+value → export weights).

> **Encoding evolves by generation.** The champion uses encoding **v3** (`v3-1616`): per-slot stats +
> keywords + structured card **effects**, plus **unseen-card-pool** summaries (the opponent's and own
> remaining cards, order-free). Earlier generations used v1 (stats/keywords) and v2 (+effects).
> See [DATA_FORMAT.md](DATA_FORMAT.md) and [GENERATIONS.md](GENERATIONS.md).

**Role & limitation.** Standalone, the NN is a **fast learned heuristic** — one forward pass
replaces a whole search. Its policy is only as decisive as the search it imitates, so on its
own it is a baseline; the network's real contribution is **guiding MCTS** (below), where the
value head replaces noisy rollouts.

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
| iterations | total search budget | ladder: **1 s/move** (time-matched) |
| determinize | ISMCTS determinization on/off | true |
| knowsOpponentDeck | true = known decklist (re-deal hidden); false = global-pool sampling (TODO) | true |
| parallelize | root parallelization (aggregate trees) | false |

The **rollout policy** is what distinguishes the families — they are otherwise identical:

### MCTS · Random rollout

Playouts choose moves **uniformly at random**. Each rollout is noisy but ~14× cheaper
(~0.2 ms/iter), so far more simulations fit the same time. Rollout depth has a **sweet
spot ~40**: too short truncates before a signal forms, running to terminal adds variance
faster than signal. Under a wall-clock budget this cheap-and-plentiful approach is highly
competitive (see [MODEL_TUNING.md](MODEL_TUNING.md) → Comparison).

### MCTS · Greedy rollout

Playouts choose moves with the **Greedy heuristic**. Each rollout is realistic, so leaf
values are high-quality — but each iteration is expensive (~3.5 ms/iter). Deeper rollouts
help (reaching near-terminal matters).

### MCTS · Neural (NN-guided)

The same distilled network (see **Neural (NN)** above) **guides the search** instead of acting
alone — AlphaZero-style. Two changes to the MCTS loop; determinization and information-set
keying are unchanged.

- **A value+rollout blend evaluates the leaf.** Instead of a pure rollout, a newly expanded leaf is
  scored by mixing the network's **value head** with a short **random rollout**:
  `mix·rollout + (1−mix)·value`, frozen at **mix = 0.75**. Pure value alone suffers **distribution
  shift** — the value head is trained on decision-time positions but queried on search-frontier leaves,
  where it is less reliable; blending in a cheap rollout corrects this and delivered the **first
  equal-time win over plain MCTS**. (mix = 0 recovers pure value-at-leaf; mix = 1 is a plain rollout.)
- **PUCT replaces UCB.** The policy head supplies a **prior** $P(i)$ that biases exploration
  toward moves the network favors:

$$
\text{PUCT}(i) = v_i + c\,P(i)\,\frac{\sqrt{a_i}}{1 + n_i}
$$

where $v_i$ is the exploitation value (as in UCB above: $q_i$ if the node's chooser is the
**root** else $1-q_i$; a neutral $0.5$ for an unvisited child), $P(i)$ is the **policy-head
prior** for action $i$ (softmax of the network's logits over the legal actions), $c$ =
`puctC` (prior weight), and $a_i$ (availability) and $n_i$ (visits) are exactly as in UCB.

Early on the prior steers the tree (few visits); as visits accumulate the value-driven term
$v_i$ dominates. Unvisited children take a neutral value so the prior drives first contact.

**Why.** Random rollouts are cheap-but-noisy and greedy rollouts accurate-but-expensive; a
learned evaluator targets **both** — a good leaf estimate at a fixed, low cost — while the
policy prior focuses the search. This is the engine's strongest intended configuration and the
main contribution.

**Generational training.** NN-guided self-play can produce **stronger** data than plain MCTS,
which trains a stronger network, which sharpens the search — a self-play loop run by
generations (a strength-vs-generation curve; see the generations log).

| param | meaning |
|---|---|
| networkResource | which exported weight set guides the search |
| puctC | PUCT exploration constant (prior weight) |

> **Status.** The forward pass was optimized (sparse input + legal-only output + allocation-free
> scratch), closing the speed gap to ~1.6× a plain rollout. The **policy prior** is the dominant source
> of strength (a mix sweep showed most of the edge survives with the value off); the value blend adds the
> rest. Strength rises across generations, then **plateaus** — the loop's limit is representation /
> distillation, not search (see the generations log and the compute-scaling ceiling analysis).

---

*Rankings and head-to-head results for these agents → [LADDER.md](LADDER.md).*