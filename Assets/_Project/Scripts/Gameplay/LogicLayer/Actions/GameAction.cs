using System;

/// <summary>
/// Represents one concrete game action.
/// 
/// Example:
/// - PlayCard: source = card from hand, target = optional card or hero
/// - AttackCard: source = attacking card, target = defending card
/// - AttackHero: source = attacking card, target = enemy hero
/// - EndTurn: source = 0, target = none
/// </summary>
public class GameAction
{
    /// <summary>
    /// Side that performs this action.
    /// </summary>
    public PlayerSide ActorSide { get; }

    /// <summary>
    /// Type of action that should be performed.
    /// </summary>
    public GameActionType Type { get; }

    /// <summary>
    /// Instance ID of the card that performs the action.
    /// For PlayCard this is the card from hand.
    /// For AttackCard and AttackHero this is the attacking card.
    /// For EndTurn this can be 0.
    /// </summary>
    public int SourceInstanceId { get; }

    /// <summary>
    /// Defines whether this action targets nothing, a card, or a hero.
    /// </summary>
    public PlayTargetType TargetType { get; }

    /// <summary>
    /// Optional target card instance ID.
    /// Used only when TargetType == PlayTargetType.Card.
    /// </summary>
    public int? TargetInstanceId { get; }

    /// <summary>
    /// Optional target hero side.
    /// Used only when TargetType == PlayTargetType.Hero.
    /// </summary>
    public PlayerSide? TargetHeroSide { get; }

    /// <summary>Optional battlefield slot index where a played/summoned entity should be inserted.</summary>
    public int? FieldIndex { get; }

    /// <summary>
    /// Creates a new game action.
    /// </summary>
    public GameAction(
        PlayerSide actorSide,
        GameActionType type,
        int sourceInstanceId = 0,
        int? targetInstanceId = null,
        PlayTargetType targetType = PlayTargetType.None,
        PlayerSide? targetHeroSide = null,
        int? fieldIndex = null)
    {
        ActorSide = actorSide;
        Type = type;
        SourceInstanceId = sourceInstanceId;
        TargetInstanceId = targetInstanceId;
        TargetHeroSide = targetHeroSide;

        FieldIndex = fieldIndex;

        if (targetInstanceId.HasValue && targetType == PlayTargetType.None)
        {
            TargetType = PlayTargetType.Card;
        }
        else if (targetHeroSide.HasValue && targetType == PlayTargetType.None)
        {
            TargetType = PlayTargetType.Hero;
        }
        else
        {
            TargetType = targetType;
        }
    }

    /// <summary>Builds a PlayCard action targeting a card on the battlefield.</summary>
    public static GameAction PlayCardOnCard(
        PlayerSide actorSide,
        int sourceInstanceId,
        int targetInstanceId,
        int? fieldIndex = null)
    {
        return new GameAction(
            actorSide,
            GameActionType.PlayCard,
            sourceInstanceId,
            targetInstanceId,
            PlayTargetType.Card,
            null,
            fieldIndex
        );
    }

    /// <summary>Builds a PlayCard action targeting a hero.</summary>
    public static GameAction PlayCardOnHero(
        PlayerSide actorSide,
        int sourceInstanceId,
        PlayerSide targetHeroSide,
        int? fieldIndex = null)
    {
        return new GameAction(
            actorSide,
            GameActionType.PlayCard,
            sourceInstanceId,
            null,
            PlayTargetType.Hero,
            targetHeroSide,
            fieldIndex
        );
    }

    /// <summary>Builds an AttackCard action: an attacker striking a defending card.</summary>
    public static GameAction AttackCard(
        PlayerSide actorSide,
        int attackerInstanceId,
        int defenderInstanceId)
    {
        return new GameAction(
            actorSide,
            GameActionType.AttackCard,
            attackerInstanceId,
            defenderInstanceId,
            PlayTargetType.Card
        );
    }

    /// <summary>Builds an AttackHero action: an attacker striking the enemy hero.</summary>
    public static GameAction AttackHero(
        PlayerSide actorSide,
        int attackerInstanceId,
        PlayerSide targetHeroSide)
    {
        return new GameAction(
            actorSide,
            GameActionType.AttackHero,
            attackerInstanceId,
            null,
            PlayTargetType.Hero,
            targetHeroSide
        );
    }

    /// <summary>Builds an EndTurn action for the given side.</summary>
    public static GameAction EndTurn(PlayerSide actorSide)
    {
        return new GameAction(
            actorSide,
            GameActionType.EndTurn
        );
    }

    public override string ToString()
    {
        string actionText;

        if (TargetType == PlayTargetType.Card && TargetInstanceId.HasValue)
        {
            actionText = $"{Type}: {SourceInstanceId} -> Card {TargetInstanceId.Value}";
        }
        else if (TargetType == PlayTargetType.Hero && TargetHeroSide.HasValue)
        {
            actionText = $"{Type}: {SourceInstanceId} -> Hero {TargetHeroSide.Value}";
        }
        else if (TargetType == PlayTargetType.Hero)
        {
            actionText = $"{Type}: {SourceInstanceId} -> Hero";
        }
        else
        {
            actionText = $"{Type}: {SourceInstanceId}";
        }

        return $"{ActorSide} | {actionText}";
    }
}