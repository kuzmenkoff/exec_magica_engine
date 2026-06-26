using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Validates pure GameState integrity.
/// This is used for debugging, simulations and future tests.
/// </summary>
public static class GameStateValidator
{
    public static List<string> Validate(GameState state)
    {
        List<string> errors = new List<string>();

        if (state == null)
        {
            errors.Add("GameState is null.");
            return errors;
        }

        if (state.Player == null)
            errors.Add("Player state is null.");

        if (state.Enemy == null)
            errors.Add("Enemy state is null.");

        if (state.Player == null || state.Enemy == null)
            return errors;

        ValidatePlayerState(state.Player, PlayerSide.Player, errors);
        ValidatePlayerState(state.Enemy, PlayerSide.Enemy, errors);
        ValidateUniqueInstanceIds(state, errors);

        return errors;
    }

    public static bool IsValid(GameState state)
    {
        return Validate(state).Count == 0;
    }

    private static void ValidatePlayerState(
        PlayerState player,
        PlayerSide expectedSide,
        List<string> errors)
    {
        if (player.Side != expectedSide)
        {
            errors.Add($"PlayerState side mismatch. Expected {expectedSide}, got {player.Side}.");
        }

        if (player.HP < 0)
            errors.Add($"{expectedSide} HP is negative: {player.HP}.");

        if (player.MaxHP <= 0)
            errors.Add($"{expectedSide} MaxHP must be positive.");

        if (player.Mana < 0)
            errors.Add($"{expectedSide} Mana is negative: {player.Mana}.");

        if (player.ManaPool < 0)
            errors.Add($"{expectedSide} ManaPool is negative: {player.ManaPool}.");

        if (player.ManaPool > GameRules.MaxManaPool)
            errors.Add($"{expectedSide} ManaPool exceeds max: {player.ManaPool}.");

        ValidateZone(player.Deck, expectedSide, GameZone.Deck, errors);
        ValidateZone(player.Hand, expectedSide, GameZone.Hand, errors);
        ValidateZone(player.Field, expectedSide, GameZone.Field, errors);
        ValidateZone(player.Graveyard, expectedSide, GameZone.Graveyard, errors);

        if (player.Hand != null && player.Hand.Count > GameRules.MaxCardsOnHand)
            errors.Add($"{expectedSide} hand size exceeds max: {player.Hand.Count}.");

        if (player.Field != null && player.Field.Count > GameRules.MaxCardsOnField)
            errors.Add($"{expectedSide} field size exceeds max: {player.Field.Count}.");
    }

    private static void ValidateZone(
        List<CardInstance> cards,
        PlayerSide expectedOwner,
        GameZone expectedZone,
        List<string> errors)
    {
        if (cards == null)
            return;

        foreach (CardInstance card in cards)
        {
            if (card == null)
            {
                errors.Add($"{expectedOwner} {expectedZone} contains null card.");
                continue;
            }

            if (card.InstanceId <= 0)
                errors.Add($"Card {card.Title} has invalid InstanceId: {card.InstanceId}.");

            if (card.CardId <= 0)
                errors.Add($"Card instance {card.InstanceId} has invalid CardId: {card.CardId}.");

            if (card.OwnerSide != expectedOwner)
            {
                errors.Add(
                    $"Card {card.Title} [{card.InstanceId}] owner mismatch. " +
                    $"Expected {expectedOwner}, got {card.OwnerSide}."
                );
            }

            if (card.Zone != expectedZone)
            {
                errors.Add(
                    $"Card {card.Title} [{card.InstanceId}] zone mismatch. " +
                    $"Expected {expectedZone}, got {card.Zone}."
                );
            }

            if (card.Zone == GameZone.Field && !card.IsPlaced)
            {
                errors.Add($"Card {card.Title} [{card.InstanceId}] is on field but IsPlaced is false.");
            }

            if (card.Zone != GameZone.Field && card.IsPlaced)
            {
                errors.Add($"Card {card.Title} [{card.InstanceId}] is not on field but IsPlaced is true.");
            }

            if (card.IsEntity && card.MaxHP <= 0)
            {
                errors.Add($"Entity {card.Title} [{card.InstanceId}] has invalid MaxHP.");
            }
        }
    }

    private static void ValidateUniqueInstanceIds(
        GameState state,
        List<string> errors)
    {
        List<CardInstance> allCards = new List<CardInstance>();

        AddCards(allCards, state.Player);
        AddCards(allCards, state.Enemy);

        var duplicateGroups = allCards
            .Where(card => card != null)
            .GroupBy(card => card.InstanceId)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            errors.Add($"Duplicate InstanceId detected: {group.Key}.");
        }
    }

    private static void AddCards(List<CardInstance> result, PlayerState player)
    {
        if (player == null)
            return;

        if (player.Deck != null)
            result.AddRange(player.Deck);

        if (player.Hand != null)
            result.AddRange(player.Hand);

        if (player.Field != null)
            result.AddRange(player.Field);

        if (player.Graveyard != null)
            result.AddRange(player.Graveyard);
    }
}
