public enum GameEndReason
{
    HeroLethal,         // a hero reached 0 HP from card/attack damage
    Fatigue,            // empty-deck draw damage (now active, Phase 1.1a)
    MaxActionsReached,  // playout hit the action cap / stalled; no winner
    Draw                // IsGameOver with no Winner
}
