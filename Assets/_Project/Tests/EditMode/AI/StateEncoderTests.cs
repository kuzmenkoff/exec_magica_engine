using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// StateEncoder across all layout versions: v1-400, v2-1216, v3-1616.
/// Covers feature lengths + dispatch, imperfect-information guarantees (opponent hand identities and
/// deck order never leak), the v2 effect block, and the v3 unseen-pool block + v2-prefix property.
/// </summary>
[TestFixture]
public class StateEncoderTests
{
    private static GameState Fresh(int seed = 42)
    {
        AllCards db = SyntheticDecks.Database();
        return GameStateBuilder.CreateInitialState(
            SyntheticDecks.Deck(), SyntheticDecks.Deck(), db, playerFirst: true, shuffleSeed: seed);
    }

    private static void Shuffle(List<CardInstance> deck, int seed)
    {
        System.Random r = new System.Random(seed);
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = r.Next(i + 1);
            CardInstance t = deck[i]; deck[i] = deck[j]; deck[j] = t;
        }
    }

    // ─────────────────────────────  v1 (400)  ─────────────────────────────

    [Test]
    public void V1_HasExpectedLength()
    {
        Assert.That(StateEncoder.FeatureSize, Is.EqualTo(400));
        Assert.That(StateEncoder.Encode(Fresh(), PlayerSide.Player).Length, Is.EqualTo(400));
    }

    /// <summary>The opponent's hand-card IDENTITIES are never encoded — only the count. Mutating a
    /// hidden hand card must not move a single feature.</summary>
    [Test]
    public void V1_HidesOpponentHandIdentity()
    {
        GameState s = Fresh();
        float[] before = StateEncoder.Encode(s, PlayerSide.Player);
        CardInstance c = s.Enemy.Hand[0];
        c.CardId += 1; c.Attack += 5; c.HP += 5;               // same count, different identity/stats
        float[] after = StateEncoder.Encode(s, PlayerSide.Player);
        Assert.That(after, Is.EqualTo(before), "v1 leaked opponent hand identity");
    }

    // ─────────────────────────────  v2 (1216)  ────────────────────────────

    [Test]
    public void V2_HasExpectedLength()
    {
        Assert.That(StateEncoder.FeatureSizeV2, Is.EqualTo(1216));
        Assert.That(StateEncoder.EncodeV2(Fresh(), PlayerSide.Player).Length, Is.EqualTo(1216));
    }

    [Test]
    public void V2_HidesOpponentHandIdentity()
    {
        GameState s = Fresh();
        float[] before = StateEncoder.EncodeV2(s, PlayerSide.Player);
        CardInstance c = s.Enemy.Hand[0];
        c.CardId += 1; c.Attack += 5; c.HP += 5;
        float[] after = StateEncoder.EncodeV2(s, PlayerSide.Player);
        Assert.That(after, Is.EqualTo(before), "v2 leaked opponent hand identity");
    }

    /// <summary>v2 adds a per-slot effect block: a non-silenced card with an effect must encode a
    /// non-zero effect block (v1 has no such block).</summary>
    [Test]
    public void V2_EncodesPrimaryEffectBlock()
    {
        GameState s = Fresh();
        CardInstance c = s.Player.Deck[0];
        s.Player.Deck.RemoveAt(0);
        c.IsSilenced = false;
        c.Effects = new List<CardEffect> { new CardEffect { Value = 5 } };   // default enums are valid (0)
        s.Player.Field.Insert(0, c);                                         // first my-field slot

        float[] v2 = StateEncoder.EncodeV2(s, PlayerSide.Player);
        int effBase = StateEncoder.HandSlots * StateEncoder.CardFeatureSizeV2 + StateEncoder.CardFeatureSize;
        float sum = 0f;
        for (int i = effBase; i < effBase + StateEncoder.CardEffectSize; i++) sum += v2[i];
        Assert.That(sum, Is.GreaterThan(0f), "v2 effect block empty for a card with an effect");
    }

    // ─────────────────────────────  v3 (1616)  ────────────────────────────

    [Test]
    public void V3_HasExpectedLength()
    {
        Assert.That(StateEncoder.FeatureSizeV3, Is.EqualTo(1616));
        Assert.That(StateEncoder.EncodeV3(Fresh(), PlayerSide.Player).Length, Is.EqualTo(1616));
    }

    /// <summary>The "one dataset, two nets" plan rests on this: a v3 row must contain a byte-identical
    /// v2 row in its first 1216 slots, so slicing trains a v2 net with no regeneration.</summary>
    [Test]
    public void V2_IsAStrictPrefixOfV3()
    {
        GameState s = Fresh();
        foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Enemy })
        {
            float[] v2 = StateEncoder.EncodeV2(s, side);
            float[] v3 = StateEncoder.EncodeV3(s, side);
            for (int i = 0; i < StateEncoder.FeatureSizeV2; i++)
                Assert.That(v3[i], Is.EqualTo(v2[i]), $"prefix broke at {i} for {side}");
        }
    }

    /// <summary>THE anti-cheat test. The player does not know deck ORDER — reshuffling either deck must
    /// not move a single feature. If this fails, the net is reading future draws.</summary>
    [Test]
    public void DeckOrder_DoesNotAffectEncoding()
    {
        GameState s = Fresh();
        float[] before = StateEncoder.EncodeV3(s, PlayerSide.Player);
        Shuffle(s.Player.Deck, 777);
        Shuffle(s.Enemy.Deck, 999);
        float[] after = StateEncoder.EncodeV3(s, PlayerSide.Player);
        for (int i = 0; i < after.Length; i++)
            Assert.That(after[i], Is.EqualTo(before[i]).Within(1e-6f), $"leak: feature {i} depends on deck order");
    }

    /// <summary>Drawing reveals nothing: the card moves from one hidden zone (deck) to another (hand),
    /// so the opponent pool must be unchanged.</summary>
    [Test]
    public void OpponentDraw_DoesNotChangeTheirPool()
    {
        GameState s = Fresh();
        float[] before = StateEncoder.EncodeV3(s, PlayerSide.Player);
        CardInstance drawn = s.Enemy.Deck[0];
        s.Enemy.Deck.RemoveAt(0);
        s.Enemy.Hand.Add(drawn);
        float[] after = StateEncoder.EncodeV3(s, PlayerSide.Player);
        int b = StateEncoder.FeatureSizeV2;
        for (int i = b; i < b + StateEncoder.PoolBlockSize; i++)
            Assert.That(after[i], Is.EqualTo(before[i]).Within(1e-6f),
                $"enemy pool moved on a draw (feature {i}) — deck and hand must be one bag");
    }

    /// <summary>Playing a card DOES reveal it: it leaves the hidden pool and appears on the field.</summary>
    [Test]
    public void OpponentPlay_ShrinksTheirPool()
    {
        GameState s = Fresh();
        float[] before = StateEncoder.EncodeV3(s, PlayerSide.Player);
        CardInstance played = s.Enemy.Hand[0];
        s.Enemy.Hand.RemoveAt(0);
        s.Enemy.Field.Add(played);
        float[] after = StateEncoder.EncodeV3(s, PlayerSide.Player);
        int b = StateEncoder.FeatureSizeV2;
        int bucket = played.ManaCost <= 2 ? 0 : played.ManaCost <= 4 ? 1 : played.ManaCost <= 6 ? 2 : 3;
        int occupied = b + bucket * StateEncoder.CardFeatureSizeV2 + 11;     // "occupied" flag sums into a count
        Assert.That(after[occupied], Is.LessThan(before[occupied]), "revealed card must leave the pool");
    }

    /// <summary>My own hand is visible and already in the slot block, so my pool is deck-only: drawing shrinks it.</summary>
    [Test]
    public void MyDraw_ShrinksMyPool()
    {
        GameState s = Fresh();
        float[] before = StateEncoder.EncodeV3(s, PlayerSide.Player);
        CardInstance drawn = s.Player.Deck[0];
        s.Player.Deck.RemoveAt(0);
        s.Player.Hand.Add(drawn);
        float[] after = StateEncoder.EncodeV3(s, PlayerSide.Player);
        int b = StateEncoder.FeatureSizeV2 + StateEncoder.PoolBlockSize;     // my pool block
        int bucket = drawn.ManaCost <= 2 ? 0 : drawn.ManaCost <= 4 ? 1 : drawn.ManaCost <= 6 ? 2 : 3;
        int occupied = b + bucket * StateEncoder.CardFeatureSizeV2 + 11;
        Assert.That(after[occupied], Is.LessThan(before[occupied]));
    }

    [Test]
    public void PoolBlocks_AreNotEmpty()
    {
        float[] v3 = StateEncoder.EncodeV3(Fresh(), PlayerSide.Player);
        float sum = 0f;
        for (int i = StateEncoder.FeatureSizeV2; i < StateEncoder.FeatureSizeV3; i++) sum += v3[i];
        Assert.That(sum, Is.GreaterThan(0f), "pool blocks are all zero — AddPool never ran");
    }

    // ─────────────────────────────  dispatch  ─────────────────────────────

    [Test]
    public void EncodeFor_DispatchesByInputSize()
    {
        GameState s = Fresh();
        Assert.That(StateEncoder.EncodeFor(400, s, PlayerSide.Player).Length, Is.EqualTo(400));
        Assert.That(StateEncoder.EncodeFor(1216, s, PlayerSide.Player).Length, Is.EqualTo(1216));
        Assert.That(StateEncoder.EncodeFor(1616, s, PlayerSide.Player).Length, Is.EqualTo(1616));
    }
}