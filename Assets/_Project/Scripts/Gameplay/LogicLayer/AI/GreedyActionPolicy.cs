using System.Collections.Generic;

/// <summary>
/// Heuristic baseline policy: one-ply greedy.
/// For each legal action it applies the action to a deep copy of the state via the real
/// GameEngine, scores the resulting state from the acting side's perspective with a linear
/// evaluation, and picks the highest-scoring action.
///
/// Information model (Phase 2.0): the evaluation reads only PUBLIC quantities (hero HP,
/// board stats, hand counts), never opponent hand identities, so it respects the
/// imperfect-information setting without determinization.
/// </summary>
public class GreedyActionPolicy : IGameActionPolicy
{
    private const double WinScore = 1_000_000.0;
    private readonly double heroHpWeight;
    private readonly double attackWeight;
    private readonly double hpWeight;
    private readonly double minionCountWeight;
    private readonly double handCountWeight;
    private readonly System.Random tieBreaker;

    /// <summary>
    /// Creates a one-ply greedy policy. <paramref name="seed"/>: 0 = non-deterministic tie-break,
    /// non-zero = reproducible. Weights tune the linear evaluation (hero HP, board attack/HP,
    /// minion count and hand-size differences).
    /// </summary>
    public GreedyActionPolicy(
        int seed = 0,
        double heroHpWeight = 1.0,
        double attackWeight = 2.0,
        double hpWeight = 1.0,
        double minionCountWeight = 1.0,
        double handCountWeight = 1.0)
    {
        this.heroHpWeight = heroHpWeight;
        this.attackWeight = attackWeight;
        this.hpWeight = hpWeight;
        this.minionCountWeight = minionCountWeight;
        this.handCountWeight = handCountWeight;
        tieBreaker = seed == 0 ? new System.Random() : new System.Random(seed);
    }

    /// <summary>
    /// Evaluates every legal action one ply deep (applies it on a deep copy via the real engine,
    /// scores the result from the actor's perspective) and returns the best, with a uniform
    /// reservoir tie-break.
    /// </summary>
    public GameAction ChooseAction(
        GameState state,
        List<GameAction> legalActions,
        PlayerSide actorSide)
    {
        if (state == null || legalActions == null || legalActions.Count == 0)
            return null;

        GameAction best = null;
        double bestScore = double.NegativeInfinity;
        int tiesSeen = 0;

        foreach (GameAction action in legalActions)
        {
            if (action == null || action.ActorSide != actorSide)
                continue;

            GameState copy = state.GetDeepCopy();
            GameEngine engine = new GameEngine(copy);

            GameStepResult result = engine.ApplyAction(action);
            if (!result.Success)
                continue;

            double score = Evaluate(engine.State, actorSide);

            if (score > bestScore)
            {
                bestScore = score;
                best = action;
                tiesSeen = 1;
            }
            else if (score == bestScore)
            {
                // Uniform reservoir tie-break: deterministic for a fixed seed.
                tiesSeen++;
                if (tieBreaker.Next(tiesSeen) == 0)
                    best = action;
            }
        }

        return best;
    }

    private double Evaluate(GameState state, PlayerSide side)
    {
        if (state.IsGameOver)
        {
            if (!state.Winner.HasValue)
                return 0.0; // draw
            return state.Winner.Value == side ? WinScore : -WinScore;
        }

        PlayerState me = state.GetPlayerState(side);
        PlayerState opp = state.GetOpponentState(side);

        BoardSummary myb = SummarizeBoard(me);
        BoardSummary opb = SummarizeBoard(opp);

        double score = 0.0;
        score += heroHpWeight * (me.HP - opp.HP);
        score += attackWeight * (myb.Attack - opb.Attack);
        score += hpWeight * (myb.Hp - opb.Hp);
        score += minionCountWeight * (myb.Count - opb.Count);
        score += handCountWeight * (me.Hand.Count - opp.Hand.Count);
        return score;
    }

    private static BoardSummary SummarizeBoard(PlayerState player)
    {
        BoardSummary s = default;
        if (player?.Field == null)
            return s;

        foreach (CardInstance card in player.Field)
        {
            if (card == null || !card.IsEntity)
                continue;

            s.Attack += card.Attack;
            s.Hp += card.HP;
            s.Count += 1;
        }
        return s;
    }

    private struct BoardSummary
    {
        public int Attack;
        public int Hp;
        public int Count;
    }
}
