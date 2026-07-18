using System;
using System.Collections.Generic;

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

            Deck = CopyZone(Deck),
            Hand = CopyZone(Hand),
            Field = CopyZone(Field),
            Graveyard = CopyZone(Graveyard)
        };
    }

    /// <summary>Clones a zone. Hand-rolled instead of LINQ: this runs once per zone per deep copy,
    /// and a deep copy runs on every MCTS iteration and every Greedy candidate action.</summary>
    private static List<CardInstance> CopyZone(List<CardInstance> src)
    {
        if (src == null) return new List<CardInstance>();

        List<CardInstance> dst = new List<CardInstance>(src.Count);   // exact capacity → one array, no regrowth
        for (int i = 0; i < src.Count; i++)
        {
            CardInstance c = src[i];
            if (c != null) dst.Add(c.GetDeepCopy());
        }
        return dst;
    }
}
