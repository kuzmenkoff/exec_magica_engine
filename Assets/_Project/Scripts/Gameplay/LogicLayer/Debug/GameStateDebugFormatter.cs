using System.Text;

/// <summary>
/// Creates readable debug snapshots of pure GameState.
/// Useful before connecting GameEngine to Unity and AI simulations.
/// </summary>
public static class GameStateDebugFormatter
{
    public static string Format(GameState state)
    {
        if (state == null)
            return "[GameState] null";

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("========== GAME STATE ==========");
        sb.AppendLine($"Turn: {state.TurnNumber}");
        sb.AppendLine($"ActionIndex: {state.ActionIndex}");
        sb.AppendLine($"ActiveSide: {state.ActiveSide}");
        sb.AppendLine($"IsGameOver: {state.IsGameOver}");
        sb.AppendLine($"Winner: {(state.Winner.HasValue ? state.Winner.Value.ToString() : "None")}");
        sb.AppendLine($"NextInstanceId: {state.NextInstanceId}");
        sb.AppendLine();

        AppendPlayer(sb, state.Player);
        sb.AppendLine();
        AppendPlayer(sb, state.Enemy);

        sb.AppendLine("================================");

        return sb.ToString();
    }

    private static void AppendPlayer(StringBuilder sb, PlayerState player)
    {
        if (player == null)
        {
            sb.AppendLine("[PlayerState] null");
            return;
        }

        sb.AppendLine($"--- {player.Side} ---");
        sb.AppendLine($"HP: {player.HP}/{player.MaxHP}");
        sb.AppendLine($"Mana: {player.Mana}/{player.ManaPool}");
        sb.AppendLine($"PendingTemporaryMana: {player.PendingTemporaryMana}");
        sb.AppendLine($"Deck: {player.Deck?.Count ?? 0}");
        sb.AppendLine($"Hand: {player.Hand?.Count ?? 0}");
        sb.AppendLine($"Field: {player.Field?.Count ?? 0}");
        sb.AppendLine($"Graveyard: {player.Graveyard?.Count ?? 0}");

        AppendZone(sb, "Hand", player.Hand);
        AppendZone(sb, "Field", player.Field);
    }

    private static void AppendZone(
        StringBuilder sb,
        string zoneName,
        System.Collections.Generic.List<CardInstance> cards)
    {
        sb.AppendLine($"{zoneName}:");

        if (cards == null || cards.Count == 0)
        {
            sb.AppendLine("  - empty");
            return;
        }

        foreach (CardInstance card in cards)
        {
            if (card == null)
            {
                sb.AppendLine("  - null");
                continue;
            }

            string keywords = card.Keywords != null && card.Keywords.Count > 0
             ? string.Join(", ", card.Keywords)
             : "None";

            int effectsCount = card.Effects != null
                ? card.Effects.Count
                : 0;

            sb.AppendLine(
                $"  - {card.Title} " +
                $"[InstanceId: {card.InstanceId}, CardId: {card.CardId}, " +
                $"Zone: {card.Zone}, ATK: {card.Attack}, HP: {card.HP}/{card.MaxHP}, " +
                $"Mana: {card.ManaCost}, Keywords: {keywords}, Effects: {effectsCount}, " +
                $"CanAttack: {card.CanAttack}, OnlyCards: {card.CanAttackOnlyCardsThisTurn}, " +
                $"RemainingAttacks: {card.RemainingAttacksThisTurn}]"
            );
        }
    }
}
