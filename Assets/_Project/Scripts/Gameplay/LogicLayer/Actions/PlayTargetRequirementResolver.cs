using System.Collections.Generic;

/// <summary>
/// Resolves what kind of play target is required by a card's OnPlay effects.
/// 
/// This class is pure core logic and must not depend on Unity objects.
/// </summary>
public static class PlayTargetRequirementResolver
{
    public static PlayTargetRequirement GetRequirement(CardDefinition definition)
    {
        if (definition == null)
            return PlayTargetRequirement.None;

        return GetRequirementFromEffects(definition.Effects);
    }

    public static PlayTargetRequirement GetRequirement(CardInstance card)
    {
        if (card == null)
            return PlayTargetRequirement.None;

        return GetRequirementFromEffects(card.Effects);
    }

    public static PlayTargetRequirement GetRequirement(Card card)
    {
        if (card == null)
            return PlayTargetRequirement.None;

        return GetRequirementFromEffects(card.Effects);
    }

    private static PlayTargetRequirement GetRequirementFromEffects(
        IEnumerable<CardEffect> effects)
    {
        if (effects == null)
            return PlayTargetRequirement.None;

        foreach (CardEffect effect in effects)
        {
            PlayTargetRequirement requirement = GetRequirement(effect);

            if (requirement != PlayTargetRequirement.None)
                return requirement;
        }

        return PlayTargetRequirement.None;
    }

    public static PlayTargetRequirement GetRequirement(CardEffect effect)
    {
        if (effect == null || effect.Trigger != EffectTrigger.OnPlay)
            return PlayTargetRequirement.None;

        switch (effect.Target)
        {
            case EffectTarget.SelectedAllyCard:
                return PlayTargetRequirement.AllyCard;

            case EffectTarget.SelectedEnemyCard:
                return PlayTargetRequirement.EnemyCard;

            case EffectTarget.SelectedAllyCharacter:
                return PlayTargetRequirement.AllyCharacter;

            case EffectTarget.SelectedEnemyCharacter:
                return PlayTargetRequirement.EnemyCharacter;

            default:
                return PlayTargetRequirement.None;
        }
    }

    public static bool RequiresTarget(CardDefinition definition)
    {
        return GetRequirement(definition) != PlayTargetRequirement.None;
    }

    public static bool RequiresTarget(CardInstance card)
    {
        return GetRequirement(card) != PlayTargetRequirement.None;
    }

    public static bool RequiresTarget(Card card)
    {
        return GetRequirement(card) != PlayTargetRequirement.None;
    }
}
