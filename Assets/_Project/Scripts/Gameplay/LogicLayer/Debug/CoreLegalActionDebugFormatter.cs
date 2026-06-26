using System.Collections.Generic;
using System.Text;

/// <summary>
/// Formats pure GameState legal actions for debugging.
/// </summary>
public static class CoreLegalActionDebugFormatter
{
    public static string Format(GameState state, List<GameAction> actions)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("========== LEGAL ACTIONS ==========");

        if (actions == null || actions.Count == 0)
        {
            sb.AppendLine("No legal actions.");
            sb.AppendLine("===================================");
            return sb.ToString();
        }

        for (int i = 0; i < actions.Count; i++)
        {
            sb.AppendLine($"{i}. {FormatAction(state, actions[i])}");
        }

        sb.AppendLine("===================================");

        return sb.ToString();
    }

    private static string FormatAction(GameState state, GameAction action)
    {
        if (action == null)
            return "[null action]";

        string sourceText = FormatSource(state, action);

        string targetText = FormatTarget(state, action);

        if (action.TargetType == PlayTargetType.None)
            return $"{action.ActorSide} | {action.Type}: {sourceText}";

        return $"{action.ActorSide} | {action.Type}: {sourceText} -> {targetText}";
    }

    private static string FormatSource(GameState state, GameAction action)
    {
        if (action.SourceInstanceId == 0)
            return "none";

        CardInstance source = FindCard(state, action.SourceInstanceId);

        if (source == null)
            return $"missing-source[{action.SourceInstanceId}]";

        return $"{source.Title} [{source.InstanceId}]";
    }

    private static string FormatTarget(GameState state, GameAction action)
    {
        if (action.TargetType == PlayTargetType.Hero)
        {
            if (action.TargetHeroSide.HasValue)
                return $"{action.TargetHeroSide.Value} Hero";

            return "Hero";
        }

        if (action.TargetType == PlayTargetType.Card && action.TargetInstanceId.HasValue)
        {
            CardInstance target = FindCard(state, action.TargetInstanceId.Value);

            if (target == null)
                return $"missing-target[{action.TargetInstanceId.Value}]";

            return $"{target.Title} [{target.InstanceId}]";
        }

        return "none";
    }

    private static CardInstance FindCard(GameState state, int instanceId)
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

    private static CardInstance FindCardInPlayer(PlayerState player, int instanceId)
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

    private static CardInstance FindCardInList(
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
