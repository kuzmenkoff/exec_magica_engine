using System;

/// <summary>
/// The complete, serializable snapshot of a match — both players, whose turn it is, and
/// the outcome. This is the single source of truth the engine reads and mutates; AI agents
/// plan over deep copies of it.
/// </summary>
[Serializable]
public class GameState
{
    public int TurnNumber;
    /// <summary>Monotonic counter of actions applied so far (drives action caps and ordering).</summary>
    public int ActionIndex;

    public PlayerSide ActiveSide;

    public bool IsGameOver;
    /// <summary>Winning side, or null while the game is ongoing or ended in a draw.</summary>
    public PlayerSide? Winner;

    public PlayerState Player;
    public PlayerState Enemy;

    /// <summary>Source of the next unique card instance id; see <see cref="GenerateInstanceId"/>.</summary>
    public int NextInstanceId;

    /// <summary>Shared read-only card database (for summons/lookups); copied by reference, not deep-cloned.</summary>
    public AllCards CardDatabase;

    /// <summary>Returns the state for the given side.</summary>
    public PlayerState GetPlayerState(PlayerSide side)
    {
        return side == PlayerSide.Player
            ? Player
            : Enemy;
    }

    /// <summary>Returns the opponent's state relative to the given side.</summary>
    public PlayerState GetOpponentState(PlayerSide side)
    {
        return side == PlayerSide.Player
            ? Enemy
            : Player;
    }

    /// <summary>Returns the opposing side.</summary>
    public PlayerSide GetOpponentSide(PlayerSide side)
    {
        return side == PlayerSide.Player
            ? PlayerSide.Enemy
            : PlayerSide.Player;
    }

    /// <summary>Returns the next unique instance id and advances the counter.</summary>
    public int GenerateInstanceId()
    {
        int id = NextInstanceId;
        NextInstanceId++;
        return id;
    }

    /// <summary>Returns an independent deep copy of the state. The card database is shared by reference (it is read-only).</summary>
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
