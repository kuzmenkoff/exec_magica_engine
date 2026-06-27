using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One player's full state: hero health, mana, fatigue, and the four card zones
/// (deck, hand, field, graveyard).
/// </summary>
[Serializable]
public class PlayerState
{
    public PlayerSide Side;

    public int HP;
    public int MaxHP;

    /// <summary>Mana available to spend this turn.</summary>
    public int Mana;
    /// <summary>Refillable mana "crystals" (0..<see cref="GameRules.MaxManaPool"/>) that top up each turn.</summary>
    public int ManaPool;
    /// <summary>Temporary mana queued to be granted at the start of the next turn.</summary>
    public int PendingTemporaryMana;

    /// <summary>How many turns this player has begun (drives the mana-pool ramp).</summary>
    public int TurnsStarted;
    /// <summary>Fatigue hits taken so far; each empty-deck draw deals escalating self-damage.</summary>
    public int FatigueCounter;

    public List<CardInstance> Deck = new List<CardInstance>();
    public List<CardInstance> Hand = new List<CardInstance>();
    public List<CardInstance> Field = new List<CardInstance>();
    public List<CardInstance> Graveyard = new List<CardInstance>();

    /// <summary>Returns an independent deep copy of this player's state and all card zones.</summary>
    public PlayerState GetDeepCopy()
    {
        return new PlayerState
        {
            Side = Side,

            HP = HP,
            MaxHP = MaxHP,

            Mana = Mana,
            ManaPool = ManaPool,
            PendingTemporaryMana = PendingTemporaryMana,

            TurnsStarted = TurnsStarted,
            FatigueCounter = FatigueCounter,

            Deck = Deck != null
                ? Deck.Where(card => card != null).Select(card => card.GetDeepCopy()).ToList()
                : new List<CardInstance>(),

            Hand = Hand != null
                ? Hand.Where(card => card != null).Select(card => card.GetDeepCopy()).ToList()
                : new List<CardInstance>(),

            Field = Field != null
                ? Field.Where(card => card != null).Select(card => card.GetDeepCopy()).ToList()
                : new List<CardInstance>(),

            Graveyard = Graveyard != null
                ? Graveyard.Where(card => card != null).Select(card => card.GetDeepCopy()).ToList()
                : new List<CardInstance>()
        };
    }
}
