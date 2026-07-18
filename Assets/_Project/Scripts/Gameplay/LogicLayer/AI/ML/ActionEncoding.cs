using System.Collections.Generic;

/// <summary>
/// Maps a <see cref="GameAction"/> to the flat policy index used by the trained network,
/// mirroring ml/train.py:action_index. Slot indices are positions within a zone list,
/// consistent with <see cref="StateEncoder"/> and <see cref="SelfPlayDataGenerator"/>.
///
/// Flat layout (ActionCount = 227):
///   End=0 | PlayCard(noTarget)=[1..10] | Play→myField=[11..80] | Play→oppField=[81..150]
///   | Play→myHero=[151..160] | Play→oppHero=[161..170]
///   | AttackCard=[171..219] | AttackHero=[220..226]
/// </summary>
public static class ActionEncoding
{
    public const int Hand = StateEncoder.HandSlots;    // 10
    public const int Field = StateEncoder.FieldSlots;  // 7

    public const int OffEnd = 0;
    public const int OffPlayNone = OffEnd + 1;                  // 1
    public const int OffPlayMyF = OffPlayNone + Hand;          // 11
    public const int OffPlayOpF = OffPlayMyF + Hand * Field;   // 81
    public const int OffPlayMyH = OffPlayOpF + Hand * Field;   // 151
    public const int OffPlayOpH = OffPlayMyH + Hand;           // 161
    public const int OffAtkCard = OffPlayOpH + Hand;           // 171
    public const int OffAtkHero = OffAtkCard + Field * Field;  // 220
    public const int ActionCount = OffAtkHero + Field;          // 227

    /// <summary>Flat index from a descriptor. Returns -1 for out-of-range slots (treat as illegal).</summary>
    public static int Index(int type, int src, int tgtZone, int tgtIdx)
    {
        switch (type)
        {
            case 0: return OffEnd;
            case 1:
                if (src < 0 || src >= Hand) return -1;
                switch (tgtZone)
                {
                    case 0: return OffPlayNone + src;
                    case 1: return InField(tgtIdx) ? OffPlayMyF + src * Field + tgtIdx : -1;
                    case 2: return InField(tgtIdx) ? OffPlayOpF + src * Field + tgtIdx : -1;
                    case 3: return OffPlayMyH + src;
                    case 4: return OffPlayOpH + src;
                    default: return -1;
                }
            case 2: return (InField(src) && InField(tgtIdx)) ? OffAtkCard + src * Field + tgtIdx : -1;
            case 3: return InField(src) ? OffAtkHero + src : -1;
            default: return -1;
        }
    }

    private static bool InField(int i) => i >= 0 && i < Field;

    /// <summary>Computes the [type, src, tgtZone, tgtIdx] descriptor for an action from <paramref name="me"/>'s POV.</summary>
    public static void Descriptor(GameAction a, GameState s, PlayerSide me,
                                  out int type, out int src, out int tgtZone, out int tgtIdx)
    {
        PlayerState meP = s.GetPlayerState(me);
        PlayerState opP = s.GetOpponentState(me);
        type = 0; src = -1; tgtZone = 0; tgtIdx = -1;

        switch (a.Type)
        {
            case GameActionType.PlayCard: type = 1; src = SlotOf(meP.Hand, a.SourceInstanceId); break;
            case GameActionType.AttackCard: type = 2; src = SlotOf(meP.Field, a.SourceInstanceId); break;
            case GameActionType.AttackHero: type = 3; src = SlotOf(meP.Field, a.SourceInstanceId); break;
        }

        if (a.TargetType == PlayTargetType.Hero && a.TargetHeroSide.HasValue)
            tgtZone = a.TargetHeroSide.Value == me ? 3 : 4;
        else if (a.TargetType == PlayTargetType.Card && a.TargetInstanceId.HasValue)
        {
            int idx = SlotOf(meP.Field, a.TargetInstanceId.Value);
            if (idx >= 0) { tgtZone = 1; tgtIdx = idx; }
            else { tgtZone = 2; tgtIdx = SlotOf(opP.Field, a.TargetInstanceId.Value); }
        }
    }

    /// <summary>Flat policy index for an action, or -1 if it doesn't map (out-of-range slot).</summary>
    public static int IndexOf(GameAction a, GameState s, PlayerSide me)
    {
        Descriptor(a, s, me, out int t, out int src, out int tz, out int ti);
        return Index(t, src, tz, ti);
    }

    private static int SlotOf(List<CardInstance> zone, int instanceId)
    {
        if (zone == null) return -1;
        for (int i = 0; i < zone.Count; i++)
            if (zone[i] != null && zone[i].InstanceId == instanceId) return i;
        return -1;
    }
}