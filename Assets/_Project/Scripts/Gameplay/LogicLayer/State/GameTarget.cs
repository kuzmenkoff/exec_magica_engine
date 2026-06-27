using System;

/// <summary>
/// Identifies the target of a targeted action or effect: nothing, a specific card
/// instance, or a hero. Use the factory helpers to build a well-formed target.
/// </summary>
[Serializable]
public class GameTarget
{
    public PlayTargetType TargetType;
    /// <summary>Target card's instance id when <see cref="TargetType"/> is Card; otherwise null.</summary>
    public int? TargetInstanceId;
    /// <summary>Target hero's side when <see cref="TargetType"/> is Hero; otherwise null.</summary>
    public PlayerSide? TargetHeroSide;

    /// <summary>A target meaning "no target".</summary>
    public static GameTarget None()
    {
        return new GameTarget
        {
            TargetType = PlayTargetType.None,
            TargetInstanceId = null,
            TargetHeroSide = null
        };
    }

    /// <summary>A target pointing at the card with the given instance id.</summary>
    public static GameTarget Card(int targetInstanceId)
    {
        return new GameTarget
        {
            TargetType = PlayTargetType.Card,
            TargetInstanceId = targetInstanceId,
            TargetHeroSide = null
        };
    }

    /// <summary>A target pointing at the given hero.</summary>
    public static GameTarget Hero(PlayerSide heroSide)
    {
        return new GameTarget
        {
            TargetType = PlayTargetType.Hero,
            TargetInstanceId = null,
            TargetHeroSide = heroSide
        };
    }
}
