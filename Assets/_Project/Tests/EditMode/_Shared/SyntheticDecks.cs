using System.Collections.Generic;

public static class SyntheticDecks
{
    public static readonly (int id, int atk, int hp, int cost)[] Curve =
    {
        (9001, 1, 2, 1), (9002, 2, 1, 1), (9003, 2, 3, 2), (9004, 3, 2, 2),
        (9005, 3, 4, 3), (9006, 4, 3, 3), (9007, 4, 5, 4), (9008, 5, 4, 4),
        (9009, 5, 6, 5), (9010, 6, 5, 5), (9011, 6, 7, 6), (9012, 7, 6, 6),
        (9013, 7, 8, 7), (9014, 8, 8, 8), (9015, 4, 4, 3),
    };

    public static Card Minion(int id, int atk, int hp, int cost) => new Card
    {
        id = id,
        Title = "VM" + id,
        Class = Card.CardClass.ENTITY,
        Attack = atk,
        HP = hp,
        MaxHP = hp,
        ManaCost = cost,
        IsCollectible = true,
        Keywords = new List<KeywordType>(),
        Effects = new List<CardEffect>()
    };

    public static AllCards Deck()
    {
        AllCards d = new AllCards { cards = new List<Card>() };
        foreach (var c in Curve) { d.cards.Add(Minion(c.id, c.atk, c.hp, c.cost)); d.cards.Add(Minion(c.id, c.atk, c.hp, c.cost)); }
        return d;
    }

    public static AllCards Database()
    {
        AllCards db = new AllCards { cards = new List<Card>() };
        foreach (var c in Curve) db.cards.Add(Minion(c.id, c.atk, c.hp, c.cost));
        return db;
    }
}
