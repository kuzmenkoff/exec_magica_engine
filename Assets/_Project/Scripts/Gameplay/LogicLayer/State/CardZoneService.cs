using System.Collections.Generic;

/// <summary>
/// Centralized pure service for moving cards between zones.
/// This class must not depend on Unity objects.
/// </summary>
public static class CardZoneService
{
    public static CardInstance DrawCard(
        GameState state,
        PlayerState player,
        CardInstance source,
        List<GameEvent> events)
    {
        if (state == null || player == null)
            return null;

        if (player.Deck == null || player.Deck.Count == 0)
        {
            player.FatigueCounter++;

            int oldHp = player.HP;
            player.HP -= player.FatigueCounter;
            if (player.HP < 0)
                player.HP = 0;
            int fatigueDamage = oldHp - player.HP;

            events?.Add(GameEvent.Create(
                state,
                GameEventType.FatigueDamage,
                player.Side,
                $"{player.Side} takes {fatigueDamage} fatigue damage (deck empty).",
                fatigueDamage,
                targetHeroSide: player.Side
            ));

            if (player.HP <= 0)
            {
                state.IsGameOver = true;
                state.Winner = state.GetOpponentSide(player.Side);

                events?.Add(GameEvent.Create(
                    state,
                    GameEventType.GameEnded,
                    player.Side,
                    $"{state.Winner.Value} wins.",
                    targetHeroSide: player.Side
                ));
            }

            return null;
        }

        if (player.Hand == null)
            player.Hand = new List<CardInstance>();

        if (player.Hand != null && player.Hand.Count >= GameRules.MaxCardsOnHand)
        {
            events?.Add(GameEvent.Create(
                state,
                GameEventType.CardDrawSkippedHandFull,
                player.Side,
                source != null
                    ? $"{player.Side} cannot draw from {source.Title} because hand is full."
                    : $"{player.Side} cannot draw because hand is full.",
                sourceInstanceId: source?.InstanceId
            ));

            return null;
        }

        CardInstance drawnCard = player.Deck[0];
        player.Deck.RemoveAt(0);

        drawnCard.Zone = GameZone.Hand;
        drawnCard.IsPlaced = false;
        drawnCard.CanAttack = false;
        drawnCard.CanAttackOnlyCardsThisTurn = false;
        drawnCard.RemainingAttacksThisTurn = 0;

        player.Hand.Add(drawnCard);

        events?.Add(GameEvent.Create(
            state,
            GameEventType.CardDrawn,
            player.Side,
            source != null
                ? $"{player.Side} drew {drawnCard.Title} from {source.Title}."
                : $"{player.Side} drew {drawnCard.Title}.",
            sourceInstanceId: source?.InstanceId,
            targetInstanceId: drawnCard.InstanceId
        ));

        return drawnCard;
    }

    public static bool MoveToGraveyard(
        GameState state,
        PlayerState owner,
        CardInstance card,
        List<GameEvent> events,
        GameEventType eventType,
        string message = null)
    {
        if (state == null || owner == null || card == null)
            return false;

        RemoveFromCurrentZone(owner, card);

        card.Zone = GameZone.Graveyard;
        card.IsPlaced = false;
        card.CanAttack = false;
        card.CanAttackOnlyCardsThisTurn = false;
        card.RemainingAttacksThisTurn = 0;

        if (owner.Graveyard == null)
            owner.Graveyard = new List<CardInstance>();

        if (!owner.Graveyard.Contains(card))
            owner.Graveyard.Add(card);

        events?.Add(GameEvent.Create(
            state,
            eventType,
            owner.Side,
            message ?? $"{card.Title} moved to graveyard.",
            sourceInstanceId: card.InstanceId
        ));

        return true;
    }

    public static bool MoveEntityFromHandToField(
        GameState state,
        PlayerState owner,
        CardInstance card,
        List<GameEvent> events,
        int? fieldIndex = null)
    {
        if (state == null || owner == null || card == null)
            return false;

        RemoveFromCurrentZone(owner, card);

        card.Zone = GameZone.Field;
        card.IsPlaced = true;

        ApplyPlayedEntityAttackRules(card);

        if (owner.Field == null)
            owner.Field = new List<CardInstance>();

        if (!owner.Field.Contains(card))
        {
            if (fieldIndex.HasValue)
            {
                int index = fieldIndex.Value;

                if (index < 0)
                    index = 0;

                if (index > owner.Field.Count)
                    index = owner.Field.Count;

                owner.Field.Insert(index, card);
            }
            else
            {
                owner.Field.Add(card);
            }
        }

        card.LastKnownFieldIndex = owner.Field.IndexOf(card);

        events?.Add(GameEvent.Create(
            state,
            GameEventType.CardMovedToField,
            owner.Side,
            $"{card.Title} moved to field.",
            sourceInstanceId: card.InstanceId
        ));

        return true;
    }

    public static bool SummonToField(
        GameState state,
        PlayerState owner,
        CardInstance card,
        CardInstance source,
        List<GameEvent> events,
        int? fieldIndex = null)
    {
        if (state == null || owner == null || card == null)
            return false;

        if (owner.Field == null)
            owner.Field = new List<CardInstance>();

        if (owner.Field.Count >= GameRules.MaxCardsOnField)
        {
            events?.Add(GameEvent.Create(
                state,
                GameEventType.SummonSkippedFieldFull,
                source != null ? source.OwnerSide : owner.Side,
                source != null
                    ? $"{source.Title} could not summon {card.Title}: field is full."
                    : $"{owner.Side} could not summon {card.Title}: field is full.",
                sourceInstanceId: source?.InstanceId,
                targetInstanceId: card.InstanceId
            ));

            return false;
        }

        RemoveFromCurrentZone(owner, card);

        card.Zone = GameZone.Field;
        card.IsPlaced = true;

        ApplySummonedEntityAttackRules(card);

        int insertIndex = owner.Field.Count;

        if (fieldIndex.HasValue)
        {
            insertIndex = fieldIndex.Value;

            if (insertIndex < 0)
                insertIndex = 0;

            if (insertIndex > owner.Field.Count)
                insertIndex = owner.Field.Count;

            owner.Field.Insert(insertIndex, card);
        }
        else
        {
            owner.Field.Add(card);
        }

        card.LastKnownFieldIndex = insertIndex;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.CardSummoned,
            source != null ? source.OwnerSide : owner.Side,
            source != null
                ? $"{source.Title} summoned {card.Title}."
                : $"{owner.Side} summoned {card.Title}.",
            sourceInstanceId: source?.InstanceId,
            targetInstanceId: card.InstanceId,
            fieldIndex: insertIndex
        ));

        return true;
    }

    private static void ApplyPlayedEntityAttackRules(CardInstance card)
    {
        if (card == null)
            return;

        if (KeywordService.HasKeyword(card, KeywordType.Charge))
        {
            card.RemainingAttacksThisTurn = KeywordService.GetMaxAttacksPerTurn(card);
            card.CanAttack = card.RemainingAttacksThisTurn > 0;
            card.CanAttackOnlyCardsThisTurn = false;
            return;
        }

        if (KeywordService.HasKeyword(card, KeywordType.Rush))
        {
            card.RemainingAttacksThisTurn = KeywordService.GetMaxAttacksPerTurn(card);
            card.CanAttack = card.RemainingAttacksThisTurn > 0;
            card.CanAttackOnlyCardsThisTurn = true;
            return;
        }

        card.RemainingAttacksThisTurn = 0;
        card.CanAttack = false;
        card.CanAttackOnlyCardsThisTurn = false;
    }

    private static void ApplySummonedEntityAttackRules(CardInstance card)
    {
        if (card == null)
            return;

        card.RemainingAttacksThisTurn = 0;
        card.CanAttack = false;
        card.CanAttackOnlyCardsThisTurn = false;
    }

    private static void RemoveFromCurrentZone(
        PlayerState owner,
        CardInstance card)
    {
        if (owner == null || card == null)
            return;

        owner.Deck?.Remove(card);
        owner.Hand?.Remove(card);
        owner.Field?.Remove(card);
        owner.Graveyard?.Remove(card);
    }


}
