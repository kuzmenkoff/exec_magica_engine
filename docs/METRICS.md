# Metrics — EXEC_MAGICA <!-- omit in toc -->

How the project measures and compares decision-making agents. Every metric is
computed **offline** from a recorded game's **event log** — the deterministic
stream of everything that happened (`HeroDamaged`, `CardSummoned`, `ManaSpent`, …).
Because the log is replayable for a fixed seed, every number here is reproducible
and auditable: the same games always yield the same metrics.

## Table of Contents <!-- omit in toc -->
- [Win rate (with confidence interval)](#win-rate-with-confidence-interval)
- [Rating — Bradley–Terry / Elo](#rating--bradleyterry--elo)
- [Game duration](#game-duration)
- [Think time per move](#think-time-per-move)
- [Mana efficiency](#mana-efficiency)
- [Card impact score](#card-impact-score)
- [Game end reason](#game-end-reason)
- [Compute scaling (tactical depth)](#compute-scaling-tactical-depth)


## Win rate (with confidence interval)

**What.** The fraction of games an agent wins — the headline measure of strength.

**Why.** It answers the core question directly: *does agent A beat agent B?* But a
raw fraction from a handful of games is unreliable (win 7 of 10 — is that really
70%, or luck?). So we never report the bare number alone; we attach a **confidence
interval (CI)** — the range the *true* win rate plausibly lies in.

**How.** Point estimate for `k` wins in `n` games:

$$\hat{p} = \frac{k}{n}$$

For the 95% interval we use the **Wilson score interval** (not the textbook
$\hat{p} \pm z\sqrt{\hat{p}(1-\hat{p})/n}$, which breaks down — it can fall below 0
or above 1, and is wrong for small `n` or lopsided results):

$$
\text{CI}_{\pm} = \frac{\hat{p} + \dfrac{z^2}{2n} \;\pm\; z\sqrt{\dfrac{\hat{p}(1-\hat{p})}{n} + \dfrac{z^2}{4n^2}}}{1 + \dfrac{z^2}{n}}
$$

Reading it in words:
- $z = 1.96$ is the "95% confidence" constant (how many standard deviations cover 95%).
- $\dfrac{z^2}{2n}$ gently **pulls the center toward 50%** — the fewer games you have,
  the less the interval trusts an extreme result.
- the square-root part is the **spread** (how wide the interval is).
- dividing by $1 + \dfrac{z^2}{n}$ keeps everything **inside `[0, 1]`**.

Net effect: a sensible interval even at 40 games or a 90% win rate — exactly the
regime this project runs in.

**Example.** 88 wins in 100 games → $\hat{p} = 88\%$, Wilson 95% CI ≈ **[80%, 93%]**.
Two agents whose intervals **overlap** are not distinguishable at this sample size.

---

## Rating — Bradley–Terry / Elo

**What.** A single number per agent that summarizes strength on one common scale,
so the whole field can be ranked at a glance.

**Why.** Win rate is always *pairwise* (A vs B). With many agents that's a messy
grid. A rating **collapses the entire round-robin into one number per agent**, and
the scale is interpretable: a fixed gap means fixed odds.

**How.** The probability that agent $i$ beats agent $j$ is modeled as:

$$P(i \text{ beats } j) = \frac{1}{1 + 10^{(R_j - R_i)/400}}$$

- Equal ratings ($R_i = R_j$) → $P = 50\%$ (a coin flip — as it should be).
- A **400-point** lead → $P \approx 91\%$ (i.e. 10-to-1 odds). That "400 = ×10 odds"
  is the standard Elo convention.

The ratings $R_i$ are **fit by maximum likelihood**: pick the numbers that best
reproduce the actually observed win rates across all pairs. The scale is relative,
so we **anchor Random at $R = 0$** as the zero point. The confidence intervals come
from **bootstrap**: resample the games, refit the ratings, repeat (300×), and take
the 2.5/97.5 percentiles — this captures how much the ratings could wobble given
the limited games per pair.

**Example.** tuned Greedy ≈ 492, NN+MCTS (gen0) ≈ 794. The ~300-point gap predicts the NN+MCTS wins
~85% of head-to-head games — matching the observed **82%** in the matchup matrix.

---

## Game duration

**What.** How long a game runs — in **turns** and in **actions** (individual plays).

**Why.** A behavioral fingerprint, not a strength measure. Short games suggest an
aggressive, racing style; long games suggest grindy, control play. It's also a
**sanity check**: if many games hit the action cap without a winner, something is
stalling and the strength numbers would be suspect.

**How.** Read straight from the final state: `TurnNumber` and `ActionIndex` at the
end. Report the **mean and median** across games (the median ignores the occasional
freak-long game).

---

## Think time per move

**What.** How long an agent takes to choose one action, in milliseconds.

**Why.** This is the **compute-cost axis**. Strength alone is half the story — an
agent that wins but takes 5 seconds a move trades very differently from one that
wins instantly. Paired with win rate, think-time gives the project's central
**"strength vs compute budget"** comparison.

**How.** The harness wraps each `ChooseAction` call in a `Stopwatch`. Per agent we
report both **mean** and **median**:
- the **median** is the "typical" move (robust to outliers),
- the **mean** reflects total compute spent.
MCTS in particular has a few very slow moves (large branching) that pull the mean
above the median — reporting both is honest.

For search agents on a **time** budget, the ladder also reports **iters/move** — how many MCTS iterations
fit the budget. It is the machine-anchored companion to think-time: on other hardware the same wall-clock
buys proportionally more/fewer iterations (see [LADDER.md](LADDER.md) → *Benchmark environment*).

---

## Mana efficiency

**What.** What fraction of its available mana an agent actually spends each turn.

**Why.** A behavioral proxy for **resource use / tempo**, independent of win/loss.
Floating lots of unused mana usually means weak, passive play; high efficiency
means the agent converts resources into board presence. It helps explain *why* one
agent beats another, not just *that* it does.

**How.** Per turn $t$ for a side (skipping turns where no mana was available, to
avoid dividing by zero):

$$
e_t = \min\!\left(1,\; \frac{\text{manaSpent}_t}{\text{manaAvailable}_t}\right)
$$

The $\min(1, \cdot)$ caps the value at 100% — temporary mana can momentarily make
"spent" exceed "available", but over-100% efficiency is meaningless. The game value
is the average over the side's turns:

$$\bar{e} = \frac{1}{|T|}\sum_{t \in T} e_t$$

**Example.** A turn spending 3 of 4 mana scores $0.75$. An agent averaging $\bar{e} = 0.85$
across the game uses its resources well; one at $0.55$ is leaving a lot on the table.

---

## Card impact score

**What.** A per-card number measuring how much a card actually *does* in the games it's
played.

**Why.** Rank cards by **board contribution**, not just by whether their owner won.

**How.** For card $c$, sum a weight over every event the card *caused*, then divide by
$G_c$, the number of games in which $c$ was played at least once:

$$\text{impact}(c) = \frac{1}{G_c} \sum_{e \,:\, \text{src}(e)\to c} w(e)$$

**Currently credited** (each event contributes its `Value`):

| Event the card caused | weight $w(e)$ |
|---|---|
| `HeroDamaged` (enemy hero) | $+\,\text{Value}$ (damage dealt) |
| `DamageDealt` (enemy card) | $+\,\text{Value}$ |
| `CardHealed` / `HeroHealed` (own side) | $+\,\text{Value}$ (healing) |
| `CardStatsBuffed` (own side) | $+\,\text{Value}$ (buff) |

Events with no card source (e.g. `FatigueDamage`) are naturally excluded.

**Planned (may be added later).** Crediting removal and summons by the stats they move:

| Event the card caused | weight $w(e)$ |
|---|---|
| `CardSummoned` | $+\,(\text{attack}+\text{hp})$ of the summoned token |
| `CardDestroyed` / `CardSilenced` (enemy) | $+\,(\text{attack}+\text{hp})$ removed |

**Cross-check.** Impact score is paired with the **play-weighted win rate** — the win rate
of the owning side in games where the card was played at least once. Agreement between the
two means the card is genuinely strong.

**Example.** A spell dealing 5 damage to the enemy hero contributes $+5$ (`HeroDamaged`).
Once the planned removal credit lands, a minion that kills a 3/4 would add $+7$.

---

## Game end reason

**What.** A label for *why* each game ended.

**Why.** Validity. A win by lethal damage is a real result; a game that just hit the
action cap is a stall, not a victory. Separating these keeps the strength metrics
honest (and flags broken matchups).

**How.** Classified by the harness after the playout loop:

| Reason | Meaning |
|---|---|
| `HeroLethal` | a hero hit 0 HP from card/attack damage — a normal win |
| `Fatigue` | a hero hit 0 HP from drawing on an empty deck (deckout) |
| `MaxActionsReached` | the loop hit the action cap with no winner — a stall |
| `Draw` | game over with no winner (both heroes resolved to 0) |

A healthy experiment is dominated by `HeroLethal`; a spike in `MaxActionsReached`
means games are stalling and the numbers need a second look.

---

## Compute scaling (tactical depth)

**What.** How much an agent's play improves when given **more thinking time** — measured by playing the
champion **against itself** with one side on a K× time budget, reading the stronger side's win rate.

**Why.** Strength metrics are taken at a fixed budget; this asks the orthogonal question — *how much depth
is left to find?* If K× more compute barely wins (~50%), extra search finds no better moves: the deck is
near its **skill ceiling**. If it wins decisively, real tactical depth remains. Run per deck, it maps where
the game rewards deeper thought — and separates a *method* plateau from the *game's* ceiling.

**How.** For ratio $K \in \{1,2,4,8,16\}$ the "strong" side gets $K\times$ the reference's time/move (both
sides the same network), alternating start; report the strong side's Wilson win rate. $K=1$ is the ~50%
sanity check; a rising curve = depth remains; a flat ~50% curve = depth exhausted.

**Example.** Champion vs self: on ControlPreset the 16× side wins ~73% (deep — more search keeps helping);
on TokenPreset ~51% (flat — the ceiling is reached). See [GENERATIONS.md](GENERATIONS.md) → *Conclusion*.

*Computation lives in the offline metrics aggregator; raw per-game data and the
serialized record schema are described in [DATA_FORMAT.md](DATA_FORMAT.md).*