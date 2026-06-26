using System;

[Serializable]
public class GameTarget
{
    public PlayTargetType TargetType;
    public int? TargetInstanceId;
    public PlayerSide? TargetHeroSide;

    public static GameTarget None()
    {
        return new GameTarget
        {
            TargetType = PlayTargetType.None,
            TargetInstanceId = null,
            TargetHeroSide = null
        };
    }

    public static GameTarget Card(int targetInstanceId)
    {
        return new GameTarget
        {
            TargetType = PlayTargetType.Card,
            TargetInstanceId = targetInstanceId,
            TargetHeroSide = null
        };
    }

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
