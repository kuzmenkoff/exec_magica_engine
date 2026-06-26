using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;



public class RandomModel : BaseModel, IPlayerModel, ISimPlayerModel
{
    private string _name;
    public override string Name => _name;

    public Player self, opponent;
    public SimPlayer simSelf, simOpponent;

    bool LOGS = false;
    public RandomModel(SimPlayer self, SimPlayer opponent, string name)
    {
        this.simSelf = self;
        this.simOpponent = opponent;
        this._name = name;
    }
    public RandomModel(Player self, Player opponent, string name)
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
        GameManagerScr game = GameManagerScr.Instance;

        List<CardController> hand = game.Enemy.HandCards;
        List<CardController> field = game.Enemy.FieldCards;
        List<CardController> opponentField = game.Player.FieldCards;

        // Каст карт
        while (true)
        {
            var playableCards = hand
                .Where(c => c.Card.ManaCost <= game.Enemy.Mana)
                .Where(c =>
                    !(c.Card.SpellTarget == Card.TargetType.ALLY_CARD_TARGET && game.Enemy.FieldCards.Count == 0) &&
                    !(c.Card.SpellTarget == Card.TargetType.ENEMY_CARD_TARGET && game.Player.FieldCards.Count == 0)
                )
                .ToList();

            if (playableCards.Count == 0)
                break;

            var cardCtrl = playableCards[random.Next(playableCards.Count)];

            if (!cardCtrl.Card.IsSpell && field.Count >= 6)
                break;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (!cardCtrl.Card.IsSpell)
            {
                //UnityEngine.Debug.Log($"[AI] Casting: {cardCtrl.Card.Title}");
                stopwatch.Stop();
                UnityEngine.Debug.Log($"[TIME] Action (Play Card) took {stopwatch.ElapsedMilliseconds} ms");
                yield return CastEntity(cardCtrl);
            }
            else
            {
                CardController target = null;
                switch (cardCtrl.Card.SpellTarget)
                {
                    case Card.TargetType.ALLY_CARD_TARGET:
                        if (game.Enemy.FieldCards.Count > 0)
                            target = field[random.Next(field.Count)];
                        break;
                    case Card.TargetType.ENEMY_CARD_TARGET:
                        if (game.Player.FieldCards.Count > 0)
                            target = opponentField[random.Next(opponentField.Count)];
                        break;
                }
                //UnityEngine.Debug.Log($"[AI] Casting: {cardCtrl.Card.Title}");
                stopwatch.Stop();
                UnityEngine.Debug.Log($"[TIME] Action (Play Card) took {stopwatch.ElapsedMilliseconds} ms");
                yield return CastSpell(cardCtrl, target);
            }

            yield return new WaitForSeconds(0.3f);
        }

        // Атака
        List<CardController> attackers = field.FindAll(c => c.Card.CanAttack);

        foreach (var attacker in attackers)
        {
            var provokers = opponentField.FindAll(c => c.Card.Abilities.Contains(Card.AbilityType.PROVOCATION));
            var validTargets = provokers.Count > 0 ? provokers : opponentField;

            bool canAttackHero = provokers.Count == 0;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            if ((canAttackHero && random.NextDouble() > 0.7) || validTargets.Count == 0)
            {
                //UnityEngine.Debug.Log($"[{Name}] Attacking Hero: {attacker.Card.Title}");
                stopwatch.Stop();
                UnityEngine.Debug.Log($"[TIME] Action (Attack) took {stopwatch.ElapsedMilliseconds} ms");
                yield return AttackHero(attacker);
            }
            else
            {
                var target = validTargets[random.Next(validTargets.Count)];
                //UnityEngine.Debug.Log($"[{Name}] Attacking {target.Card.Title}: {attacker.Card.Title}");
                stopwatch.Stop();
                UnityEngine.Debug.Log($"[TIME] Action (Attack) took {stopwatch.ElapsedMilliseconds} ms");
                yield return AttackCard(attacker, target);
            }

            yield return new WaitForSeconds(0.3f);
        }

        // Завершення ходу
        game.ChangeTurn();
    }

    /*public void MakeMove(GameState gameState)
    {

        List<Card> selfHand = new List<Card>(simSelf.HandCards);
        List<Card> selfField = new List<Card>(simSelf.FieldCards);
        List<Card> opponentField = new List<Card>(simSelf.FieldCards);

        // ===== Cast cards =====
        Shuffle(selfHand);

        foreach (var card in selfHand)
        {
            UnityEngine.Debug.Log("ITERATION");
            if (!card.IsSpell)
            {
                if (gameState.TryPlayCard(card))
                {
                    if (LOGS)
                        UnityEngine.Debug.Log("PLAYING CARD:" + card.Title);
                    continue;
                }
                else
                {
                    if (LOGS)
                        UnityEngine.Debug.Log("COULD NOT PLAY CARD:" + card.Title);
                }

            }
            else
            {
                Card target = null;
                switch (card.SpellTarget)
                {
                    case Card.TargetType.ALLY_CARD_TARGET:
                        if (selfField.Count > 0)
                            target = selfField[random.Next(selfField.Count)];
                        break;
                    case Card.TargetType.ENEMY_CARD_TARGET:
                        if (opponentField.Count > 0)
                            target = opponentField[random.Next(opponentField.Count)];
                        break;
                }

                if (gameState.TryPlayCard(card, target))
                {
                    if (LOGS)
                        UnityEngine.Debug.Log("PLAYING CARD:" + card.Title);
                    continue;
                }
                else
                {
                    if (LOGS)
                        UnityEngine.Debug.Log("COULD NOT PLAY CARD:" + card.Title);
                }
            }
        }

        // ===== Use cards =====
        List<Card> attackers = simSelf.FieldCards.FindAll(c => c.CanAttack);

        foreach (var attacker in attackers)
        {
            var provokers = simOpponent.FieldCards.FindAll(c => c.Abilities.Contains(Card.AbilityType.PROVOCATION));
            var validTargets = provokers.Count > 0 ? provokers : simOpponent.FieldCards;

            bool canAttackHero = provokers.Count == 0;
            if (canAttackHero)
            {
                const int HERO_TARGET_INDEX = -1;

                List<int> allTargetIndices = new List<int>();
                for (int i = 0; i < validTargets.Count; i++)
                    allTargetIndices.Add(i);
                allTargetIndices.Add(HERO_TARGET_INDEX);

                int chosen = allTargetIndices[random.Next(allTargetIndices.Count)];
                if (chosen == HERO_TARGET_INDEX)
                {
                    gameState.TryAttackHero(attacker);
                }
                else
                {
                    Card target = validTargets[chosen];
                    gameState.TryAttackCard(attacker, target);
                }
            }
            else
            {
                if (validTargets.Count > 0)
                {
                    Card target = validTargets[random.Next(validTargets.Count)];
                    gameState.TryAttackCard(attacker, target);
                }
            }
        }
    }*/

    public void MakeMove(GameState game)
    {
        List<Card> hand = simSelf.HandCards;
        List<Card> field = simSelf.FieldCards;
        List<Card> opponentField = simOpponent.FieldCards;

        if (LOGS)
            UnityEngine.Debug.Log($"[{Name}] MAKE MOVE START");

        Shuffle(hand);
        int i = 0;
        // ===== Cast cards (доки є доступні) =====
        while (true)
        {
            i++;
            if (i > 5) break;
            var playableCards = simSelf.HandCards
                .Where(c => c.ManaCost <= simSelf.Mana)
                .ToList();

            if (playableCards.Count == 0)
                break;

            var card = playableCards[random.Next(playableCards.Count)];

            if (!card.IsSpell)
            {
                if (game.TryPlayCard(card))
                {
                    if (LOGS)
                        Console.WriteLine($"[{Name}] PLAYING CARD:" + card.Title);
                    continue;
                }

            }
            else
            {
                Card target = null;
                switch (card.SpellTarget)
                {
                    case Card.TargetType.ALLY_CARD_TARGET:
                        if (field.Count > 0)
                            target = field[random.Next(field.Count)];
                        break;
                    case Card.TargetType.ENEMY_CARD_TARGET:
                        if (opponentField.Count > 0)
                            target = opponentField[random.Next(opponentField.Count)];
                        break;
                }

                if (game.TryPlayCard(card, target))
                {
                    if (game.LOGS)
                        Console.WriteLine($"[{Name}] PLAYING CARD:" + card.Title);
                    continue;
                }
            }
            playableCards.Remove(card);
            if (playableCards.Count == 0)
                break;
        }

        // ===== Use cards =====
        List<Card> attackers = field.Where(c => c.CanAttack).ToList();

        foreach (var attacker in attackers)
        {
            var provokers = simOpponent.FieldCards
                .Where(c => c.Abilities.Contains(Card.AbilityType.PROVOCATION)).ToList();

            var validTargets = provokers.Count > 0
                ? provokers
                : simOpponent.FieldCards.ToList();

            bool canAttackHero = provokers.Count == 0;

            if (canAttackHero && random.NextDouble() < 0.5)
            {
                if (game.TryAttackHero(attacker) && LOGS)
                    UnityEngine.Debug.Log($"ATTACKED HERO with {attacker.Title}");
            }
            else if (validTargets.Count > 0)
            {
                var target = validTargets[random.Next(validTargets.Count)];
                if (game.TryAttackCard(attacker, target) && LOGS)
                    UnityEngine.Debug.Log($"ATTACKED CARD: {attacker.Title} -> {target.Title}");
            }
        }

        if (LOGS)
            UnityEngine.Debug.Log($"{Name} MAKE MOVE END");
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}


