using System.Collections.Generic;

/// <summary>
/// Pure gameplay engine.
/// 
/// This class must not depend on Unity objects:
/// no MonoBehaviour, no GameObject, no Transform, no UI, no coroutines.
/// </summary>
public class GameEngine
{
    /// <summary>The current game state this engine reads and mutates.</summary>
    public GameState State { get; private set; }

    /// <summary>Creates an engine wrapping the given initial state (build it with <see cref="GameStateBuilder"/>).</summary>
    public GameEngine(GameState initialState)
    {
        State = initialState;
    }

    /// <summary>Returns the legal actions for the side whose turn it is.</summary>
    public List<GameAction> GetLegalActions()
    {
        return CoreLegalActionGenerator.GetLegalActions(State);
    }

    /// <summary>Returns the legal actions available to the given side.</summary>
    public List<GameAction> GetLegalActions(PlayerSide side)
    {
        return CoreLegalActionGenerator.GetLegalActions(State, side);
    }

    /// <summary>
    /// Applies one action to the state: validates it (game not over, correct actor, legality),
    /// mutates the state, and returns a <see cref="GameStepResult"/> with the emitted event stream.
    /// On any violation the state is left unchanged and a failure result is returned.
    /// </summary>
    public GameStepResult ApplyAction(GameAction action)
    {
        if (State == null)
            return GameStepResult.Failure("GameState is null.");

        if (action == null)
            return GameStepResult.Failure("GameAction is null.");

        if (State.IsGameOver)
            return GameStepResult.Failure("Game is already over.");

        if (action.ActorSide != State.ActiveSide)
            return GameStepResult.Failure(
                $"Wrong actor side. Active side is {State.ActiveSide}, action side is {action.ActorSide}."
            );

        switch (action.Type)
        {
            case GameActionType.EndTurn:
                return ApplyEndTurn(action);

            case GameActionType.PlayCard:
                return ApplyPlayCard(action);

            case GameActionType.AttackCard:
                return ApplyAttackCard(action);

            case GameActionType.AttackHero:
                return ApplyAttackHero(action);

            default:
                return GameStepResult.Failure(
                    $"Action type {action.Type} is not implemented in pure GameEngine yet."
                );
        }
    }

    private GameStepResult ApplyAttackCard(GameAction action)
    {
        List<GameEvent> events = new List<GameEvent>();

        PlayerState actingPlayer = State.GetPlayerState(action.ActorSide);
        PlayerState opponentPlayer = State.GetOpponentState(action.ActorSide);

        if (actingPlayer == null || opponentPlayer == null)
            return GameStepResult.Failure("Player state is null.");

        CardInstance attacker = GameStateCardFinder.FindCardInList(
            actingPlayer.Field,
            action.SourceInstanceId
        );

        if (attacker == null)
            return GameStepResult.Failure($"Attacker {action.SourceInstanceId} was not found on actor field.");

        if (!action.TargetInstanceId.HasValue)
            return GameStepResult.Failure("AttackCard target is missing.");

        CardInstance defender = GameStateCardFinder.FindCardInList(
            opponentPlayer.Field,
            action.TargetInstanceId.Value
        );

        if (defender == null)
            return GameStepResult.Failure($"Defender {action.TargetInstanceId.Value} was not found on opponent field.");

        string validationError = ValidateAttackerCanAttack(attacker);

        if (!string.IsNullOrEmpty(validationError))
            return GameStepResult.Failure(validationError);

        if (!CanAttackTargetCard(opponentPlayer, defender))
            return GameStepResult.Failure($"{attacker.Title} cannot attack {defender.Title}: Provocation blocks this attack.");

        ConsumeAttack(attacker);

        events.Add(GameEvent.Create(
            State,
            GameEventType.CardAttacked,
            action.ActorSide,
            $"{attacker.Title} attacked {defender.Title}.",
            0,
            sourceInstanceId: attacker.InstanceId,
            targetInstanceId: defender.InstanceId
        ));

        EffectEngine.DealDamageToCard(
            State,
            attacker,
            defender,
            attacker.Attack,
            events
        );

        if (defender.Attack > 0)
        {
            EffectEngine.DealDamageToCard(
                State,
                defender,
                attacker,
                defender.Attack,
                events
            );
        }

        EffectEngine.ResolveDeaths(State, events);

        State.ActionIndex++;

        return GameStepResult.SuccessResult(events);
    }

    private GameStepResult ApplyAttackHero(GameAction action)
    {
        List<GameEvent> events = new List<GameEvent>();

        PlayerState actingPlayer = State.GetPlayerState(action.ActorSide);

        if (actingPlayer == null)
            return GameStepResult.Failure("Acting player state is null.");

        CardInstance attacker = GameStateCardFinder.FindCardInList(
            actingPlayer.Field,
            action.SourceInstanceId
        );

        if (attacker == null)
            return GameStepResult.Failure($"Attacker {action.SourceInstanceId} was not found on actor field.");

        string validationError = ValidateAttackerCanAttack(attacker);

        if (!string.IsNullOrEmpty(validationError))
            return GameStepResult.Failure(validationError);

        if (attacker.CanAttackOnlyCardsThisTurn)
            return GameStepResult.Failure($"{attacker.Title} can attack only cards this turn.");

        PlayerSide targetHeroSide = action.TargetHeroSide.HasValue
            ? action.TargetHeroSide.Value
            : State.GetOpponentSide(action.ActorSide);

        if (targetHeroSide == action.ActorSide)
            return GameStepResult.Failure($"{attacker.Title} cannot attack own hero.");

        PlayerState targetPlayer = State.GetPlayerState(targetHeroSide);

        if (targetPlayer == null)
            return GameStepResult.Failure("Target hero state is null.");

        PlayerState opponentPlayer = State.GetOpponentState(action.ActorSide);

        if (HasProvocation(opponentPlayer))
            return GameStepResult.Failure($"{attacker.Title} cannot attack hero while enemy has Provocation.");

        ConsumeAttack(attacker);

        events.Add(GameEvent.Create(
            State,
            GameEventType.HeroAttacked,
            action.ActorSide,
            $"{attacker.Title} attacked {targetHeroSide} hero.",
            attacker.Attack,
            sourceInstanceId: attacker.InstanceId,
            targetHeroSide: targetHeroSide
        ));

        EffectEngine.DealDamageToHero(
            State,
            attacker,
            targetHeroSide,
            attacker.Attack,
            events
        );

        State.ActionIndex++;

        return GameStepResult.SuccessResult(events);
    }

    private GameStepResult ApplyEndTurn(GameAction action)
    {
        List<GameEvent> events = new List<GameEvent>();

        PlayerSide endingSide = State.ActiveSide;

        events.Add(GameEvent.Create(
            State,
            GameEventType.TurnEnded,
            endingSide,
            $"{endingSide} ended turn."
        ));

        PlayerSide nextSide = State.GetOpponentSide(endingSide);

        State.ActiveSide = nextSide;
        State.TurnNumber++;
        State.ActionIndex++;

        StartTurn(nextSide, events);

        return GameStepResult.SuccessResult(events);
    }

    private GameStepResult ApplyPlayCard(GameAction action)
    {
        List<GameEvent> events = new List<GameEvent>();

        PlayerState actingPlayer = State.GetPlayerState(action.ActorSide);

        if (actingPlayer == null)
            return GameStepResult.Failure("Acting player state is null.");

        CardInstance sourceCard = FindCardInZone(
            actingPlayer.Hand,
            action.SourceInstanceId
        );

        if (sourceCard == null)
            return GameStepResult.Failure($"Card {action.SourceInstanceId} was not found in hand.");

        if (sourceCard.ManaCost > actingPlayer.Mana)
            return GameStepResult.Failure($"{sourceCard.Title} cannot be played: not enough mana.");

        if (sourceCard.IsEntity && actingPlayer.Field.Count >= GameRules.MaxCardsOnField)
            return GameStepResult.Failure($"{sourceCard.Title} cannot be played: field is full.");

        PlayTargetRequirement targetRequirement =
            PlayTargetRequirementResolver.GetRequirement(sourceCard);

        if (targetRequirement != PlayTargetRequirement.None)
        {
            bool hasAnyValidTarget =
                PlayTargetValidator.HasAnyValidPlayTarget(State, sourceCard);

            if (!hasAnyValidTarget && sourceCard.IsEntity && action.TargetType == PlayTargetType.None)
            {
                // Allow play without target.
            }
            else if (!PlayTargetValidator.IsValidPlayTarget(State, sourceCard, action))
            {
                return GameStepResult.Failure(
                    $"{sourceCard.Title} has invalid play target."
                );
            }
        }

        if (action.TargetType == PlayTargetType.Hero && !action.TargetHeroSide.HasValue)
        {
            return GameStepResult.Failure(
                $"{sourceCard.Title} targets a hero, but TargetHeroSide is missing."
            );
        }

        actingPlayer.Mana -= sourceCard.ManaCost;

        events.Add(GameEvent.Create(
            State,
            GameEventType.ManaSpent,
            action.ActorSide,
            $"{action.ActorSide} spent {sourceCard.ManaCost} mana.",
            sourceCard.ManaCost,
            sourceInstanceId: sourceCard.InstanceId
        ));

        actingPlayer.Hand.Remove(sourceCard);

        events.Add(GameEvent.Create(
            State,
            GameEventType.CardPlayed,
            action.ActorSide,
            $"{action.ActorSide} played {sourceCard.Title}.",
            sourceInstanceId: sourceCard.InstanceId,
            targetInstanceId: action.TargetInstanceId,
            targetHeroSide: action.TargetHeroSide
        ));

        GameTarget selectedTarget = CreateTargetFromAction(action);

        if (sourceCard.IsEntity)
        {
            MoveEntityToField(sourceCard, actingPlayer, events, action.FieldIndex);

            EffectEngine.ResolveEffects(
                State,
                sourceCard,
                EffectTrigger.OnPlay,
                selectedTarget,
                events
            );
        }
        else
        {
            EffectEngine.ResolveEffects(
                State,
                sourceCard,
                EffectTrigger.OnPlay,
                selectedTarget,
                events
            );

            MoveSpellToGraveyard(sourceCard, actingPlayer, events);
        }

        State.ActionIndex++;

        return GameStepResult.SuccessResult(events);
    }

    private GameTarget CreateTargetFromAction(GameAction action)
    {
        if (action == null)
            return GameTarget.None();

        if (action.TargetType == PlayTargetType.Card &&
            action.TargetInstanceId.HasValue)
        {
            return GameTarget.Card(action.TargetInstanceId.Value);
        }

        if (action.TargetType == PlayTargetType.Hero &&
            action.TargetHeroSide.HasValue)
        {
            return GameTarget.Hero(action.TargetHeroSide.Value);
        }

        return GameTarget.None();
    }

    private void MoveEntityToField(
        CardInstance card,
        PlayerState owner,
        List<GameEvent> events,
        int? fieldIndex = null)
    {
        CardZoneService.MoveEntityFromHandToField(
            State,
            owner,
            card,
            events,
            fieldIndex
        );
    }

    private void MoveSpellToGraveyard(
        CardInstance card,
        PlayerState owner,
        List<GameEvent> events)
    {
        CardZoneService.MoveToGraveyard(
             State,
             owner,
             card,
             events,
             GameEventType.CardMovedToGraveyard,
             $"{card.Title} moved to graveyard."
         );
    }

    private CardInstance FindCardInZone(
        List<CardInstance> cards,
        int instanceId)
    {
        if (cards == null)
            return null;

        foreach (CardInstance card in cards)
        {
            if (card != null && card.InstanceId == instanceId)
                return card;
        }

        return null;
    }

    private void StartTurn(PlayerSide side, List<GameEvent> events)
    {
        PlayerState player = State.GetPlayerState(side);

        if (player == null)
            return;

        // Do not increase mana before the player's first personal turn.
        // The initial state already gives both players StartingManaPool.
        if (player.TurnsStarted > 0)
        {
            int oldManaPool = player.ManaPool;

            player.ManaPool = ClampManaPool(
                player.ManaPool + GameRules.ManaIncreasePerTurn
            );

            if (player.ManaPool != oldManaPool)
            {
                events.Add(GameEvent.Create(
                    State,
                    GameEventType.ManaPoolChanged,
                    side,
                    $"{side} mana pool changed from {oldManaPool} to {player.ManaPool}.",
                    player.ManaPool
                ));
            }
        }

        player.TurnsStarted++;

        RestoreMana(player, events);
        RefreshFieldAttackState(player);
        DrawCard(player, events);

        events.Add(GameEvent.Create(
            State,
            GameEventType.TurnStarted,
            side,
            $"{side} started turn."
        ));
    }

    private int ClampManaPool(int manaPool)
    {
        if (manaPool < 0)
            return 0;

        if (manaPool > GameRules.MaxManaPool)
            return GameRules.MaxManaPool;

        return manaPool;
    }

    private void RestoreMana(PlayerState player, List<GameEvent> events)
    {
        if (player == null)
            return;

        int oldMana = player.Mana;

        int restoredMana = player.ManaPool + player.PendingTemporaryMana;

        if (restoredMana > GameRules.MaxManaPool)
            restoredMana = GameRules.MaxManaPool;

        player.Mana = restoredMana;
        player.PendingTemporaryMana = 0;

        events.Add(GameEvent.Create(
            State,
            GameEventType.ManaRestored,
            player.Side,
            $"{player.Side} mana restored from {oldMana} to {player.Mana}.",
            player.Mana
        ));
    }

    private void RefreshFieldAttackState(PlayerState player)
    {
        if (player == null || player.Field == null)
            return;

        foreach (CardInstance card in player.Field)
        {
            if (card == null || !card.IsEntity)
                continue;

            RefreshAttackCapacity(card);
            card.CanAttackOnlyCardsThisTurn = false;
        }
    }

    private void DrawCard(PlayerState player, List<GameEvent> events)
    {
        CardZoneService.DrawCard(
            State,
            player,
            null,
            events
        );
    }

    private string ValidateAttackerCanAttack(CardInstance attacker)
    {
        if (attacker == null)
            return "Attacker is null.";

        if (!attacker.IsEntity)
            return $"{attacker.Title} is not an entity.";

        if (attacker.Zone != GameZone.Field || !attacker.IsPlaced)
            return $"{attacker.Title} is not placed on field.";

        if (!attacker.CanAttack || attacker.RemainingAttacksThisTurn <= 0)
            return $"{attacker.Title} cannot attack.";

        if (attacker.Attack <= 0)
            return $"{attacker.Title} has 0 attack.";

        return null;
    }

    private bool CanAttackTargetCard(
        PlayerState opponentPlayer,
        CardInstance defender)
    {
        if (opponentPlayer == null || defender == null)
            return false;

        bool opponentHasProvocation = HasProvocation(opponentPlayer);

        if (!opponentHasProvocation)
            return true;

        return KeywordService.HasKeyword(defender, KeywordType.Provocation);
    }

    private bool HasProvocation(PlayerState player)
    {
        if (player == null || player.Field == null)
            return false;

        foreach (CardInstance card in player.Field)
        {
            if (card == null)
                continue;

            if (card.Zone != GameZone.Field)
                continue;

            if (KeywordService.HasKeyword(card, KeywordType.Provocation))
                return true;
        }

        return false;
    }

    private void RefreshAttackCapacity(CardInstance card)
    {
        if (card == null || !card.IsEntity)
            return;

        card.RemainingAttacksThisTurn = KeywordService.GetMaxAttacksPerTurn(card);
        card.CanAttack = card.RemainingAttacksThisTurn > 0;
    }

    private void ConsumeAttack(CardInstance card)
    {
        if (card == null)
            return;

        if (card.RemainingAttacksThisTurn > 0)
            card.RemainingAttacksThisTurn--;

        card.CanAttack = card.RemainingAttacksThisTurn > 0;

        if (!card.CanAttack)
            card.CanAttackOnlyCardsThisTurn = false;
    }
}
