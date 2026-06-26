# Game Rules — EXEC_MAGICA <!-- omit in toc -->

A turn-based, **Hearthstone-style** collectible card game: two heroes, a board of minions,
spells, and a mana curve. All values below are the constants the engine actually uses
(`GameRules.cs`).

## Table of Contents <!-- omit in toc -->

- [Objective](#objective)
- [Match flow](#match-flow)
- [Board, hand \& deck](#board-hand--deck)
- [Cards](#cards)
- [Keywords](#keywords)
- [Effects](#effects)
- [Deck presets](#deck-presets)
- [Card set](#card-set)

---

## Objective

Reduce the opposing hero from **30 HP** to **0**. How a game ends:

| End reason | Cause |
|---|---|
| **Hero lethal** | a hero reaches 0 HP from card/attack damage — a normal win |
| **Fatigue** | a hero reaches 0 HP from drawing on an empty deck |
| **Draw** | both heroes resolve to 0 simultaneously |
| **Stall** | the action cap is reached with no winner (non-decisive) |

---

## Match flow

Each player's turn:
1. **Mana** increases by **1** crystal (start **1**, cap **10**) and is fully refilled.
2. **Draw** one card.
3. **Play** cards from hand (paying their mana cost) and **attack** with minions.
4. **End turn.**

Setup:
- Both heroes start at **30 HP** and **1** mana crystal.
- The **first** player starts with **3** cards, the **second** with **4** cards **+ the Coin**
  (a card granting **+1 temporary mana** this turn).
- **Temporary mana** (the Coin, or card effects) can raise current mana **above the pool**,
  but never above the **10** cap; it does not raise the permanent pool.

Attacking:
- Minions attack an **enemy minion** or the **enemy hero**.
- A freshly played minion cannot attack the turn it arrives, **unless** it has `Charge`/`Rush`
  (see Keywords).

---

## Board, hand & deck

| Rule | Value |
|---|---|
| Minions on the battlefield (per side) | **7** |
| Cards in hand (max) | **10** — drawing with a full hand skips the draw |
| Deck size | exactly **30** |
| Copies of one card per deck | up to **2** |
| Starting hero health | **30** |
| Starting mana / per-turn ramp / cap | **1 / +1 / 10** |

**Fatigue:** drawing from an empty deck deals escalating self-damage — **1, then 2, then 3,
…** cumulative — to your own hero, guaranteeing games end.

---

## Cards

Two classes (`Card.CardClass`):

- **Entity** (minion) — has **Attack** and **HP**, stays on the board, can attack.
- **Spell** — a one-shot effect, no board presence.

A card definition (`CardDefinition`) carries: `Title`, `Description`, `ManaCost`, `Attack`,
`HP` / `MaxHP` (entities), `Keywords`, `Effects`, and `IsCollectible` (summoned tokens are
**non-collectible** — they can't appear in decks).

---

## Keywords

Passive properties (`KeywordType`):

| Keyword | Effect |
|---|---|
| **Provocation** | Enemy cards and hero cannot be attacked while this card is on the battlefield (must be dealt with first). |
| **Shield** | The next damage this card takes is prevented. |
| **Charge** | Can attack immediately the turn it is played. |
| **Rush** | Can attack enemy **cards** the turn it is played, but not the enemy hero that turn. |
| **DoubleAttack** | Can attack twice in one turn. |
| **Lifesteal** | When this card deals damage, its owner hero is healed for that amount. |

---

## Effects

An effect is a **(trigger, type, target)** triple (`CardEffect`).

**Triggers** (`EffectTrigger`) — *when* it fires:

| Trigger | Fires when… |
|---|---|
| `OnPlay` | the card is played (default for spells) |
| `OnDeath` | the card dies |
| `OnTurnStart` | the owner's turn begins |
| `OnDamageDeal` | after this card deals damage |
| `OnDamageTake` | after this card takes damage |

**Types** (`EffectType`) — *what* it does:

| Type | Action |
|---|---|
| `DealDamage` / `Heal` | damage / heal the target |
| `DrawCards` | draw for the owner |
| `BuffAttack` / `BuffHealth` / `BuffStats` | raise attack / health / both |
| `DebuffAttack` | lower a card's attack |
| `Silence` | remove all abilities (and the card text) from the target |
| `Destroy` | destroy the target(s) |
| `Summon` | summon a token onto the board |
| `AddKeyword` | grant a keyword (e.g. Shield, Provocation) |
| `GainTemporaryMana` / `…NextTurn` | temporary mana this turn / next turn |

**Targets** (`EffectTarget`) — *who* it hits:

`None` · `SelfCard` · `SelfHero` · `EnemyHero` · `SelectedAllyCard` · `SelectedEnemyCard` ·
`SelectedAllyCharacter` · `SelectedEnemyCharacter` · `AllAllyCards` · `OtherAllyCards` ·
`AllEnemyCards` · `AllCards`
(*"Character"* = a card **or** a hero.)

---

## Deck presets

Four built-in 30-card decks are used across all experiments (tuning, comparison, ladder):

| Preset | Archetype |
|---|---|
| **Aggro** | low-curve, fast pressure — race the opponent's hero down |
| **Control** | high-value cards and removal — survive, then win late |
| **Midrange** | balanced curve — flexible tempo |
| **Token** | many cheap minions — go wide and buff the board |

---

## Card set

A frozen set of **collectible cards across three tiers** — **Early**, **Midgame**, **Late** —
defined in `Assets/Resources/CardsInfo/AllCards/*.json`. Summoned tokens and the Coin are
non-collectible and never appear in decks.