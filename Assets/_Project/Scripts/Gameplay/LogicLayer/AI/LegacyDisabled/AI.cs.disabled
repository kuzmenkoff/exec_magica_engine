/*using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using UnityEditor.UIElements;
using UnityEngine;
//using System.Threading.Tasks;
using static Card;
using UnityEditor.Experimental.GraphView;
using System.Threading;
using static UnityEngine.GraphicsBuffer;
//using System.Threading;

public class AI : MonoBehaviour
{
    GameState gameState;
    const int NumberOfSimulationsForCast = 100;
    const int NumberOfSimulationsForSpellTarget = 100;
    const int NumberOfSimulationsForAttackWithProvocation = 100;
    const int NumberOfSimulationsForAttack = 100;

    public bool CourutineIsRunning = false;
    public bool SubCourutineIsRunning = false;
    public bool SubSubCourutineIsRunning = false;

    DecksManagerScr decksManager;
    public void MakeTurn()
    {
        StartCoroutine(EnemyTurn(GameManagerScr.Instance.EnemyHandCards));
    }

    IEnumerator EnemyTurn(List<CardController> cards)
    {
        decksManager = UnityEngine.Object.FindObjectOfType<DecksManagerScr>();
        CourutineIsRunning = true;
        yield return new WaitForSeconds(1);

        //Casting cards
        int targetindex;
        List<CardController> cardsList = cards.FindAll(x => GameManagerScr.Instance.CurrentGame.Enemy.Mana >= x.Card.ManaCost);

        //int randomCount = UnityEngine.Random.Range(0, cards.Count);
        while (cardsList.Count > 0)
        {
            if (GameManagerScr.Instance.EnemyFieldCards.Count > 5 ||
                GameManagerScr.Instance.CurrentGame.Enemy.Mana == 0 ||
                GameManagerScr.Instance.EnemyHandCards.Count == 0)
                break;

            if (cardsList.Count == 0)
                break;
            int index = FindBestCardToCast(cardsList);
            if (index == -1)
                break;

            if (cardsList[index].Card.IsSpell)
            {

                if (cardsList[index].Card.SpellTarget == Card.TargetType.ALLY_CARD_TARGET)
                {
                    if (GameManagerScr.Instance.EnemyFieldCards.Count == 0)
                    {
                        cardsList = cards.FindAll(x => GameManagerScr.Instance.CurrentGame.Enemy.Mana >= x.Card.ManaCost);
                        cardsList.RemoveAt(index);
                        
                        continue;
                    }
                    else if (GameManagerScr.Instance.EnemyFieldCards.Count == 1)
                        targetindex = 0;
                    else
                        targetindex = FindBestTargetForSpell(index, GameManagerScr.Instance.EnemyFieldCards);
                    CastSpell(cardsList[index], targetindex);
                    while (SubCourutineIsRunning)
                        yield return new WaitForSeconds(0.1f);
                }
                else if (cardsList[index].Card.SpellTarget == Card.TargetType.ENEMY_CARD_TARGET)
                {
                    if (GameManagerScr.Instance.PlayerFieldCards.Count == 0)
                    {
                        cardsList = cards.FindAll(x => GameManagerScr.Instance.CurrentGame.Enemy.Mana >= x.Card.ManaCost);
                        cardsList.RemoveAt(index);
                        continue;
                    }
                    else if (GameManagerScr.Instance.PlayerFieldCards.Count == 1)
                        targetindex = 0;
                    else
                        targetindex = FindBestTargetForSpell(index, GameManagerScr.Instance.PlayerFieldCards);
                    CastSpell(cardsList[index], targetindex);
                    while (SubCourutineIsRunning)
                        yield return new WaitForSeconds(0.1f);
                }
                else
                    CastSpell(cardsList[index], -1);
                while (SubCourutineIsRunning)
                    yield return new WaitForSeconds(0.1f);

                UIController.Instance.UpdateHPAndMana();
            }
            else
            {
                cardsList[index].GetComponent<CardMovementScr>().MoveToField(GameManagerScr.Instance.EnemyField);
                yield return new WaitForSeconds(.51f);
                cardsList[index].transform.SetParent(GameManagerScr.Instance.EnemyField);
                cardsList[index].OnCast();
                UIController.Instance.UpdateHPAndMana();
                cardsList = cards.FindAll(x => GameManagerScr.Instance.CurrentGame.Enemy.Mana >= x.Card.ManaCost);
            }
            cardsList = cards.FindAll(x => GameManagerScr.Instance.CurrentGame.Enemy.Mana >= x.Card.ManaCost);

        }

        yield return new WaitForSeconds(1);

        //Using cards

        while (GameManagerScr.Instance.EnemyFieldCards.Exists(x => x.Card.CanAttack))
        {
            CardController enemy, attacker;
            var activeCards = GameManagerScr.Instance.EnemyFieldCards.FindAll(x => x.Card.CanAttack);
            bool hasProvocation = GameManagerScr.Instance.PlayerFieldCards.Exists(x => x.Card.IsProvocation);
            if (hasProvocation)
            {
                int enemyIndex = GameManagerScr.Instance.PlayerFieldCards.FindIndex(x => x.Card.IsProvocation);
                if (activeCards.Count == 1)
                    attacker = activeCards[0];
                else
                    attacker = activeCards[FindBestAttacker(enemyIndex, activeCards)];
                enemy = GameManagerScr.Instance.PlayerFieldCards[enemyIndex];

                UnityEngine.Debug.Log(attacker.Card.Title + " (" + attacker.Card.Attack + "; " + attacker.Card.HP + ") ---> " +
                          enemy.Card.Title + " (" + enemy.Card.Attack + "; " + enemy.Card.HP + ")");

                attacker.GetComponent<CardMovementScr>().MoveToTarget(enemy.transform);
                while (SubSubCourutineIsRunning)
                    yield return new WaitForSeconds(0.1f);
                GameManagerScr.Instance.CardsFight(enemy, attacker);
                attacker.Card.CanAttack = false;
            }
            else
            {
                //for (int i = 0; i < activeCards.Count; i++)
                attacker = activeCards[0];
                if (GameManagerScr.Instance.PlayerFieldCards.Count == 0)
                    targetindex = -1;
                else
                    targetindex = FindBestTargetForEntity(0, GameManagerScr.Instance.PlayerFieldCards);
                if (targetindex == -1)
                {
                    UnityEngine.Debug.Log(attacker.Card.Title + " (" + attacker.Card.Attack + "; " + attacker.Card.HP + ") ---> Hero");
                    attacker.GetComponent<CardMovementScr>().MoveToTarget(GameManagerScr.Instance.PlayerHero.transform);
                    while (SubSubCourutineIsRunning)
                        yield return new WaitForSeconds(0.1f);
                    GameManagerScr.Instance.DamageHero(attacker, false);
                    attacker.Card.CanAttack = false;

                }
                else
                {
                    enemy = GameManagerScr.Instance.PlayerFieldCards[targetindex];
                    UnityEngine.Debug.Log(attacker.Card.Title + " (" + attacker.Card.Attack + "; " + attacker.Card.HP + ") ---> " +
                    enemy.Card.Title + " (" + enemy.Card.Attack + "; " + enemy.Card.HP + ")");
                    attacker.GetComponent<CardMovementScr>().MoveToTarget(enemy.transform);
                    while (SubSubCourutineIsRunning)
                        yield return new WaitForSeconds(0.1f);
                    GameManagerScr.Instance.CardsFight(enemy, attacker);
                    attacker.Card.CanAttack = false;
                }



            }

        }

        yield return new WaitForSeconds(1);

        CourutineIsRunning = false;
        GameManagerScr.Instance.ChangeTurn();
    }

    int FindBestCardToCast(List<CardController> cards)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        int[] NumOfWins = new int[cards.Count + 1];
        for (int i = 0; i < cards.Count; i++)
        {
            NumOfWins[i] = 0;

            for (int sim = 0; sim < NumberOfSimulationsForCast; sim++)
            {
                GameState gameState = new GameState(decksManager);
                Card card = cards[i].Card.GetDeepCopy();
                gameState.AIFieldCards.Add(card);
                gameState.SimulateGame(0);

                if (gameState.Win)
                {
                    NumOfWins[i]++;
                }
            }
            UnityEngine.Debug.Log("Card " + cards[i].Card.Title + " HP: " + cards[i].Card.HP + " has got winrate: " + NumOfWins[i] + "/ " + NumberOfSimulationsForCast);
        }
        NumOfWins[cards.Count] = 0;

        for (int sim = 0; sim < NumberOfSimulationsForCast; sim++)
        {
            gameState = new GameState(decksManager);
            Card card = new Card();
            gameState.SimulateGame(0);

            if (gameState.Win)
            {
                NumOfWins[cards.Count]++;
            }
        }
        UnityEngine.Debug.Log("No card has got winrate: " + NumOfWins[cards.Count] + "/ " + NumberOfSimulationsForCast);
        int index = 0;
        if (GameManagerScr.Instance.Difficulty == "Hard")
            index = FindBiggestElementIndex(NumOfWins);
        else if (GameManagerScr.Instance.Difficulty == "Normal")
            index = FindAverageElementIndex(NumOfWins);
        else if (GameManagerScr.Instance.Difficulty == "Easy")
            index = FindSmallestElementIndex(NumOfWins);

        stopwatch.Stop();
        UnityEngine.Debug.Log("Execution Time of FindBestCardToCast: " + stopwatch.ElapsedMilliseconds + " ms");

        if (index == cards.Count)
        {

            return -1;

        }
        return index;
    }

    int FindBestTargetForSpell(int cardindex, List<CardController> targets)
    {
        int[] NumOfWins = new int[targets.Count + 1];

        for (int i = 0; i < targets.Count; i++)
        {
            NumOfWins[i] = 0;

            for (int sim = 0; sim < NumberOfSimulationsForSpellTarget; sim++)
            {
                GameState gameState = new GameState(decksManager);
                if (gameState.AIHandCards[cardindex].SpellTarget == Card.TargetType.ALLY_CARD_TARGET)
                {
                    gameState.CastSpellOnTarget(gameState.AIHandCards[cardindex], gameState.AIFieldCards[i]);
                }
                else if (gameState.AIHandCards[cardindex].SpellTarget == Card.TargetType.ENEMY_CARD_TARGET)
                {
                    gameState.CastSpellOnTarget(gameState.AIHandCards[cardindex], gameState.PlayerFieldCards[i]);
                }

                gameState.CastCards(true);

                if (gameState.CheckForVictory())
                {
                    gameState.Win = gameState.ReturnResult();
                }
                else
                {
                    gameState.UseCards(true);
                    if (gameState.CheckForVictory())
                    {
                        gameState.Win = gameState.ReturnResult();
                    }
                    else
                    {
                        gameState.AITurn = false;
                        gameState.SimulateGame(1);
                    }
                }

                if (gameState.Win)
                {
                    NumOfWins[i]++;
                }
            }


            UnityEngine.Debug.Log($"Target {targets[i].Card.Title} HP: {targets[i].Card.HP} has got winrate: {NumOfWins[i]}/{NumberOfSimulationsForSpellTarget}");
        }

        if (GameManagerScr.Instance.Difficulty == "Hard")
            return FindBiggestElementIndex(NumOfWins.ToArray());
        else if (GameManagerScr.Instance.Difficulty == "Normal")
            return FindAverageElementIndex(NumOfWins.ToArray());
        else if (GameManagerScr.Instance.Difficulty == "Easy")
            return FindSmallestElementIndex(NumOfWins.ToArray());

        return FindBiggestElementIndex(NumOfWins.ToArray());
    }


    int FindBestTargetForEntity(int attackerIndex, List<CardController> targets)
    {
        int index = 0;

        int[] NumOfWins = new int[targets.Count + 1];

       
        for (int i = 0; i < targets.Count; i++) {
            NumOfWins[i] = 0;
            for (int sim = 0; sim < NumberOfSimulationsForAttack; sim++)
            {
                GameState gameState = new GameState(decksManager);

                gameState.CardsFight(
                    gameState.AIFieldCards.FindAll(x => x.CanAttack)[attackerIndex],
                    gameState.PlayerFieldCards[i]
                );

                gameState.UseCards(true);
                if (gameState.CheckForVictory())
                {
                    gameState.Win = gameState.ReturnResult();
                }
                else
                {
                    gameState.AITurn = false;
                    gameState.SimulateGame(1);
                }
                if (gameState.Win)
                    NumOfWins[i]++;
            }
            UnityEngine.Debug.Log("Target " + targets[i].Card.Title + " HP: " + targets[i].Card.HP + " has got winrate: " + NumOfWins[i] + "/ " + NumberOfSimulationsForAttack);

            
        }
        NumOfWins[targets.Count] = 0;
        for (int sim = 0; sim < NumberOfSimulationsForAttack; sim++)
        {
            GameState gameState = new GameState(decksManager);

            gameState.DamageHero(true, gameState.AIFieldCards.FindAll(x => x.CanAttack)[attackerIndex]);

            if (gameState.CheckForVictory())
            {
                gameState.Win = gameState.ReturnResult();
            }
            else
            {
                gameState.UseCards(true);
                if (gameState.CheckForVictory())
                {
                    gameState.Win = gameState.ReturnResult();
                }
                else
                {
                    gameState.AITurn = false;
                    gameState.SimulateGame(1);
                }
            }
            if (gameState.Win)
                NumOfWins[targets.Count]++;
        }

        UnityEngine.Debug.Log($"Hero target has got winrate: {NumOfWins[targets.Count]}/{NumberOfSimulationsForAttack}");

        // Вибір найкращого індексу залежно від складності
        if (GameManagerScr.Instance.Difficulty == "Hard")
            if (NumOfWins[targets.Count] == 100)
                index = targets.Count;
            else
                index = FindBiggestElementIndex(NumOfWins);
        else if (GameManagerScr.Instance.Difficulty == "Normal")
            index = FindAverageElementIndex(NumOfWins);
        else if (GameManagerScr.Instance.Difficulty == "Easy")
            index = FindSmallestElementIndex(NumOfWins);

        if (index == targets.Count)
            return -1;

        return index;
    }


    int FindBestAttacker(int targetIndex, List<CardController> cards)
    {
        if (cards.Count == 0)
            return 0;

        int[] NumOfWins = new int[cards.Count];

        for (int i = 0; i < cards.Count; i++)
        {
            NumOfWins[i] = 0;
            
            for (int sim = 0; sim < NumberOfSimulationsForAttackWithProvocation; sim++)
            {
                GameState gameState = new GameState(decksManager);

                gameState.CardsFight(
                    gameState.AIFieldCards.FindAll(x => x.CanAttack)[i],
                    gameState.PlayerFieldCards[targetIndex]
                );

                gameState.UseCards(true);

                if (gameState.CheckForVictory())
                {
                    gameState.Win = gameState.ReturnResult();
                }
                else
                {
                    gameState.AITurn = false;
                    gameState.SimulateGame(1);
                }

                if (gameState.Win)
                {
                    NumOfWins[i]++;
                }
            }
        }

        if (GameManagerScr.Instance.Difficulty == "Hard")
            return FindBiggestElementIndex(NumOfWins);
        else if (GameManagerScr.Instance.Difficulty == "Normal")
            return FindAverageElementIndex(NumOfWins);
        else if (GameManagerScr.Instance.Difficulty == "Easy")
            return FindSmallestElementIndex(NumOfWins);

        return FindBiggestElementIndex(NumOfWins);
    }


    int FindBiggestElementIndex(int[] ints)
    {
        int maxNumber = int.MinValue;
        int maxIndex = -1;
        for (int i = 0; i < ints.Length; i++)
        {
            if (ints[i] > maxNumber)
            {
                maxNumber = ints[i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }

    int FindAverageElementIndex(int[] ints)
    {
        double average = ints.Average();

        int closestIndex = -1;
        double minDifference = double.MaxValue;

        // Iterate through the list to find the element closest to the average
        for (int i = 0; i < ints.Length; i++)
        {
            double difference = Math.Abs(ints[i] - average);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    int FindSmallestElementIndex(int[] ints)
    {
        int minNumber = int.MaxValue;
        int minIndex = -1;
        for (int i = 0; i < ints.Length; i++)
        {
            if (ints[i] < minNumber)
            {
                minNumber = ints[i];
                minIndex = i;
            }
        }
        return minIndex;
    }

    void CastSpell(CardController card, int targetindex)
    {
        card.Info.ShowCardInfo();
        switch (card.Card.SpellTarget)
        {
            case Card.TargetType.NO_TARGET:
                switch (card.Card.Spell)
                {
                    case Card.SpellType.HEAL_ALLY_FIELD_CARDS:
                        if (GameManagerScr.Instance.EnemyFieldCards.Count > 0)
                            StartCoroutine(CastCard(card));


                        break;

                    case Card.SpellType.DAMAGE_ENEMY_FIELD_CARDS:
                        if (GameManagerScr.Instance.EnemyFieldCards.Count > 0)
                            StartCoroutine(CastCard(card));

                        break;

                    case Card.SpellType.HEAL_ALLY_HERO:
                        StartCoroutine(CastCard(card));
                        break;

                    case Card.SpellType.DAMAGE_ENEMY_HERO:
                        StartCoroutine(CastCard(card));
                        break;
                }
                break;

            case Card.TargetType.ALLY_CARD_TARGET:
                if (GameManagerScr.Instance.EnemyFieldCards.Count > 0)
                    StartCoroutine(CastCard(card,
                        GameManagerScr.Instance.EnemyFieldCards[targetindex]));
                break;

            case Card.TargetType.ENEMY_CARD_TARGET:
                if (GameManagerScr.Instance.PlayerFieldCards.Count > 0)
                    StartCoroutine(CastCard(card,
                        GameManagerScr.Instance.PlayerFieldCards[targetindex]));

                break;
        }
    }

    IEnumerator CastCard(CardController spell, CardController target = null)
    {
        SubCourutineIsRunning = true;
        if (spell.Card.SpellTarget == Card.TargetType.NO_TARGET)
        {
            spell.Info.ShowCardInfo();
            spell.GetComponent<CardMovementScr>().MoveToField(GameManagerScr.Instance.EnemyField);
            while (SubSubCourutineIsRunning)
                yield return new WaitForSeconds(0.1f);

            spell.OnCast();


        }
        else
        {

            spell.GetComponent<CardMovementScr>().MoveToTarget(target.transform);

            while (SubSubCourutineIsRunning)
                yield return new WaitForSeconds(0.1f);
            spell.Info.ShowCardInfo();

            GameManagerScr.Instance.EnemyHandCards.Remove(spell);
            GameManagerScr.Instance.EnemyFieldCards.Add(spell);
            GameManagerScr.Instance.ReduceMana(false, spell.Card.ManaCost);

            spell.Card.IsPlaced = true;

            spell.UseSpell(target);

            //yield return new WaitForSeconds(.49f);
        }

        string targetStr = target == null ? "no_target" : target.Card.Title;
        UnityEngine.Debug.Log("AI spell cast: " + spell.Card.Title + "---> target: " + targetStr);
        SubCourutineIsRunning = false;
    }
}*/

/*public class GameState : ICloneable
{
    private static readonly System.Random random = new System.Random();

    public int AIHP, PlayerHP;
    public List<Card> AIFieldCards = new List<Card>();
    public List<Card> PlayerFieldCards = new List<Card>();
    public List<Card> AIHandCards = new List<Card>();
    public List<Card> PlayerHandCards = new List<Card>();
    public AllCards AIDeckCards;
    public AllCards PlayerDeckCards;

    public DecksManagerScr decksManager;

    Player Player, AI;

    public bool AITurn;
    public bool Win;

    public GameState(DecksManagerScr decksManager)
    {
        AITurn = !GameManagerScr.Instance.PlayersTurn;

        //decksManager = UnityEngine.Object.FindObjectOfType<DecksManagerScr>();
        this.decksManager = decksManager;
        Player = new Player();
        Player.HP = GameManagerScr.Instance.CurrentGame.Player.HP;
        Player.Mana = Player.Manapool = GameManagerScr.Instance.CurrentGame.Player.Manapool;

        AI = new Player();
        AI.HP = GameManagerScr.Instance.CurrentGame.Enemy.HP;
        AI.Mana = AI.Manapool = GameManagerScr.Instance.CurrentGame.Enemy.Manapool;

        AIHandCards = new List<Card>();
        PlayerHandCards = new List<Card>();
        AIFieldCards = new List<Card>();
        PlayerFieldCards = new List<Card>();

        AIDeckCards = new AllCards();
        PlayerDeckCards = new AllCards();

        AIFieldCards = DeepCopy(CardControllerToCards(GameManagerScr.Instance.EnemyFieldCards));
        PlayerFieldCards = DeepCopy(CardControllerToCards(GameManagerScr.Instance.PlayerFieldCards));
        AIHandCards = DeepCopy(CardControllerToCards(GameManagerScr.Instance.EnemyHandCards));
        PlayerHandCards = DeepCopy(CardControllerToCards(GameManagerScr.Instance.PlayerHandCards));
        AIDeckCards.cards = DeepCopy(GameManagerScr.Instance.decksManager.GetEnemyDeckCopy().cards);
        PlayerDeckCards.cards = DeepCopy(GameManagerScr.Instance.decksManager.GetMyDeckCopy().cards);

        int PlayerHandCount = PlayerHandCards.Count;

        PlayerDeckCards.cards.AddRange(PlayerHandCards);
        PlayerHandCards.Clear();

        PlayerDeckCards.cards = ShuffleDeck(PlayerDeckCards.cards);
        AIDeckCards.cards = ShuffleDeck(AIDeckCards.cards);

        for (int i = 0; i < PlayerHandCount; i++)
        {
            PlayerHandCards.Add(PlayerDeckCards.cards[0]);
            PlayerDeckCards.cards.RemoveAt(0);
        }

    }

    public object Clone()
    {
        GameState clone = new GameState(decksManager);

        clone.AIHP = this.AIHP;
        clone.PlayerHP = this.PlayerHP;

        clone.AIFieldCards = new List<Card>(this.AIFieldCards.Select(card => card.GetDeepCopy()));
        clone.PlayerFieldCards = new List<Card>(this.PlayerFieldCards.Select(card => card.GetDeepCopy()));
        clone.AIHandCards = new List<Card>(this.AIHandCards.Select(card => card.GetDeepCopy()));
        clone.PlayerHandCards = new List<Card>(this.PlayerHandCards.Select(card => card.GetDeepCopy()));

        clone.AIDeckCards = new AllCards
        {
            cards = new List<Card>(this.AIDeckCards.cards.Select(card => card.GetDeepCopy()))
        };

        clone.PlayerDeckCards = new AllCards
        {
            cards = new List<Card>(this.PlayerDeckCards.cards.Select(card => card.GetDeepCopy()))
        };

        clone.Player = new Player
        {
            HP = this.Player.HP,
            Mana = this.Player.Mana,
            Manapool = this.Player.Manapool
        };

        clone.AI = new Player
        {
            HP = this.AI.HP,
            Mana = this.AI.Mana,
            Manapool = this.AI.Manapool
        };

        clone.AITurn = this.AITurn;
        clone.Win = this.Win;

        clone.decksManager = this.decksManager;

        return clone;
    }

    double GetProfitability(bool IsAI)
    {
        double w1 = 0.5; // Вага здоров'я героїв
        double w2 = 0.35; // Вага карт на дошці
        double w3 = 0.15; // Вага карт в руках

        // Нормалізація
        double normalizedHealth = IsAI ? (AI.HP - Player.HP) / 30.0 : (Player.HP - AI.HP) / 30.0;
        double normalizedFieldValue = (GetCardsTotalValue(IsAI ? AIFieldCards : PlayerFieldCards) -
                                       GetCardsTotalValue(IsAI ? PlayerFieldCards : AIFieldCards)) / 8;
        double normalizedHandValue = (GetCardsTotalValue(IsAI ? AIHandCards : PlayerHandCards) -
                                      GetCardsTotalValue(IsAI ? PlayerHandCards : AIHandCards)) / 8;

        return w1 * normalizedHealth + w2 * normalizedFieldValue + w3 * normalizedHandValue;
    }

    int GetCardsTotalValue (List<Card> cards)
    {
        int value = 0;
        foreach (Card card in cards)
        {
            value += card.GetValue();
        }
        return value;
    }



    List<Card> CardControllerToCards(List<CardController> List)
    {
        List<Card> NewList = new List<Card>();
        for (int i = 0; i < List.Count; i++)
        {
            NewList.Add(List[i].Card.GetDeepCopy());
        }
        return NewList;
    }

    List<Card> DeepCopy(List<Card> source)
    {
        List<Card> list = new List<Card>();
        for (int i = 0; i < source.Count; i++)
        {
            list.Add(source[i].GetDeepCopy());
        }
        return list;
    }

    List<Card> ShuffleDeck(List<Card> Deck)
    {
        Card temp;
        System.Random random = new System.Random();
        // Fisher–Yates shuffle
        for (int i = Deck.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);

            temp = Deck[i];
            Deck[i] = Deck[randomIndex];
            Deck[randomIndex] = temp;
        }
        return Deck;
    }

    public void SimulateGame(int turn)
    {
        while (true)
        {

            AITurn = !AITurn;
            if (AITurn)
            {
                if (turn != 0)
                    AI.IncreaseManapool(true);
                AI.RestoreRoundMana(true);
                foreach (Card card in AIFieldCards)
                {
                    card.CanAttack = true;
                    if (turn != 0 && card.Abilities.Exists(x => x == Card.AbilityType.REGENERATION_EACH_TURN))
                        card.HP += card.SpellValue;
                    if (turn != 0 && card.Abilities.Exists(x => x == Card.AbilityType.INCREASE_ATTACK_EACH_TURN))
                        card.Attack += card.SpellValue;
                    if (turn != 0 && card.Abilities.Exists(x => x == Card.AbilityType.ADDITIONAL_MANA_EACH_TURN))
                        AI.Mana += card.SpellValue;
                }
            }
            else
            {
                if (turn != 0)
                    Player.IncreaseManapool(true);
                Player.RestoreRoundMana(true);
                foreach (Card card in PlayerFieldCards)
                {
                    card.CanAttack = true;

                    if (turn != 0 && card.Abilities.Exists(x => x == Card.AbilityType.REGENERATION_EACH_TURN))
                        card.HP += card.SpellValue;
                    if (turn != 0 && card.Abilities.Exists(x => x == Card.AbilityType.INCREASE_ATTACK_EACH_TURN))
                        card.Attack += card.SpellValue;
                    if (turn != 0 && card.Abilities.Exists(x => x == Card.AbilityType.ADDITIONAL_MANA_EACH_TURN))
                        Player.Mana += card.SpellValue;
                }
            }
            if (turn != 0)
                CastCards(AITurn);
            if (CheckForVictory())
                break;
            UseCards(AITurn);
            if (CheckForVictory())
                break;
            turn++;
        }
        Win = ReturnResult();
    }

    int FindBestCardToCast(List<Card> availableCards, bool IsAI)
    {
        double bestProfitability = double.MinValue;
        int bestCardIndex = -1;

        for (int i = 0; i < availableCards.Count; i++)
        {
            GameState newGS = (GameState)this.Clone();
            if (availableCards[i].IsSpell)
            {
                if (availableCards[i].SpellTarget == Card.TargetType.NO_TARGET ||
                   (availableCards[i].SpellTarget == Card.TargetType.ALLY_CARD_TARGET && PlayerFieldCards.Count > 0) ||
                   (availableCards[i].SpellTarget == Card.TargetType.ENEMY_CARD_TARGET && AIFieldCards.Count > 0))
                {
                    CastSpell(availableCards[i], false);
                }
            }
            else
                newGS.CastCard(availableCards[i], IsAI);

            double profitability = newGS.GetProfitability(IsAI);

            if (profitability > bestProfitability)
            {
                bestProfitability = profitability;
                bestCardIndex = i;
            }
        }

        return bestCardIndex;
    }


    public void CastCards(bool AITurn)
    {
        if (AITurn)
        {
            GiveCardToHand(AIDeckCards.cards, AIHandCards, true);
            while (AIFieldCards.Count <= 5 && AI.Mana > 0 && AIHandCards.Count > 0)
            {
                List<Card> availableCards = AIHandCards.FindAll(x => AI.Mana >= x.ManaCost);

                if (availableCards.Count == 0)
                    break;

                int chosenIndex = FindBestCardToCast(availableCards, true);
                //int chosenIndex = UnityEngine.Random.Range(0, availableCards.Count);

                if (chosenIndex == -1)
                    break;

                Card chosenCard = availableCards[chosenIndex];
                AI.Mana -= chosenCard.ManaCost;

                if (chosenCard.IsSpell)
                {
                    if (chosenCard.SpellTarget == Card.TargetType.NO_TARGET ||
                       (chosenCard.SpellTarget == Card.TargetType.ALLY_CARD_TARGET && AIFieldCards.Count > 0) ||
                       (chosenCard.SpellTarget == Card.TargetType.ENEMY_CARD_TARGET && PlayerFieldCards.Count > 0))
                    {
                        CastSpell(chosenCard, true);
                    }
                }
                else
                {
                    CastCard(chosenCard, true);
                }

                AIHandCards.Remove(chosenCard);
            }
        }
        else
        {
            GiveCardToHand(PlayerDeckCards.cards, PlayerHandCards, false);
            while (PlayerFieldCards.Count <= 5 && Player.Mana > 0 && PlayerHandCards.Count > 0)
            {
                List<Card> availableCards = PlayerHandCards.FindAll(x => Player.Mana >= x.ManaCost);

                if (availableCards.Count == 0)
                    break;

                int chosenIndex = FindBestCardToCast(availableCards, false);
                //int chosenIndex = UnityEngine.Random.Range(0, availableCards.Count);

                if (chosenIndex == -1)
                    break;

                Card chosenCard = availableCards[chosenIndex];
                Player.Mana -= chosenCard.ManaCost;

                if (chosenCard.IsSpell)
                {
                    if (chosenCard.SpellTarget == Card.TargetType.NO_TARGET ||
                       (chosenCard.SpellTarget == Card.TargetType.ALLY_CARD_TARGET && PlayerFieldCards.Count > 0) ||
                       (chosenCard.SpellTarget == Card.TargetType.ENEMY_CARD_TARGET && AIFieldCards.Count > 0))
                    {
                        CastSpell(chosenCard, false);
                    }
                }
                else
                {
                    CastCard(chosenCard, false);
                }

                PlayerHandCards.Remove(chosenCard);
            }
        }
    }


    public void CastSpellOnTarget(Card spell, Card target)
    {
        AI.Mana -= spell.ManaCost;
        if (spell.SpellTarget == Card.TargetType.ALLY_CARD_TARGET)
        {
            switch (spell.Spell)
            {
                case Card.SpellType.HEAL_ALLY_CARD:
                    target.HP += spell.SpellValue;
                    break;

                case Card.SpellType.SHIELD_ON_ALLY_CARD:
                    target.Abilities.Add(Card.AbilityType.SHIELD);
                    break;

                case Card.SpellType.PROVOCATION_ON_ALLY_CARD:
                    target.Abilities.Add(Card.AbilityType.PROVOCATION);
                    break;

                case Card.SpellType.BUFF_CARD_DAMAGE:
                    target.Attack += spell.SpellValue;
                    break;
            }
        }
        else if (spell.SpellTarget == Card.TargetType.ENEMY_CARD_TARGET)
        {
            switch (spell.Spell)
            {
                case Card.SpellType.DEBUFF_CARD_DAMAGE:
                    target.Attack -= spell.SpellValue;
                    break;

                case Card.SpellType.SILENCE:
                    target.Abilities.Clear();
                    target.Abilities.Add(AbilityType.NO_ABILITY);
                    break;
            }
        }
        DestroyCard(spell);
    }

    int FindBestTargetToAttack(int AttackerIndex, bool IsAI)
    {
        double bestProfitability = double.MinValue, profitability;
        int bestCardIndex = -1;

        if (IsAI && (AttackerIndex < 0 || AttackerIndex >= AIFieldCards.Count))
            throw new ArgumentOutOfRangeException(nameof(AttackerIndex), "AttackerIndex is out of range for AIFieldCards.");
        if (!IsAI && (AttackerIndex < 0 || AttackerIndex >= PlayerFieldCards.Count))
            throw new ArgumentOutOfRangeException(nameof(AttackerIndex), "AttackerIndex is out of range for PlayerFieldCards.");

        int availableTargetsNum = IsAI ? PlayerFieldCards.Count : AIFieldCards.Count;

        if (availableTargetsNum == 0)
            return -1;

        for (int i = 0; i < availableTargetsNum; i++)
        {
            GameState newGS = (GameState)this.Clone();
            if (IsAI)
                newGS.CardsFight(newGS.AIFieldCards[AttackerIndex], newGS.PlayerFieldCards[i]);
            else
                newGS.CardsFight(newGS.PlayerFieldCards[AttackerIndex], newGS.AIFieldCards[i]);

            profitability = newGS.GetProfitability(IsAI);

            if (profitability > bestProfitability)
            {
                bestProfitability = profitability;
                bestCardIndex = i;
            }
        }
        GameState clonedGS = (GameState)this.Clone();
        if (IsAI)
            clonedGS.DamageHero(true, clonedGS.AIFieldCards[AttackerIndex]);
        else
            clonedGS.DamageHero(false, clonedGS.PlayerFieldCards[AttackerIndex]);

        profitability = clonedGS.GetProfitability(IsAI);

        if (profitability > bestProfitability)
        {
            bestProfitability = profitability;
            bestCardIndex = 100;
        }


        return bestCardIndex;
    }

    public void UseCards(bool AITurn)
    {
        List<Card> Attackers = AITurn
            ? AIFieldCards.FindAll(x => x.CanAttack)
            : PlayerFieldCards.FindAll(x => x.CanAttack);

        List<Card> Defenders = AITurn ? PlayerFieldCards : AIFieldCards;

        foreach (Card card in Attackers)
            card.TimesDealedDamage = 0;

        for (int AttackerIndex = 0; AttackerIndex < Attackers.Count;)
        {
            int DefenderIndex = FindBestTargetToAttack(AttackerIndex, AITurn);

            if (Defenders.Any(x => x.IsProvocation))
            {
                DefenderIndex = Defenders.FindIndex(x => x.IsProvocation);
            }

            if ((DefenderIndex == 100 && !FieldHasProvocation(Defenders)) || Defenders.Count == 0)
            {
                DamageHero(AITurn, Attackers[AttackerIndex]);
                Attackers[AttackerIndex].TimesDealedDamage++;

                if (CheckForVictory())
                    return;
            }
            else
            {
                CardsFight(Attackers[AttackerIndex], Defenders[DefenderIndex]);
                Attackers[AttackerIndex].TimesDealedDamage++;
            }

            if (Attackers[AttackerIndex].Abilities.Exists(x => x == AbilityType.DOUBLE_ATTACK) &&
                Attackers[AttackerIndex].TimesDealedDamage < 2)
            {
                continue;
            }
        }
    }

    public void DamageHero(bool AITurn, Card card)
    {

        if (AITurn)
            Player.HP -= card.Attack;
        else
            AI.HP -= card.Attack;
        card.CanAttack = false;
    }


    public void CardsFight(Card attacker, Card defender)
    {
        defender.GetDamage(attacker.Attack);
        attacker.GetDamage(defender.Attack);


        if (attacker.Abilities.Exists(x => x == AbilityType.EXHAUSTION))
        {
            attacker.Attack += attacker.SpellValue;
            defender.Attack -= attacker.SpellValue;
        }
        if (attacker.Abilities.Exists(x => x == AbilityType.HORDE))
        {
            attacker.Attack = attacker.HP;
        }
        if (defender.Abilities.Exists(x => x == AbilityType.HORDE))
        {
            defender.Attack = defender.HP;
        }

        attacker.CanAttack = false;

        CheckForAlive(defender);
        CheckForAlive(attacker);
    }

    bool FieldHasProvocation(List<Card> FieldCards)
    {
        for (int i = 0; i < FieldCards.Count; i++)
        {
            if (FieldCards[i].IsProvocation)
                return true;
        }
        return false;
    }

    void GiveCardToHand(List<Card> deck, List<Card> hand, bool AI)
    {
        if ((AI && AIHandCards.Count >= 8) || (!AI && PlayerHandCards.Count >= 8))
            return;
        if (deck.Count == 0)
            deck = RenewDeck(AI);

        hand.Add(deck[0]);
        deck.RemoveAt(0);

    }

    public List<Card> RenewDeck(bool AI)
    {
        if (AI)
        {
            AIDeckCards.cards = new List<Card>(GameManagerScr.Instance.decksManager.GetEnemyDeckCopy().cards);
            AIDeckCards.cards = ShuffleDeck(AIDeckCards.cards);
            return AIDeckCards.cards;
        }
        else
        {
            PlayerDeckCards.cards = new List<Card>(GameManagerScr.Instance.decksManager.GetMyDeckCopy().cards);
            PlayerDeckCards.cards = ShuffleDeck(PlayerDeckCards.cards);
            return PlayerDeckCards.cards;
        }
    }


    void CastCard(Card card, bool AITurn)
    {
        if (AITurn)
        {
            foreach (Card fieldcard in AIFieldCards)
            {
                if (fieldcard.Abilities.Exists(x => x == Card.AbilityType.ALLIES_INSPIRATION))
                {
                    card.Attack += fieldcard.SpellValue;
                }
            }
            AIFieldCards.Add(card);
            AIHandCards.Remove(card);

        }
        else
        {
            foreach (Card fieldcard in PlayerFieldCards)
            {
                if (fieldcard.Abilities.Exists(x => x == Card.AbilityType.ALLIES_INSPIRATION))
                {
                    card.Attack += fieldcard.SpellValue;
                }
            }
            PlayerFieldCards.Add(card);
            PlayerHandCards.Remove(card);
        }

        if (card.HasAbility)
        {
            foreach (var ability in card.Abilities)
            {
                switch (ability)
                {
                    case Card.AbilityType.LEAP:
                        card.CanAttack = true;
                        break;

                    case Card.AbilityType.ALLIES_INSPIRATION:
                        if (AITurn)
                        {
                            foreach (var fieldcard in AIFieldCards)
                            {
                                if (fieldcard.id != card.id)
                                {
                                    fieldcard.Attack += card.SpellValue;
                                }
                            }
                        }
                        else
                        {
                            foreach (var fieldcard in PlayerFieldCards)
                            {
                                if (fieldcard.id != card.id)
                                {
                                    fieldcard.Attack += card.SpellValue;
                                }
                            }
                        }

                        break;
                }
            }
        }
    }

    void CastSpell(Card card, bool AITurn)
    {
        int targetIndex = 0;
        if (card.SpellTarget == Card.TargetType.ALLY_CARD_TARGET && AITurn)
            targetIndex = GetRandomIndex(0, AIFieldCards.Count);
        else if (card.SpellTarget == Card.TargetType.ALLY_CARD_TARGET && !AITurn)
            targetIndex = GetRandomIndex(0, PlayerFieldCards.Count);
        else if (card.SpellTarget == Card.TargetType.ENEMY_CARD_TARGET && AITurn)
            targetIndex = GetRandomIndex(0, PlayerFieldCards.Count);
        else if (card.SpellTarget == Card.TargetType.ENEMY_CARD_TARGET && !AITurn)
            targetIndex = GetRandomIndex(0, AIFieldCards.Count);
        switch (card.Spell)
        {
            case Card.SpellType.HEAL_ALLY_FIELD_CARDS:
                var allyCards = AITurn ?
                                 new List<Card>(AIFieldCards) :
                                 new List<Card>(PlayerFieldCards);
                foreach (Card fieldcard in allyCards)
                    fieldcard.HP += card.SpellValue;
                break;

            case Card.SpellType.DAMAGE_ENEMY_FIELD_CARDS:
                var enemyCards = AITurn ?
                                 new List<Card>(PlayerFieldCards) :
                                 new List<Card>(AIFieldCards);
                foreach (Card fieldcard in enemyCards)
                    GiveDamageTo(fieldcard, card.SpellValue);
                break;
            case Card.SpellType.HEAL_ALLY_HERO:
                if (AITurn)
                    AI.HP += card.SpellValue;
                else
                    Player.HP += card.SpellValue;
                break;
            case Card.SpellType.DAMAGE_ENEMY_HERO:
                if (AITurn)
                    Player.HP -= card.SpellValue;
                else
                    AI.HP -= card.SpellValue;
                break;

            case Card.SpellType.HEAL_ALLY_CARD:
                if (AITurn)
                    AIFieldCards[targetIndex].HP += card.SpellValue;
                else
                    PlayerFieldCards[targetIndex].HP += card.SpellValue;
                break;

            case Card.SpellType.SHIELD_ON_ALLY_CARD:
                if (AITurn)
                {
                    if (!AIFieldCards[targetIndex].Abilities.Exists(x => x == Card.AbilityType.SHIELD))
                        AIFieldCards[targetIndex].Abilities.Add(Card.AbilityType.SHIELD);
                }
                else
                {
                    if (!PlayerFieldCards[targetIndex].Abilities.Exists(x => x == Card.AbilityType.SHIELD))
                        PlayerFieldCards[targetIndex].Abilities.Add(Card.AbilityType.SHIELD);
                }
                break;

            case Card.SpellType.PROVOCATION_ON_ALLY_CARD:
                if (AITurn)
                {
                    if (!AIFieldCards[targetIndex].Abilities.Exists(x => x == Card.AbilityType.PROVOCATION))
                        AIFieldCards[targetIndex].Abilities.Add(Card.AbilityType.PROVOCATION);
                }
                else
                {
                    if (!PlayerFieldCards[targetIndex].Abilities.Exists(x => x == Card.AbilityType.PROVOCATION))
                        PlayerFieldCards[targetIndex].Abilities.Add(Card.AbilityType.PROVOCATION);
                }
                break;

            case Card.SpellType.BUFF_CARD_DAMAGE:
                if (AITurn)
                {
                    AIFieldCards[targetIndex].Attack += card.SpellValue;
                }
                else
                {
                    PlayerFieldCards[targetIndex].Attack += card.SpellValue;
                }
                break;

            case Card.SpellType.DEBUFF_CARD_DAMAGE:

                if (AITurn)
                {
                    PlayerFieldCards[targetIndex].Attack = Mathf.Clamp(PlayerFieldCards[targetIndex].Attack - card.SpellValue, 0, int.MaxValue);
                }
                else
                {
                    AIFieldCards[targetIndex].Attack = Mathf.Clamp(AIFieldCards[targetIndex].Attack - card.SpellValue, 0, int.MaxValue);
                }
                break;

            case Card.SpellType.SILENCE:
                if (AITurn)
                {
                    PlayerFieldCards[targetIndex].Abilities.Clear();
                    PlayerFieldCards[targetIndex].Abilities.Add(AbilityType.NO_ABILITY);
                }
                else
                {
                    AIFieldCards[targetIndex].Abilities.Clear();
                    AIFieldCards[targetIndex].Abilities.Add(AbilityType.NO_ABILITY);
                }
                break;

            case Card.SpellType.KILL_ALL:
                while (AIFieldCards.Count != 0)
                    DestroyCard(AIFieldCards[0]);
                while (PlayerFieldCards.Count != 0)
                    DestroyCard(PlayerFieldCards[0]);
                break;
        }

        DestroyCard(card);
    }

    void GiveDamageTo(Card card, int damage)
    {
        card.GetDamage(damage);
        CheckForAlive(card);
    }

    void CheckForAlive(Card card)
    {
        if (!card.IsAlive())
        {
            DestroyCard(card);
        }
    }

    void DestroyCard(Card card)
    {
        RemoveCardFromList(card, AIHandCards);
        RemoveCardFromList(card, AIFieldCards);
        RemoveCardFromList(card, PlayerHandCards);
        RemoveCardFromList(card, PlayerFieldCards);
    }

    void RemoveCardFromList(Card card, List<Card> list)
    {
        if (list.Exists(x => x == card))
            list.Remove(card);
    }

    public bool CheckForVictory()
    {
        if (Player.HP <= 0 || AI.HP <= 0)
            return true;
        return false;
    }

    public bool ReturnResult()
    {
        if (Player.HP <= 0)
            return true;
        else
            return false;
    }

    int GetRandomIndex(int minInclusive, int maxExclusive)
    {
        lock (random)
        {
            return random.Next(minInclusive, maxExclusive);
        }
    }

}*/
