using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static MCTSModel.MCTSNode;
using static UnityEngine.GraphicsBuffer;

public class MCTSModel : BaseModel, IPlayerModel
{
    private static int ITERATION_LIMIT = 1200;

    private string _name;
    public override string Name => _name;

    public Player self, opponent;

    public MCTSModel(Player self, Player opponent, string name)
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
        //UnityEngine.Debug.Log($"ROUND {GameManagerScr.Instance.Turn} MOVE:");
        while (true)
        {
            var stopwatch = Stopwatch.StartNew();
            GameState gameState = GameManagerScr.Instance.getGameState();
            var root = new MCTSNode(gameState);
            if (root.IsTerminal)
            {
                GameManagerScr.Instance.ChangeTurn();
                break;
            }
            var mcts = new MCTS();
            var bestNode = mcts.Run(root);

            root.PrintTree(0, 1);

            switch (bestNode.ActionType)
            {
                case ActionType.PlayCard:
                    if (bestNode.PlayedCard.IsSpell)
                    {
                        if (bestNode.Target == null)
                        {
                            UnityEngine.Debug.Log($"[TIME] Action (Play Card) took {stopwatch.ElapsedMilliseconds} ms");
                            yield return CastSpell(GameManagerScr.Instance.GetCardCByInstanceId(bestNode.PlayedCard.InstanceId));
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"[TIME] Action (Play Card) took {stopwatch.ElapsedMilliseconds} ms");
                            yield return CastSpell(GameManagerScr.Instance.GetCardCByInstanceId(bestNode.PlayedCard.InstanceId), GameManagerScr.Instance.GetCardCByInstanceId(bestNode.Target.InstanceId));
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"[TIME] Action (Play Card) took {stopwatch.ElapsedMilliseconds} ms");
                        yield return CastEntity(GameManagerScr.Instance.GetCardCByInstanceId(bestNode.PlayedCard.InstanceId));
                    }
                    break;
                case ActionType.Attack:
                    if (bestNode.Target == null)
                    {
                        UnityEngine.Debug.Log($"[TIME] Action (Attack) took {stopwatch.ElapsedMilliseconds} ms");
                        yield return AttackHero(GameManagerScr.Instance.GetCardCByInstanceId(bestNode.PlayedCard.InstanceId));
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"[TIME] Action (Attack) took {stopwatch.ElapsedMilliseconds} ms");
                        yield return AttackCard(GameManagerScr.Instance.GetCardCByInstanceId(bestNode.PlayedCard.InstanceId), GameManagerScr.Instance.GetCardCByInstanceId(bestNode.Target.InstanceId));
                    }
                    break;
                case ActionType.EndTurn:
                    break;
            }
        yield return new WaitForSeconds(0.3f);
        }
    }

    public enum ActionType
    {
        PlayCard,
        Attack,
        EndTurn
    }

    public class MCTSNode
    {
        public GameState State;
        public MCTSNode Parent;
        public List<MCTSNode> Children = new List<MCTSNode>();

        public int VisitCount;
        public int WinScore;

        public Card PlayedCard;
        public Card Target;
        public ActionType? ActionType;
        public bool IsTerminal => ActionType == MCTSModel.ActionType.EndTurn || GetPossibleActions().Count <= 1;

        public MCTSNode(GameState state, MCTSNode parent = null)
        {
            State = state;
            Parent = parent;
        }

        public void PrintTree(int depth = 0, int maxDepth = 10)
        {
            if (depth > maxDepth)
                return;

            string indent = new string(' ', depth * 4);

            string action = ActionType.HasValue ? ActionType.Value.ToString() : "Root";
            string card = PlayedCard != null ? PlayedCard.Title : "-";
            string target = Target != null ? Target.Title : "-";
            string visits = $"Visits: {VisitCount}";
            string winrate = VisitCount > 0 ? $"WR: {(double)WinScore / VisitCount:0.00}" : "WR: -";

            UnityEngine.Debug.Log($"{indent}- [{action}] Card: {card}, Target: {target}, {visits}, {winrate}");

            foreach (var child in Children)
            {
                child.PrintTree(depth + 1, maxDepth);
            }
        }

        public List<(ActionType, Card, Card)> GetPossibleActions()
        {
            var actions = new List<(ActionType, Card, Card)>();

            if (this.ActionType == MCTSModel.ActionType.EndTurn)
                return actions;

            var currentPlayer = State.Player1Turn ? State.Player1 : State.Player2;
            var opponentPlayer = State.Player1Turn ? State.Player2 : State.Player1;

            // Adding possible card plays
            foreach (var card in currentPlayer.HandCards)
            {
                if (card.ManaCost > currentPlayer.Mana)
                    continue;

                if (card.IsSpell)
                {
                    switch (card.SpellTarget)
                    {
                        case Card.TargetType.NO_TARGET:
                            actions.Add((MCTSModel.ActionType.PlayCard, card, null));
                            break;
                        case Card.TargetType.ALLY_CARD_TARGET:
                            foreach (var ally in currentPlayer.FieldCards)
                                actions.Add((MCTSModel.ActionType.PlayCard, card, ally));
                            break;
                        case Card.TargetType.ENEMY_CARD_TARGET:
                            foreach (var enemy in opponentPlayer.FieldCards)
                                actions.Add((MCTSModel.ActionType.PlayCard, card, enemy));
                            break;
                    }
                }
                else
                {
                    actions.Add((MCTSModel.ActionType.PlayCard, card, null));
                }
            }
            // Adding possible attacks
            var attackers = currentPlayer.FieldCards.Where(c => c.CanAttack).ToList();
            var provokers = opponentPlayer.FieldCards
                .Where(c => c.Abilities.Contains(Card.AbilityType.PROVOCATION))
                .ToList();

            List<Card> validTargets = provokers.Count > 0
                ? provokers
                : new List<Card>(opponentPlayer.FieldCards) { null }; // null = атака героя

            foreach (var attacker in attackers)
            {
                foreach (var target in validTargets)
                {
                    actions.Add((MCTSModel.ActionType.Attack, attacker, target));
                }
            }

            // Add EndTurn as a valid option always
            actions.Add((MCTSModel.ActionType.EndTurn, null, null));

            return actions;
        }

        public List<MCTSNode> ExpandAll()
        {

            if (this.ActionType == MCTSModel.ActionType.EndTurn)
                return Children;

            if (Children.Count != 0)
                return Children;

            var newChildren = new List<MCTSNode>();

            var untriedActions = GetPossibleActions()
                .Where(a => !Children.Any(c =>
                    c.ActionType == a.Item1 &&
                    c.PlayedCard?.InstanceId == a.Item2?.InstanceId &&
                    c.Target?.InstanceId == a.Item3?.InstanceId))
                .ToList();

            foreach (var (actionType, card, target) in untriedActions)
            {
                GameState newState = State.GetDeepCopy();

                Card matchedCard = card == null ? null : FindMatchingCard(card, newState);
                Card matchedTarget = target == null ? null : FindMatchingCard(target, newState);

                bool success = false;

                switch (actionType)
                {
                    case MCTSModel.ActionType.PlayCard:
                        success = newState.TryPlayCard(matchedCard, matchedTarget);
                        break;
                    case MCTSModel.ActionType.Attack:
                        success = matchedTarget == null
                            ? newState.TryAttackHero(matchedCard)
                            : newState.TryAttackCard(matchedCard, matchedTarget);
                        break;
                    case MCTSModel.ActionType.EndTurn:
                        newState.EndTurn();
                        success = true;
                        break;
                }

                if (!success)
                {
                    //Console.WriteLine($"FAILED TO EXECUTE ACTION: {actionType} → {card?.Title} → {target?.Title ?? "null"}");
                    continue;
                }

                var childNode = new MCTSNode(newState, this)
                {
                    ActionType = actionType,
                    PlayedCard = matchedCard,
                    Target = matchedTarget
                };

                Children.Add(childNode);
                newChildren.Add(childNode);
            }

            return newChildren;
        }

        private Card FindMatchingCard(Card original, GameState newState)
        {
            var allCards = newState.Player1.HandCards
                .Concat(newState.Player1.FieldCards)
                .Concat(newState.Player2.HandCards)
                .Concat(newState.Player2.FieldCards);

            return allCards.FirstOrDefault(c => c.InstanceId == original.InstanceId);
        }

        public class MCTS
        {

            public MCTSModel.MCTSNode Run(MCTSModel.MCTSNode root)
            {
                for (int i = 0; i < ITERATION_LIMIT; i++)
                {

                    // 1. Selection
                    var promisingNode = SelectPromisingNode(root);
                    // 2. Expansion
                    List<MCTSNode> expandedNodes = promisingNode.ExpandAll();

                    //root.PrintTree();

                    var nodeToExplore = expandedNodes.Count > 0 ? expandedNodes[random.Next(expandedNodes.Count)] : promisingNode;
                    //Console.WriteLine("Simulation for Acton type: " + nodeToExplore.ActionType);
                    // 3. Simulation
                    int simulationResult = SimulatePlayout(nodeToExplore.State, nodeToExplore.ActionType);
                    //Console.WriteLine(simulationResult);


                    // 4. Backpropagation
                    BackPropagate(nodeToExplore, simulationResult);
                }

                return root.Children.OrderByDescending(c => c.VisitCount).FirstOrDefault();
            }

            private MCTSModel.MCTSNode SelectPromisingNode(MCTSModel.MCTSNode node)
            {
                while (node.Children.Count > 0)
                {
                    node = UCT.FindBestNodeWithUCT(node);
                }
                return node;
            }

            private int SimulatePlayout(GameState simState, MCTSModel.ActionType? actionType)
            {
                GameState copy = simState.GetDeepCopy();

                SimPlayer player, opponent;
                if (actionType == MCTSModel.ActionType.EndTurn)
                {
                    player = copy.Player2;
                    opponent = copy.Player1;

                    var simModel = new RandomModel(player, opponent, "SimSelf");
                    var simEnemy = new RandomModel(opponent, player, "SimOpponent");

                    copy.SimulateGame(simEnemy, simModel);
                    return (!copy.Player1Win) ? 1 : 0;
                }
                else
                {
                    player = copy.Player2;
                    opponent = copy.Player1;


                    var simModel = new RandomModel(player, opponent, "SimSelf");

                    simModel.MakeMove(copy);
                    copy.EndTurn();

                    player = copy.Player2;
                    opponent = copy.Player1;

                    var simModelnew = new RandomModel(player, opponent, "SimSelf");
                    var simEnemynew = new RandomModel(opponent, player, "SimOpponent");

                    copy.SimulateGame(simEnemynew, simModelnew);
                    return (!copy.Player1Win) ? 1 : 0;
                }

                //return (copy.Player1Turn && copy.Player1Win) || (!copy.Player1Turn && !copy.Player1Win) ? 1 : 0;
                
            }

            private void BackPropagate(MCTSModel.MCTSNode node, int result)
            {
                while (node != null)
                {
                    node.VisitCount++;
                    node.WinScore += result;
                    node = node.Parent;
                }
            }
        }

        public static class UCT
        {
            public static double UCTValue(int totalVisit, int nodeVisit, double winScore)
            {
                if (nodeVisit == 0)
                    return double.MaxValue;
                return (winScore / nodeVisit) + 1.41 * Math.Sqrt(Math.Log(totalVisit) / nodeVisit);
            }

            public static MCTSModel.MCTSNode FindBestNodeWithUCT(MCTSModel.MCTSNode parent)
            {
                int totalVisit = parent.Children.Sum(c => c.VisitCount);

                return parent.Children
                    .OrderByDescending(c => UCTValue(totalVisit, c.VisitCount, c.WinScore))
                    .First();
            }
        }
    }

}
