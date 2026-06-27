using System.Collections.Generic;

/// <summary>
/// Generates legal actions from pure GameState.
/// </summary>
public static class CoreLegalActionGenerator
{
    /// <summary>Returns all legal actions for the side whose turn it is (empty if the game is over).</summary>
    public static List<GameAction> GetLegalActions(GameState state)
    {
        if (state == null || state.IsGameOver)
            return new List<GameAction>();

        return GetLegalActions(state, state.ActiveSide);
    }

    /// <summary>
    /// Returns every legal action for the given side — playable cards (respecting mana, the field
    /// cap and target requirements), attacks (respecting Provocation and attack eligibility), and EndTurn.
    /// </summary>
    public static List<GameAction> GetLegalActions(GameState state, PlayerSide side)
    {
        List<GameAction> actions = new List<GameAction>();

        if (state == null || state.IsGameOver)
            return actions;

        PlayerState actingPlayer = state.GetPlayerState(side);
        PlayerState opponentPlayer = state.GetOpponentState(side);

        if (actingPlayer == null || opponentPlayer == null)
            return actions;

        AddPlayableCards(actions, actingPlayer, opponentPlayer);
        AddAttackActions(actions, actingPlayer, opponentPlayer);

        actions.Add(new GameAction(
            side,
            GameActionType.EndTurn
        ));

        return actions;
    }

    private static void AddPlayableCards(
        List<GameAction> actions,
        PlayerState actingPlayer,
        PlayerState opponentPlayer)
    {
        if (actions == null || actingPlayer == null || opponentPlayer == null)
            return;

        if (actingPlayer.Hand == null)
            return;

        foreach (CardInstance card in actingPlayer.Hand)
        {
            if (card == null)
                continue;

            if (card.ManaCost > actingPlayer.Mana)
                continue;

            if (card.IsEntity && actingPlayer.Field.Count >= GameRules.MaxCardsOnField)
                continue;

            PlayTargetRequirement targetRequirement =
                PlayTargetRequirementResolver.GetRequirement(card);

            switch (targetRequirement)
            {
                case PlayTargetRequirement.None:
                    actions.Add(new GameAction(
                        actingPlayer.Side,
                        GameActionType.PlayCard,
                        card.InstanceId
                    ));
                    break;

                case PlayTargetRequirement.AllyCard:
                    AddCardTargetPlayActions(
                        actions,
                        actingPlayer.Side,
                        card,
                        actingPlayer.Field,
                        allowNoTargetForEntityIfNoTargets: true
                    );
                    break;

                case PlayTargetRequirement.EnemyCard:
                    AddCardTargetPlayActions(
                        actions,
                        actingPlayer.Side,
                        card,
                        opponentPlayer.Field,
                        allowNoTargetForEntityIfNoTargets: true
                    );
                    break;

                case PlayTargetRequirement.AllyCharacter:
                    AddCardTargetPlayActions(
                        actions,
                        actingPlayer.Side,
                        card,
                        actingPlayer.Field,
                        allowNoTargetForEntityIfNoTargets: false
                    );

                    actions.Add(GameAction.PlayCardOnHero(
                        actingPlayer.Side,
                        card.InstanceId,
                        actingPlayer.Side
                    ));
                    break;

                case PlayTargetRequirement.EnemyCharacter:
                    AddCardTargetPlayActions(
                        actions,
                        actingPlayer.Side,
                        card,
                        opponentPlayer.Field,
                        allowNoTargetForEntityIfNoTargets: false
                    );

                    actions.Add(GameAction.PlayCardOnHero(
                        actingPlayer.Side,
                        card.InstanceId,
                        opponentPlayer.Side
                    ));
                    break;
            }
        }
    }

    private static void AddCardTargetPlayActions(
        List<GameAction> actions,
        PlayerSide actorSide,
        CardInstance sourceCard,
        List<CardInstance> possibleTargets,
        bool allowNoTargetForEntityIfNoTargets)
    {
        bool hasTargets = false;

        if (possibleTargets != null)
        {
            foreach (CardInstance target in possibleTargets)
            {
                if (target == null)
                    continue;

                if (target.Zone != GameZone.Field)
                    continue;

                if (target.InstanceId == sourceCard.InstanceId)
                    continue;

                hasTargets = true;

                actions.Add(GameAction.PlayCardOnCard(
                    actorSide,
                    sourceCard.InstanceId,
                    target.InstanceId
                ));
            }
        }

        if (!hasTargets &&
            allowNoTargetForEntityIfNoTargets &&
            sourceCard.IsEntity)
        {
            actions.Add(new GameAction(
                actorSide,
                GameActionType.PlayCard,
                sourceCard.InstanceId
            ));
        }
    }

    private static void AddAttackActions(
        List<GameAction> actions,
        PlayerState actingPlayer,
        PlayerState opponentPlayer)
    {
        if (actions == null || actingPlayer == null || opponentPlayer == null)
            return;

        if (actingPlayer.Field == null)
            return;

        bool opponentHasProvocation = HasProvocation(opponentPlayer.Field);

        foreach (CardInstance attacker in actingPlayer.Field)
        {
            if (!CanAttack(attacker))
                continue;

            if (opponentPlayer.Field != null)
            {
                foreach (CardInstance defender in opponentPlayer.Field)
                {
                    if (defender == null || defender.Zone != GameZone.Field)
                        continue;

                    if (opponentHasProvocation && !KeywordService.HasKeyword(defender, KeywordType.Provocation))
                        continue;

                    actions.Add(GameAction.AttackCard(
                        actingPlayer.Side,
                        attacker.InstanceId,
                        defender.InstanceId
                    ));
                }
            }

            if (!opponentHasProvocation && !attacker.CanAttackOnlyCardsThisTurn)
            {
                actions.Add(GameAction.AttackHero(
                    actingPlayer.Side,
                    attacker.InstanceId,
                    opponentPlayer.Side
                ));
            }
        }
    }

    private static bool CanAttack(CardInstance card)
    {
        if (card == null)
            return false;

        if (!card.IsEntity)
            return false;

        if (card.Zone != GameZone.Field)
            return false;

        if (!card.CanAttack)
            return false;

        if (card.Attack <= 0)
            return false;

        if (card.RemainingAttacksThisTurn <= 0)
            return false;

        return true;
    }

    private static bool HasProvocation(List<CardInstance> cards)
    {
        if (cards == null)
            return false;

        foreach (CardInstance card in cards)
        {
            if (card == null || card.Zone != GameZone.Field)
                continue;

            if (KeywordService.HasKeyword(card, KeywordType.Provocation))
                return true;
        }

        return false;
    }

}
