/// <summary>
/// Identifies which side of the match performs an action or owns a card.
/// </summary>
public enum PlayerSide
{
    /// <summary>The first controller — local human in PvAI, side A in AI-vs-AI.</summary>
    Player,

    /// <summary>The opposing controller — the AI in PvAI, side B in AI-vs-AI.</summary>
    Enemy
}
