/// <summary>
/// Validates whether a GameAction target satisfies a card's play target requirement.
/// </summary>
public static class PlayTargetValidator
{
    public static bool IsValidPlayTarget(
        GameState state,
        CardInstance source,
        GameAction action)
    {
        if (state == null || source == null || action == null)
            return false;

        PlayTargetRequirement requirement =
            PlayTargetRequirementResolver.GetRequirement(source);

        switch (requirement)
        {
            case PlayTargetRequirement.None:
                return action.TargetType == PlayTargetType.None;

            case PlayTargetRequirement.AllyCard:
                return IsValidAllyCardTarget(state, source, action);

            case PlayTargetRequirement.EnemyCard:
                return IsValidEnemyCardTarget(state, source, action);

            case PlayTargetRequirement.AllyCharacter:
                return IsValidAllyCharacterTarget(state, source, action);

            case PlayTargetRequirement.EnemyCharacter:
                return IsValidEnemyCharacterTarget(state, source, action);

            default:
                return false;
        }
    }

    public static bool HasAnyValidPlayTarget(
        GameState state,
        CardInstance source)
    {
        if (state == null || source == null)
            return false;

        PlayTargetRequirement requirement =
            PlayTargetRequirementResolver.GetRequirement(source);

        PlayerState owner = state.GetPlayerState(source.OwnerSide);
        PlayerState opponent = state.GetOpponentState(source.OwnerSide);

        if (owner == null || opponent == null)
            return false;

        switch (requirement)
        {
            case PlayTargetRequirement.None:
                return true;

            case PlayTargetRequirement.AllyCard:
                return HasValidCardTarget(owner.Field, source, source.OwnerSide);

            case PlayTargetRequirement.EnemyCard:
                return HasValidCardTarget(opponent.Field, source, state.GetOpponentSide(source.OwnerSide));

            case PlayTargetRequirement.AllyCharacter:
            case PlayTargetRequirement.EnemyCharacter:
                // Hero always exists.
                return true;

            default:
                return false;
        }
    }

    private static bool IsValidAllyCardTarget(
        GameState state,
        CardInstance source,
        GameAction action)
    {
        if (action.TargetType != PlayTargetType.Card || !action.TargetInstanceId.HasValue)
            return false;

        CardInstance target = GameStateCardFinder.FindCard(
            state,
            action.TargetInstanceId.Value
        );

        return IsValidCardTarget(target, source, source.OwnerSide);
    }

    private static bool IsValidEnemyCardTarget(
        GameState state,
        CardInstance source,
        GameAction action)
    {
        if (action.TargetType != PlayTargetType.Card || !action.TargetInstanceId.HasValue)
            return false;

        PlayerSide enemySide = state.GetOpponentSide(source.OwnerSide);

        CardInstance target = GameStateCardFinder.FindCard(
            state,
            action.TargetInstanceId.Value
        );

        return IsValidCardTarget(target, source, enemySide);
    }

    private static bool IsValidAllyCharacterTarget(
        GameState state,
        CardInstance source,
        GameAction action)
    {
        if (action.TargetType == PlayTargetType.Hero)
            return action.TargetHeroSide.HasValue &&
                   action.TargetHeroSide.Value == source.OwnerSide;

        return IsValidAllyCardTarget(state, source, action);
    }

    private static bool IsValidEnemyCharacterTarget(
        GameState state,
        CardInstance source,
        GameAction action)
    {
        PlayerSide enemySide = state.GetOpponentSide(source.OwnerSide);

        if (action.TargetType == PlayTargetType.Hero)
            return action.TargetHeroSide.HasValue &&
                   action.TargetHeroSide.Value == enemySide;

        return IsValidEnemyCardTarget(state, source, action);
    }

    private static bool HasValidCardTarget(
        System.Collections.Generic.List<CardInstance> cards,
        CardInstance source,
        PlayerSide requiredOwner)
    {
        if (cards == null)
            return false;

        foreach (CardInstance card in cards)
        {
            if (IsValidCardTarget(card, source, requiredOwner))
                return true;
        }

        return false;
    }

    private static bool IsValidCardTarget(
        CardInstance target,
        CardInstance source,
        PlayerSide requiredOwner)
    {
        if (target == null || source == null)
            return false;

        if (target.InstanceId == source.InstanceId)
            return false;

        if (target.OwnerSide != requiredOwner)
            return false;

        if (target.Zone != GameZone.Field)
            return false;

        if (!target.IsPlaced)
            return false;

        return true;
    }
}
