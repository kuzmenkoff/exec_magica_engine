using System;

/// <summary>
/// One atomic event produced by GameEngine.
/// This is not animation logic. It is a clean gameplay fact.
/// </summary>
[Serializable]
public class GameEvent
{
    public GameEventType Type;

    public int TurnNumber;
    public int ActionIndex;

    public PlayerSide ActorSide;

    public int? SourceInstanceId;
    public int? TargetInstanceId;
    public PlayerSide? TargetHeroSide;

    public int? FieldIndex;

    /// <summary>Numeric payload whose meaning depends on <see cref="Type"/> (e.g. damage, healing, mana amount).</summary>
    public int Value;
    public string Message;

    /// <summary>Builds an event, stamping it with the state's current turn and action index.</summary>
    public static GameEvent Create(
        GameState state,
        GameEventType type,
        PlayerSide actorSide,
        string message = null,
        int value = 0,
        int? sourceInstanceId = null,
        int? targetInstanceId = null,
        PlayerSide? targetHeroSide = null,
        int? fieldIndex = null)
    {
        return new GameEvent
        {
            Type = type,

            TurnNumber = state != null ? state.TurnNumber : 0,
            ActionIndex = state != null ? state.ActionIndex : 0,

            ActorSide = actorSide,

            SourceInstanceId = sourceInstanceId,
            TargetInstanceId = targetInstanceId,
            TargetHeroSide = targetHeroSide,

            FieldIndex = fieldIndex,

            Value = value,
            Message = message
        };
    }
}