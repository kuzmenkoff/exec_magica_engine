using System.Collections.Generic;

/// <summary>
/// Creates runtime card instances from immutable card definitions.
///
/// CardDefinition/Card describe the card loaded from JSON.
/// CardInstance is the concrete runtime card object used inside GameState.
/// </summary>
public static class CardInstanceFactory
{
    /// <summary>Creates a runtime <see cref="CardInstance"/> from a legacy <see cref="Card"/> for the given owner and zone.</summary>
    public static CardInstance Create(
        Card definition,
        int instanceId,
        PlayerSide ownerSide,
        GameZone zone)
    {
        if (definition == null)
            return null;

        return Create(
            definition.ToDefinition(),
            instanceId,
            ownerSide,
            zone
        );
    }

    /// <summary>Creates a runtime <see cref="CardInstance"/> from a <see cref="CardDefinition"/>, seeding combat flags and copying effects/keywords.</summary>
    public static CardInstance Create(
        CardDefinition definition,
        int instanceId,
        PlayerSide ownerSide,
        GameZone zone)
    {
        if (definition == null)
            return null;

        int maxHp = definition.MaxHP > 0
            ? definition.MaxHP
            : definition.HP;

        return new CardInstance
        {
            InstanceId = instanceId,
            CardId = definition.id,

            Title = definition.Title,
            Class = definition.Class,

            Attack = definition.Attack,
            HP = definition.HP,
            MaxHP = maxHp,
            ManaCost = definition.ManaCost,

            CanAttack = false,
            CanAttackOnlyCardsThisTurn = false,
            RemainingAttacksThisTurn = 0,

            IsPlaced = zone == GameZone.Field,

            OwnerSide = ownerSide,
            Zone = zone,

            Keywords = definition.Keywords != null
                ? new List<KeywordType>(definition.Keywords)
                : new List<KeywordType>(),

            Effects = definition.Effects != null
                ? new List<CardEffect>(definition.Effects)
                : new List<CardEffect>()
        };
    }
}
