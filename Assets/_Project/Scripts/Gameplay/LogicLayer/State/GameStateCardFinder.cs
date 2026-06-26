using System.Collections.Generic;

/// <summary>
/// Finds card instances inside pure GameState.
/// </summary>
public static class GameStateCardFinder
{
    public static CardInstance FindCard(GameState state, int instanceId)
    {
        if (state == null)
            return null;

        CardInstance card;

        card = FindCardInPlayer(state.Player, instanceId);
        if (card != null)
            return card;

        card = FindCardInPlayer(state.Enemy, instanceId);
        if (card != null)
            return card;

        return null;
    }

    public static CardInstance FindCardInPlayer(PlayerState player, int instanceId)
    {
        if (player == null)
            return null;

        CardInstance card;

        card = FindCardInList(player.Deck, instanceId);
        if (card != null)
            return card;

        card = FindCardInList(player.Hand, instanceId);
        if (card != null)
            return card;

        card = FindCardInList(player.Field, instanceId);
        if (card != null)
            return card;

        card = FindCardInList(player.Graveyard, instanceId);
        if (card != null)
            return card;

        return null;
    }

    public static CardInstance FindCardInList(
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
}
