using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class PlayerState
{
    public PlayerSide Side;

    public int HP;
    public int MaxHP;

    public int Mana;
    public int ManaPool;
    public int PendingTemporaryMana;

    public int TurnsStarted;
    public int FatigueCounter;

    public List<CardInstance> Deck = new List<CardInstance>();
    public List<CardInstance> Hand = new List<CardInstance>();
    public List<CardInstance> Field = new List<CardInstance>();
    public List<CardInstance> Graveyard = new List<CardInstance>();

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
