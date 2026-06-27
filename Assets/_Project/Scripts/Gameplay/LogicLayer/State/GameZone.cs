/// <summary>The zone a card currently occupies.</summary>
public enum GameZone
{
    /// <summary>Not in any zone.</summary>
    None,
    /// <summary>In the owner's deck (draw pile).</summary>
    Deck,
    /// <summary>In the owner's hand.</summary>
    Hand,
    /// <summary>On the battlefield.</summary>
    Field,
    /// <summary>In the graveyard (discard pile).</summary>
    Graveyard
}