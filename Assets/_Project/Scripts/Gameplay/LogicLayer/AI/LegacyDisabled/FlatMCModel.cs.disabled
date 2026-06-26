using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System.Diagnostics;

public class FlatMCModel : BaseModel, IPlayerModel
{

    private static int SIMULATIONS_PER_MOVE = 50;

    private string _name;
    public override string Name => _name;

    public Player self, opponent;

    public FlatMCModel(Player self, Player opponent, string name)
    {
        this.self = self;
        this.opponent = opponent;
        this._name = name;
    }

    public void MakeMove()
    {
        GameManagerScr.Instance.StartCoroutine(MakeMoveCoroutine());
    }

    private IEnumerator MakeMoveCoroutine()
    {
        GameState gameState = GameManagerScr.Instance.getGameState();

        int step = 1;
        // Cast
        while (true)
        {
            var stopwatch = Stopwatch.StartNew();
            var (bestCard, bestTarget) = EvaluateBestCardToPlay(gameState);
            stopwatch.Stop();
            UnityEngine.Debug.Log($"[TIME] Action (Play Card) took {stopwatch.ElapsedMilliseconds} ms");

            if (bestCard == null)
                break;

            gameState.TryPlayCard(bestCard, bestTarget);

            //UnityEngine.Debug.Log($"CARD TO PLAY: {bestCard.Title}");
            //UnityEngine.Debug.Log($"Step {step}: CARD TO PLAY: {bestCard.Title} | Решение заняло {elapsedMs} мс");
            step++;

            if (bestCard.IsSpell)
            {
                if (bestTarget == null)
                    yield return CastSpell(GameManagerScr.Instance.GetCardCByInstanceId(bestCard.InstanceId));
                else
                    yield return CastSpell(GameManagerScr.Instance.GetCardCByInstanceId(bestCard.InstanceId), GameManagerScr.Instance.GetCardCByInstanceId(bestTarget.InstanceId));
            }
            else
            {
                yield return CastEntity(GameManagerScr.Instance.GetCardCByInstanceId(bestCard.InstanceId));
            }
            yield return new WaitForSeconds(0.3f);
        }
        step = 1;
        // Attack
        while (true)
        {
            var stopwatch = Stopwatch.StartNew();
            var (attacker, target) = EvaluateBestAttackTarget(gameState);
            stopwatch.Stop();
            UnityEngine.Debug.Log($"[TIME] Action (Attack) took {stopwatch.ElapsedMilliseconds} ms");
            if (attacker == null)
                break;
            if (target != null)
            {
                gameState.TryAttackCard(attacker, target);
                //UnityEngine.Debug.Log($"Step {step}: ATTACK TO PLAY: {attacker.Title} ---> {target.Title} | Решение заняло {elapsedMs} мс");
            }
            else
            {
                gameState.TryAttackHero(attacker);
                //UnityEngine.Debug.Log($"Step {step}: ATTACK TO PLAY: {attacker.Title} ---> HERO | Решение заняло {elapsedMs} мс");
            }

            if (target == null)
                yield return AttackHero(GameManagerScr.Instance.GetCardCByInstanceId(attacker.InstanceId));
            else
                yield return AttackCard(GameManagerScr.Instance.GetCardCByInstanceId(attacker.InstanceId), GameManagerScr.Instance.GetCardCByInstanceId(target.InstanceId));
            yield return new WaitForSeconds(0.3f);
        }
        GameManagerScr.Instance.ChangeTurn();
    }

    public (Card bestCard, Card bestTarget) EvaluateBestCardToPlay(GameState originalGameState)
    {
        var currentPlayer = originalGameState.Player1Turn ? originalGameState.Player1 : originalGameState.Player2;
        var enemyPlayer = originalGameState.Player1Turn ? originalGameState.Player2 : originalGameState.Player1;

        if (currentPlayer.FieldCards.Count >= 6)
            return (null, null);

        Card bestCard = null;
        Card bestTarget = null;
        double bestWinRate = 0;

        for (int cardIndex = 0; cardIndex < currentPlayer.HandCards.Count; cardIndex++)
        {
            Card card = currentPlayer.HandCards[cardIndex];
            if (card.ManaCost > currentPlayer.Mana)
                continue;

            List<Card> possibleTargets = null;

            if (card.IsSpell)
            {
                switch (card.SpellTarget)
                {
                    case Card.TargetType.ALLY_CARD_TARGET:
                        possibleTargets = currentPlayer.FieldCards;
                        break;
                    case Card.TargetType.ENEMY_CARD_TARGET:
                        possibleTargets = enemyPlayer.FieldCards;
                        break;
                    case Card.TargetType.NO_TARGET:
                        possibleTargets = new List<Card> { null };
                        break;
                }
            }
            else
            {
                possibleTargets = new List<Card> { null };
            }

            foreach (var target in possibleTargets)
            {
                int wins = 0;

                for (int sim = 0; sim < SIMULATIONS_PER_MOVE; sim++)
                {
                    GameState simState = originalGameState.GetDeepCopy();

                    Card simCard = FindMatchingCard(card, simState);
                    Card simTarget = target == null ? null : FindMatchingCard(target, simState);

                    if (simCard != null && simState.TryPlayCard(simCard, simTarget))
                    {
                        SimPlayer simPlayer = simState.Player1Turn ? simState.Player1 : simState.Player2;
                        SimPlayer simOpponent = simState.Player1Turn ? simState.Player2 : simState.Player1;
                        var model1 = new RandomModel(simPlayer, simOpponent, "SimSelf");
                        model1.MakeMove(simState);
                        simState.EndTurn();
                        simPlayer = simState.Player1Turn ? simState.Player1 : simState.Player2;
                        simOpponent = simState.Player1Turn ? simState.Player2 : simState.Player1;
                        var model1new = new RandomModel(simPlayer, simOpponent, "SimSelf");
                        var model2new = new RandomModel(simOpponent, simPlayer, "SimOpponent");
                        simState.SimulateGame(model1new, model2new);
                        bool selfWasFirst = originalGameState.Player1Turn;
                        bool selfWon = selfWasFirst ? simState.Player1Win : !simState.Player1Win;

                        if (selfWon)
                            wins++;
                    }
                }
                double winRate = (double)wins / SIMULATIONS_PER_MOVE;

                string targetTitle = target == null ? "NO TARGET" : target.Title;
               // UnityEngine.Debug.Log($"Card: {card.Title} | Target: {targetTitle} | WinRate: {winRate:0.000} ({wins}/{SIMULATIONS_PER_MOVE})");

                if (winRate >= bestWinRate)
                {
                    bestWinRate = winRate;
                    bestCard = card;
                    bestTarget = target;
                }
            }
        }

        return (bestCard, bestTarget);
    }

    public (Card attacker, Card target) EvaluateBestAttackTarget(GameState originalGameState)
    {
        SimPlayer currentPlayer = originalGameState.Player1Turn ? originalGameState.Player1 : originalGameState.Player2;
        SimPlayer enemyPlayer = originalGameState.Player1Turn ? originalGameState.Player2 : originalGameState.Player1;

        List<Card> attackers = currentPlayer.FieldCards.Where(c => c.CanAttack).ToList();
        List<Card> provokers = enemyPlayer.FieldCards.Where(c => c.Abilities.Contains(Card.AbilityType.PROVOCATION)).ToList();
        List<Card> potentialTargets = provokers.Count > 0 ? provokers : new List<Card>(enemyPlayer.FieldCards);

        if (provokers.Count == 0)
            potentialTargets.Add(null);

        Card bestAttacker = null;
        Card bestTarget = null;
        double bestWinRate = 0;

        foreach (var attacker in attackers)
        {
            foreach (var target in potentialTargets)
            {
                int wins = 0;

                for (int sim = 0; sim < SIMULATIONS_PER_MOVE; sim++)
                {
                    GameState simState = originalGameState.GetDeepCopy();

                    Card simAttacker = FindMatchingCard(attacker, simState);
                    Card simTarget = target == null ? null : FindMatchingCard(target, simState);

                    if (simAttacker == null) continue;

                    bool success = (simTarget == null)
                        ? simState.TryAttackHero(simAttacker)
                        : simState.TryAttackCard(simAttacker, simTarget);

                    if (!success) continue;

                    SimPlayer simPlayer = simState.Player1Turn ? simState.Player1 : simState.Player2;
                    SimPlayer simOpponent = simState.Player1Turn ? simState.Player2 : simState.Player1;

                    var simSelfModel = new RandomModel(simPlayer, simOpponent, "SimSelf");
                    var simEnemyModel = new RandomModel(simOpponent, simPlayer, "SimOpponent");

                    // Даем возможности модели доиграть оставшуюся часть хода
                    simSelfModel.MakeMove(simState);

                    // Завершаем ход и начинаем полную симуляцию
                    simState.EndTurn();

                    simPlayer = simState.Player1Turn ? simState.Player1 : simState.Player2;
                    simOpponent = simState.Player1Turn ? simState.Player2 : simState.Player1;

                    var simSelfModelnew = new RandomModel(simPlayer, simOpponent, "SimSelf");
                    var simEnemyModelnew = new RandomModel(simOpponent, simPlayer, "SimOpponent");
                    simState.SimulateGame(simSelfModelnew, simEnemyModelnew);

                    bool selfWasFirst = originalGameState.Player1Turn;
                    if (selfWasFirst && simState.Player1Win) wins++;
                    if (!selfWasFirst && !simState.Player1Win) wins++;
                }

                double winRate = (double)wins / SIMULATIONS_PER_MOVE;

                // Логируем результат для каждой пары attacker/target:
                string attackerTitle = attacker?.Title ?? "null";
                string targetTitle = target?.Title ?? "HERO";
                //UnityEngine.Debug.Log($"Attack: {attackerTitle} → {targetTitle} | WinRate: {winRate:0.000} ({wins}/{SIMULATIONS_PER_MOVE})");

                if ((winRate == (double)1) && target == null)
                    return (bestAttacker, null);


                if (winRate >= bestWinRate)
                {
                    bestWinRate = winRate;
                    bestAttacker = attacker;
                    bestTarget = target;
                }
            }
        }

        return (bestAttacker, bestTarget);
    }

    private Card FindMatchingCard(Card original, GameState simState)
    {
        if (original == null || original.InstanceId == 0)
        {
            return null;
        }

        var match = simState.Player1.HandCards
            .Concat(simState.Player1.FieldCards)
            .Concat(simState.Player2.HandCards)
            .Concat(simState.Player2.FieldCards)
            .FirstOrDefault(c => c != null && c.InstanceId == original.InstanceId);

        return match;
    }


}
