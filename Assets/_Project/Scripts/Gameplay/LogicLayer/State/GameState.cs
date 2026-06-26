using System;

[Serializable]
public class GameState
{
    public int TurnNumber;
    public int ActionIndex;

    public PlayerSide ActiveSide;

    public bool IsGameOver;
    public PlayerSide? Winner;

    public PlayerState Player;
    public PlayerState Enemy;

    public int NextInstanceId;

    public AllCards CardDatabase;

    public PlayerState GetPlayerState(PlayerSide side)
    {
        return side == PlayerSide.Player
            ? Player
            : Enemy;
    }

    public PlayerState GetOpponentState(PlayerSide side)
    {
        return side == PlayerSide.Player
            ? Enemy
            : Player;
    }

    public PlayerSide GetOpponentSide(PlayerSide side)
    {
        return side == PlayerSide.Player
            ? PlayerSide.Enemy
            : PlayerSide.Player;
    }

    public int GenerateInstanceId()
    {
        int id = NextInstanceId;
        NextInstanceId++;
        return id;
    }

    public GameState GetDeepCopy()
    {
        return new GameState
        {
            TurnNumber = TurnNumber,
            ActionIndex = ActionIndex,
            ActiveSide = ActiveSide,

            IsGameOver = IsGameOver,
            Winner = Winner,

            Player = Player != null ? Player.GetDeepCopy() : null,
            Enemy = Enemy != null ? Enemy.GetDeepCopy() : null,

            NextInstanceId = NextInstanceId,

            CardDatabase = CardDatabase
        };
    }
}
