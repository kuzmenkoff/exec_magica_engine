using System;
using System.Collections.Generic;

/// <summary>
/// Encodes a <see cref="GameState"/> into a fixed-length feature vector for the ML agent.
/// Always encoded from the acting player's point of view ("me" = the player passed in), using
/// PUBLIC information only — the opponent's hand is represented by its card count, never its cards.
///
/// Layout (FeatureSize = 400 floats):
///   [0   .. 159]  my hand   : 10 slots x 16
///   [160 .. 271]  my field  : 7 slots x 16
///   [272 .. 383]  opp field : 7 slots x 16
///   [384 .. 399]  globals   : 16 scalars
///
/// Per-card slot (16 floats):
///   0 manaCost/10, 1 attack/15, 2 hp/15, 3 maxHp/15,
///   4 Provocation, 5 Shield, 6 Charge, 7 Rush, 8 DoubleAttack, 9 Lifesteal,
///   10 isSpell, 11 occupied, 12 canAttack, 13 remainingAttacks/2, 14 isSilenced, 15 hasEffects
///
/// Globals (16): myHP/30, oppHP/30, myMana/10, myPool/10, myPending/10, oppMana/10, oppPool/10,
///   myFatigue/10, oppFatigue/10, myDeck/30, oppDeck/30, myHand/10, oppHand/10, myField/7, oppField/7, turn/30
/// </summary>
public static class StateEncoder
{
    public const int CardFeatureSize = 16;
    public const int HandSlots = 10;
    public const int FieldSlots = 7;
    public const int GlobalSize = 16;

    public const int FeatureSize =
        HandSlots * CardFeatureSize + FieldSlots * CardFeatureSize + FieldSlots * CardFeatureSize + GlobalSize;
    public const string LayoutVersion = "v1-400";

    private const float StatNorm = 15f;
    private const float ManaNorm = 10f;
    private const float DeckNorm = 30f;
    private const float HandNorm = 10f;
    private const float FieldNorm = 7f;
    private const float FatigueNorm = 10f;
    private const float TurnNorm = 30f;
    private const float HeroHpNorm = 30f;

    public const int CardEffectSize = 34;                                   // 5 trig + 14 type + 12 target + 3 vals
    public const int CardFeatureSizeV2 = CardFeatureSize + CardEffectSize;  // 16 + 34 = 50
    public const int CardSlots = HandSlots + FieldSlots + FieldSlots;       // 24
    public const int FeatureSizeV2 =
        HandSlots * CardFeatureSizeV2 + FieldSlots * CardFeatureSizeV2 + FieldSlots * CardFeatureSizeV2 + GlobalSize; // 1216
    public const string LayoutVersionV2 = "v2-1216";
    private const float EffectValueNorm = 5f;

    // ─── v3: пулы невидимых карт ────────────────────────────────────────────
    // Порядок колоды НЕ кодируется (игрок его не знает) → блок перестановочно-
    // инвариантен: карты пула СУММИРУЮТСЯ внутри корзины по мане.
    public const int PoolBuckets = 4;                                    // 0-2 / 3-4 / 5-6 / 7+
    public const int PoolBlockSize = PoolBuckets * CardFeatureSizeV2;    // 4 * 50 = 200
    public const int FeatureSizeV3 = FeatureSizeV2 + 2 * PoolBlockSize;  // 1216 + 400 = 1616
    public const string LayoutVersionV3 = "v3-1616";
    private const float PoolNorm = 10f;

    [ThreadStatic] private static float[] _cardScratch;

    /// <summary>Encodes the state from <paramref name="me"/>'s perspective into a length-<see cref="FeatureSize"/> vector.</summary>
    public static float[] Encode(GameState state, PlayerSide me)
    {
        float[] f = new float[FeatureSize];
        if (state == null)
            return f;

        PlayerState meP = state.GetPlayerState(me);
        PlayerState opP = state.GetOpponentState(me);
        if (meP == null || opP == null)
            return f;

        int o = 0;
        EncodeZone(meP.Hand, f, ref o, HandSlots);   // my hand
        EncodeZone(meP.Field, f, ref o, FieldSlots);  // my field
        EncodeZone(opP.Field, f, ref o, FieldSlots);  // opponent field (public)
        EncodeGlobals(state, meP, opP, f, o);         // globals
        return f;
    }

    /// <summary>Picks the encoder matching a model's input size (v1-400 vs v2-1216).</summary>
    public static float[] EncodeFor(int featureSize, GameState state, PlayerSide me)
        => featureSize == FeatureSizeV3 ? EncodeV3(state, me)
         : featureSize == FeatureSizeV2 ? EncodeV2(state, me)
         : Encode(state, me);

    private static void EncodeZone(List<CardInstance> zone, float[] f, ref int o, int slots)
    {
        int n = zone != null ? Math.Min(zone.Count, slots) : 0;
        for (int i = 0; i < n; i++)
        {
            CardInstance c = zone[i];
            if (c != null)
                EncodeCard(c, f, o + i * CardFeatureSize);
        }
        o += slots * CardFeatureSize;
    }

    private static void EncodeCard(CardInstance c, float[] f, int o)
    {
        f[o + 0] = c.ManaCost / ManaNorm;
        f[o + 1] = c.Attack / StatNorm;
        f[o + 2] = c.HP / StatNorm;
        f[o + 3] = c.MaxHP / StatNorm;
        f[o + 4] = Kw(c, KeywordType.Provocation);
        f[o + 5] = Kw(c, KeywordType.Shield);
        f[o + 6] = Kw(c, KeywordType.Charge);
        f[o + 7] = Kw(c, KeywordType.Rush);
        f[o + 8] = Kw(c, KeywordType.DoubleAttack);
        f[o + 9] = Kw(c, KeywordType.Lifesteal);
        f[o + 10] = c.IsSpell ? 1f : 0f;
        f[o + 11] = 1f; // occupied
        f[o + 12] = c.CanAttack ? 1f : 0f;
        f[o + 13] = c.RemainingAttacksThisTurn / 2f;
        f[o + 14] = c.IsSilenced ? 1f : 0f;
        f[o + 15] = (c.Effects != null && c.Effects.Count > 0) ? 1f : 0f;
    }

    private static void EncodeGlobals(GameState s, PlayerState me, PlayerState op, float[] f, int o)
    {
        f[o + 0] = me.HP / HeroHpNorm;
        f[o + 1] = op.HP / HeroHpNorm;
        f[o + 2] = me.Mana / ManaNorm;
        f[o + 3] = me.ManaPool / ManaNorm;
        f[o + 4] = me.PendingTemporaryMana / ManaNorm;
        f[o + 5] = op.Mana / ManaNorm;
        f[o + 6] = op.ManaPool / ManaNorm;
        f[o + 7] = me.FatigueCounter / FatigueNorm;
        f[o + 8] = op.FatigueCounter / FatigueNorm;
        f[o + 9] = Count(me.Deck) / DeckNorm;
        f[o + 10] = Count(op.Deck) / DeckNorm;
        f[o + 11] = Count(me.Hand) / HandNorm;
        f[o + 12] = Count(op.Hand) / HandNorm;   // opponent hand: COUNT only (hidden contents)
        f[o + 13] = Count(me.Field) / FieldNorm;
        f[o + 14] = Count(op.Field) / FieldNorm;
        f[o + 15] = s.TurnNumber / TurnNorm;
    }

    private static float Kw(CardInstance c, KeywordType k) => KeywordService.HasKeyword(c, k) ? 1f : 0f;

    private static int Count(List<CardInstance> list) => list != null ? list.Count : 0;

    /// <summary>v2 encoder: v1 base block (16) + primary-effect block (34) per slot → 1216.</summary>
    public static float[] EncodeV2(GameState state, PlayerSide me)
    {
        float[] f = new float[FeatureSizeV2];
        if (state == null) return f;
        PlayerState meP = state.GetPlayerState(me);
        PlayerState opP = state.GetOpponentState(me);
        if (meP == null || opP == null) return f;
        int o = 0;
        EncodeZoneV2(meP.Hand, f, ref o, HandSlots);
        EncodeZoneV2(meP.Field, f, ref o, FieldSlots);
        EncodeZoneV2(opP.Field, f, ref o, FieldSlots);
        EncodeGlobals(state, meP, opP, f, o);                 // reuse v1 globals (16)
        return f;
    }

    private static void EncodeZoneV2(List<CardInstance> zone, float[] f, ref int o, int slots)
    {
        int n = zone != null ? Math.Min(zone.Count, slots) : 0;
        for (int i = 0; i < n; i++)
            if (zone[i] != null) EncodeCardV2(zone[i], f, o + i * CardFeatureSizeV2);
        o += slots * CardFeatureSizeV2;
    }

    private static void EncodeCardV2(CardInstance c, float[] f, int o)
    {
        EncodeCard(c, f, o);                                  // base 16 floats (reuse v1) at o..o+15
        if (!c.IsSilenced && c.Effects != null && c.Effects.Count > 0 && c.Effects[0] != null)
            EncodeEffect(c.Effects[0], f, o + CardFeatureSize);   // 34 floats at o+16..o+49
    }

    // Effect block (34): [0..4] trigger, [5..18] type, [19..30] target one-hots; [31..33] Value/Attack/Health /5.
    private static void EncodeEffect(CardEffect e, float[] f, int b)
    {
        int tr = (int)e.Trigger; if (tr >= 0 && tr < 5) f[b + tr] = 1f;
        int ty = (int)e.Type; if (ty >= 0 && ty < 14) f[b + 5 + ty] = 1f;
        int tg = (int)e.Target; if (tg >= 0 && tg < 12) f[b + 19 + tg] = 1f;
        f[b + 31] = e.Value / EffectValueNorm;
        f[b + 32] = e.AttackValue / EffectValueNorm;
        f[b + 33] = e.HealthValue / EffectValueNorm;
    }

    /// <summary>
    /// v3 encoder: the full v2 block (1216) + two "unseen pool" blocks (200 each) → 1616.
    /// The v2 layout is a strict PREFIX of v3, so a v3 sample can train a v2 net by slicing [0..1216).
    ///
    /// Pools are permutation-invariant SUMS of per-card v2 vectors, bucketed by mana cost.
    /// Deck ORDER is never encoded: the player does not know it, and reading it would leak
    /// future draws (the MCTS teacher likewise reshuffles the pool when determinizing).
    ///
    /// Opponent pool = deck + hand (both hidden; with a known decklist this multiset is legal
    /// information). My pool = deck only — my hand is visible and already encoded in slots.
    /// </summary>
    public static float[] EncodeV3(GameState state, PlayerSide me)
    {
        float[] f = new float[FeatureSizeV3];
        if (state == null) return f;
        PlayerState meP = state.GetPlayerState(me);
        PlayerState opP = state.GetOpponentState(me);
        if (meP == null || opP == null) return f;

        int o = 0;
        EncodeZoneV2(meP.Hand, f, ref o, HandSlots);
        EncodeZoneV2(meP.Field, f, ref o, FieldSlots);
        EncodeZoneV2(opP.Field, f, ref o, FieldSlots);
        EncodeGlobals(state, meP, opP, f, o);
        o += GlobalSize;                          // o == FeatureSizeV2 → v2 prefix ends here

        AddPool(opP.Deck, f, o);                  // opponent: everything I cannot see
        AddPool(opP.Hand, f, o);
        o += PoolBlockSize;

        AddPool(meP.Deck, f, o);                  // me: what I can still draw
        return f;
    }

    /// <summary>Adds each card's v2 vector into its mana bucket. Summing (not slotting) is what
    /// makes the block order-free, so a shuffled deck yields the same encoding.</summary>
    private static void AddPool(List<CardInstance> zone, float[] f, int b)
    {
        if (zone == null) return;

        float[] card = _cardScratch ??= new float[CardFeatureSizeV2];

        for (int i = 0; i < zone.Count; i++)
        {
            CardInstance c = zone[i];
            if (c == null) continue;

            Array.Clear(card, 0, CardFeatureSizeV2);
            EncodeCardV2(c, card, 0);

            int o = b + ManaBucket(c.ManaCost) * CardFeatureSizeV2;
            for (int k = 0; k < CardFeatureSizeV2; k++)
                f[o + k] += card[k] / PoolNorm;
        }
    }

    private static int ManaBucket(int mana)
        => mana <= 2 ? 0 : mana <= 4 ? 1 : mana <= 6 ? 2 : 3;

    /// <summary>Per-slot CardId, same order as Encode/EncodeV2 (0 = empty). For future embeddings.</summary>
    public static int[] EncodeCardIds(GameState state, PlayerSide me)
    {
        int[] ids = new int[CardSlots];
        if (state == null) return ids;
        PlayerState meP = state.GetPlayerState(me);
        PlayerState opP = state.GetOpponentState(me);
        int o = 0;
        o = FillIds(meP?.Hand, ids, o, HandSlots);
        o = FillIds(meP?.Field, ids, o, FieldSlots);
        o = FillIds(opP?.Field, ids, o, FieldSlots);
        return ids;
    }

    private static int FillIds(List<CardInstance> zone, int[] ids, int o, int slots)
    {
        int n = zone != null ? Math.Min(zone.Count, slots) : 0;
        for (int i = 0; i < n; i++)
            if (zone[i] != null) ids[o + i] = zone[i].CardId;
        return o + slots;
    }
}
