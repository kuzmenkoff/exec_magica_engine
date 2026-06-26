using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public static class SessionRunner
{
    public static SessionRecord RunGame(
        GameState initialState,
        IGameActionPolicy playerPolicy,
        IGameActionPolicy enemyPolicy,
        SessionRecord meta,
        int maxActions = 400,
        bool logEvents = true)
    {
        GameEngine engine = new GameEngine(initialState);

        List<GameEvent> events = new List<GameEvent>();
        Dictionary<PlayerSide, List<double>> thinkMs = new Dictionary<PlayerSide, List<double>>
        {
            { PlayerSide.Player, new List<double>() },
            { PlayerSide.Enemy,  new List<double>() }
        };

        bool reachedCap = true;
        Stopwatch total = Stopwatch.StartNew();

        for (int i = 0; i < maxActions; i++)
        {
            if (engine.State.IsGameOver) { reachedCap = false; break; }

            PlayerSide side = engine.State.ActiveSide;
            List<GameAction> legal = engine.GetLegalActions(side);
            if (legal == null || legal.Count == 0) { reachedCap = false; break; }

            IGameActionPolicy policy = side == PlayerSide.Player ? playerPolicy : enemyPolicy;

            Stopwatch decision = Stopwatch.StartNew();
            GameAction action = policy.ChooseAction(engine.State, legal, side);
            decision.Stop();
            thinkMs[side].Add(decision.Elapsed.TotalMilliseconds);

            if (action == null) { reachedCap = false; break; }

            GameStepResult result = engine.ApplyAction(action);
            if (result.Events != null) events.AddRange(result.Events);
            if (!result.Success) { reachedCap = false; break; }
        }
        total.Stop();

        GameState s = engine.State;

        meta.Outcome = new OutcomeRecord
        {
            Winner = s.Winner?.ToString(),
            Reason = ClassifyEndReason(s, events).ToString(),
            TotalTurns = s.TurnNumber,
            TotalActions = s.ActionIndex,
            DurationMs = total.Elapsed.TotalMilliseconds
        };

        meta.PerSideMetrics = new Dictionary<string, PerSideMetrics>
        {
            { "Player", BuildSideMetrics(events, PlayerSide.Player, thinkMs[PlayerSide.Player]) },
            { "Enemy",  BuildSideMetrics(events, PlayerSide.Enemy,  thinkMs[PlayerSide.Enemy]) }
        };

        meta.CardImpact = ComputeCardImpact(s, events);

        meta.Events = logEvents ? events : null;
        return meta;
    }

    private static GameEndReason ClassifyEndReason(GameState s, List<GameEvent> events)
    {
        if (!s.IsGameOver)
            return GameEndReason.MaxActionsReached;
        if (!s.Winner.HasValue)
            return GameEndReason.Draw;

        PlayerSide loser = s.GetOpponentSide(s.Winner.Value);
        for (int k = events.Count - 1; k >= 0; k--)
        {
            GameEvent e = events[k];
            if (e.TargetHeroSide == loser &&
                (e.Type == GameEventType.FatigueDamage || e.Type == GameEventType.HeroDamaged))
                return e.Type == GameEventType.FatigueDamage ? GameEndReason.Fatigue : GameEndReason.HeroLethal;
        }
        return GameEndReason.HeroLethal;
    }

    private static PerSideMetrics BuildSideMetrics(List<GameEvent> events, PlayerSide side, List<double> thinkTimes)
    {
        (double mean, double median) = ThinkStats(thinkTimes);
        return new PerSideMetrics
        {
            AvgManaEfficiency = AvgManaEfficiency(events, side),
            MeanThinkMs = mean,
            MedianThinkMs = median
        };
    }

    private static double AvgManaEfficiency(List<GameEvent> events, PlayerSide side)
    {
        double sum = 0; int turns = 0;
        int available = 0, spent = 0; bool open = false;

        foreach (GameEvent e in events)
        {
            if (e.ActorSide != side) continue;

            if (e.Type == GameEventType.ManaRestored)
            {
                if (open && available > 0) { sum += Math.Min(1.0, (double)spent / available); turns++; }
                available = e.Value; spent = 0; open = true;
            }
            else if (e.Type == GameEventType.ManaSpent && open)
            {
                spent += e.Value;
            }
        }
        if (open && available > 0) { sum += Math.Min(1.0, (double)spent / available); turns++; }
        return turns > 0 ? sum / turns : 0.0;
    }

    private static (double mean, double median) ThinkStats(List<double> t)
    {
        if (t == null || t.Count == 0) return (0.0, 0.0);
        double mean = t.Average();
        List<double> sorted = t.OrderBy(x => x).ToList();
        int n = sorted.Count;
        double median = (n % 2 == 1) ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
        return (mean, median);
    }

    // Attributes damage/heal/buff back to the source card's CardId.
    // Fatigue has no source -> naturally excluded.
    private static Dictionary<int, double> ComputeCardImpact(GameState s, List<GameEvent> events)
    {
        Dictionary<int, int> instanceToCard = new Dictionary<int, int>();
        IndexZones(instanceToCard, s.Player);
        IndexZones(instanceToCard, s.Enemy);

        Dictionary<int, double> impact = new Dictionary<int, double>();
        foreach (GameEvent e in events)
        {
            if (!e.SourceInstanceId.HasValue) continue;
            if (!instanceToCard.TryGetValue(e.SourceInstanceId.Value, out int cardId)) continue;

            double v;
            switch (e.Type)
            {
                case GameEventType.HeroDamaged:
                case GameEventType.DamageDealt:
                case GameEventType.HeroHealed:
                case GameEventType.CardHealed:
                case GameEventType.CardStatsBuffed:
                    v = e.Value;
                    break;
                default:
                    continue;
            }
            if (v == 0) continue;

            impact.TryGetValue(cardId, out double cur);
            impact[cardId] = cur + v;
        }
        return impact;
    }

    private static void IndexZones(Dictionary<int, int> map, PlayerState p)
    {
        if (p == null) return;
        foreach (List<CardInstance> zone in new[] { p.Deck, p.Hand, p.Field, p.Graveyard })
        {
            if (zone == null) continue;
            foreach (CardInstance c in zone)
                if (c != null && !map.ContainsKey(c.InstanceId))
                    map[c.InstanceId] = c.CardId;
        }
    }
}
