using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public interface ISimPlayerModel
{
    void MakeMove(GameState gameState);
    string Name { get; }
}

public class GameState
{
    private static readonly System.Random random = new System.Random();

    const int MAX_CARDS_ON_FIELD = 6;
    const int MAX_CARDS_ON_HAND = 8;
    const int STARTER_NUMBER_OF_CARDS_ON_HAND = 4;

    public SimPlayer Player1, Player2;
    public bool Player1Turn;
    public bool Player1Win;
    public int Turn;

    public bool LOGS = false;

    public GameState(SimPlayer Player1, SimPlayer Player2, bool Player1Turn, int Turn)
    {
        this.Player1 = Player1;
        Player1.DeckCards.Shuffle();
        this.Player2 = Player2;
        Player2.DeckCards.Shuffle();
        this.Player1Turn = Player1Turn;
        this.Turn = Turn;
    }

    public GameState() { }

    public GameState GetDeepCopy()
    {
        var clone = new GameState();

        clone.Player1 = this.Player1.GetDeepCopy();
        clone.Player1.DeckCards.Shuffle();
        clone.Player2 = this.Player2.GetDeepCopy();
        clone.Player2.DeckCards.Shuffle();

        clone.Player1Turn = this.Player1Turn;
        clone.Player1Win = this.Player1Win;
        clone.Turn = this.Turn;

        return clone;
    }

    public void SimulateGame(ISimPlayerModel player1Model, ISimPlayerModel player2Model)
    {
        if (Turn == 0)
        {
            Player1.GiveCards(STARTER_NUMBER_OF_CARDS_ON_HAND, MAX_CARDS_ON_HAND);
            Player2.GiveCards(STARTER_NUMBER_OF_CARDS_ON_HAND, MAX_CARDS_ON_HAND);
        }

        while (!CheckForVictory())
        {
            if (Turn >= 100)
                break;
            StartTurn();

            if (Player1Turn)
            {
                player1Model.MakeMove(this);
            }
            else
            {
                player2Model.MakeMove(this);
            }

            if (CheckForVictory())
                break;
            EndTurn();
        }
        if (LOGS)
            UnityEngine.Debug.Log(Player1Win);
    }

    private void StartTurn()
    {
        Turn++;
        SimPlayer current = Player1Turn ? Player1 : Player2;
        if (Player1.FieldCards.Contains(null) || Player2.FieldCards.Contains(null))
            RemoveDeadCards();

        current.IncreaseManapool();
        current.RestoreRoundMana();

        current.GiveCards(1, MAX_CARDS_ON_HAND);

        foreach (var card in current.FieldCards)
        {
            card.CanAttack = true;
            if (card.Abilities.Contains(Card.AbilityType.REGENERATION_EACH_TURN))
                card.HP += card.SpellValue;
            if (card.Abilities.Contains(Card.AbilityType.INCREASE_ATTACK_EACH_TURN))
                card.Attack += card.SpellValue;
            if (card.Abilities.Contains(Card.AbilityType.ADDITIONAL_MANA_EACH_TURN))
                current.Mana += card.SpellValue;
        }

        if (LOGS)
        {
            UnityEngine.Debug.Log($"\n===== Turn {Turn} | ({(Player1Turn ? "Player1" : "Player2")}) =====");
        }

    }

    public void EndTurn()
    {
        if (Player1.FieldCards.Contains(null) || Player2.FieldCards.Contains(null))
            RemoveDeadCards();
        Player1Turn = !Player1Turn;
    }

    public bool CheckForVictory()
    {
        if (Player1.HP <= 0)
        {
            Player1Win = false;
            return true;
        }
        if (Player2.HP <= 0)
        {
            Player1Win = true;
            return true;
        }
        return false;
    }

    public bool TryPlayCard(Card card, Card target = null)
    {
        if (Player1.FieldCards.Contains(null) || Player2.FieldCards.Contains(null))
            RemoveDeadCards();

        SimPlayer currentPlayer = Player1Turn ? Player1 : Player2;
        List<Card> currentField = Player1Turn ? Player1.FieldCards : Player2.FieldCards;
        List<Card> currentHand = Player1Turn ? Player1.HandCards : Player2.HandCards;

        SimPlayer enemyPlayer = Player1Turn ? Player2 : Player1;
        List<Card> enemyField = Player1Turn ? Player2.FieldCards : Player1.FieldCards;

        /*UnityEngine.Debug.Log("=== Current Hand ===");
        foreach (var c in currentHand)
        {
            UnityEngine.Debug.Log($"Card in hand: {c.Title} [ID: {c.InstanceId}]");
        }

        UnityEngine.Debug.Log($"Card to play: {card.Title} [ID: {card.InstanceId}]");*/


        if (!currentHand.Any(c => c.InstanceId == card.InstanceId))
        {
            //UnityEngine.Debug.Log("NOT WORKS");
            return false;
        }
        //UnityEngine.Debug.Log("WORKS");
        card = currentHand.FirstOrDefault(c => c.InstanceId == card.InstanceId);

        if (card == null)
        {
            //UnityEngine.Debug.LogWarning("[TryPlayCard] Card with matching InstanceId not found in hand.");
            return false;
        }

        if (currentPlayer.Mana < card.ManaCost)
        {
            return false;
        }

        // Entity
        if (!card.IsSpell)
        {
            if (currentField.Count >= MAX_CARDS_ON_FIELD)
            {
                return false;
            }

            currentPlayer.Mana -= card.ManaCost;
            currentHand.Remove(card);
            currentField.Add(card);

            ApplySummonAbilities(card, currentField);

            if (LOGS)
                UnityEngine.Debug.Log($"[Model] Casting card: {card.Title}");
            /*UnityEngine.Debug.Log("=== Current Hand ===");
            foreach (var c in currentHand)
            {
                UnityEngine.Debug.Log($"Card in hand: {c.Title} [ID: {c.InstanceId}]");
            }
            UnityEngine.Debug.Log("=== Current Field ===");
            foreach (var c in currentField)
            {
                UnityEngine.Debug.Log($"Card in Field: {c.Title} [ID: {c.InstanceId}]");
            }*/

            return true;
        }
        // Spell
        else
        {
            bool hasTarget = false;
            if (card.SpellTarget == Card.TargetType.NO_TARGET)
                hasTarget = true;
            else if (card.SpellTarget == Card.TargetType.ALLY_CARD_TARGET)
                hasTarget = currentField.Count > 0;
            else if (card.SpellTarget == Card.TargetType.ENEMY_CARD_TARGET)
                hasTarget = enemyField.Count > 0;

            if (!hasTarget)
            {
                return false;
            }

            currentPlayer.Mana -= card.ManaCost;
            currentHand.Remove(card);
            if (card.SpellTarget == Card.TargetType.NO_TARGET)
            {
                ApplyNonTargetSpellEffect(card, currentPlayer, enemyPlayer);
            }
            else
            {
                if (target == null)
                {
                    return false;
                }
                ApplyTargetSpellEffect(card, target, currentPlayer, enemyPlayer);
            }
            if (LOGS)
                UnityEngine.Debug.Log($"[Model] Playing card: {card.Title} → Target: {(target != null ? target.Title : "none")}");

            return true;
        }
    }

    public bool TryAttackCard(Card attacker, Card target)
    {
        if (Player1.FieldCards.Contains(null) || Player2.FieldCards.Contains(null))
            RemoveDeadCards();
        SimPlayer currentPlayer = Player1Turn ? Player1 : Player2;
        List<Card> currentField = Player1Turn ? Player1.FieldCards : Player2.FieldCards;
        SimPlayer enemyPlayer = Player1Turn ? Player2 : Player1;
        List<Card> enemyField = Player1Turn ? Player2.FieldCards : Player1.FieldCards;

        if (!currentField.Contains(attacker))
            return false;

        if (!enemyField.Contains(target))
            return false;

        if (enemyField.Exists(c => c.Abilities.Contains(Card.AbilityType.PROVOCATION) &&
                              !target.Abilities.Contains(Card.AbilityType.PROVOCATION)))

            if (!attacker.CanAttack)
                return false;

        // Fight

        if (!attacker.Abilities.Contains(Card.AbilityType.SHIELD))
            DealDamage(attacker, target.Attack);
        else
            attacker.Abilities.Remove(Card.AbilityType.SHIELD);

        if (!target.Abilities.Contains(Card.AbilityType.SHIELD))
            DealDamage(target, attacker.Attack);
        else
            target.Abilities.Remove(Card.AbilityType.SHIELD);

        if (attacker.Abilities.Contains(Card.AbilityType.EXHAUSTION))
            DecreaseAttack(target, attacker.SpellValue);

        attacker.CanAttack = false;

        return true;
    }

    public bool TryAttackHero(Card attacker)
    {
        SimPlayer currentPlayer = Player1Turn ? Player1 : Player2;
        List<Card> currentField = Player1Turn ? Player1.FieldCards : Player2.FieldCards;
        SimPlayer enemyPlayer = Player1Turn ? Player2 : Player1;
        List<Card> enemyField = Player1Turn ? Player2.FieldCards : Player1.FieldCards;

        if (!currentField.Contains(attacker))
            return false;

        if (!attacker.CanAttack)
            return false;

        if (enemyField.Exists(c => c.Abilities.Contains(Card.AbilityType.PROVOCATION)))
            return false;

        enemyPlayer.HP -= attacker.Attack;
        attacker.CanAttack = false;

        return true;
    }


    private void ApplySummonAbilities(Card card, List<Card> field)
    {
        if (card.Abilities.Contains(Card.AbilityType.LEAP))
            card.CanAttack = true;

        if (card.Abilities.Contains(Card.AbilityType.ALLIES_INSPIRATION))
        {
            foreach (var ally in field)
            {
                if (ally != card)
                    ally.Attack += card.SpellValue;
            }
        }
    }

    private void ApplyNonTargetSpellEffect(Card spellCard, SimPlayer currentPlayer, SimPlayer enemyPlayer)
    {
        switch (spellCard.Spell)
        {
            case Card.SpellType.HEAL_ALLY_FIELD_CARDS:
                foreach (var ally in currentPlayer.FieldCards.ToList())
                    IncreaseHP(ally, spellCard.SpellValue);
                break;

            case Card.SpellType.DAMAGE_ENEMY_FIELD_CARDS:
                foreach (var enemy in enemyPlayer.FieldCards.ToList())
                    DealDamage(enemy, spellCard.SpellValue);
                break;

            case Card.SpellType.HEAL_ALLY_HERO:
                currentPlayer.HP += spellCard.SpellValue;
                break;

            case Card.SpellType.DAMAGE_ENEMY_HERO:
                enemyPlayer.HP -= spellCard.SpellValue;
                break;

            case Card.SpellType.KILL_ALL:
                currentPlayer.FieldCards.Clear();
                enemyPlayer.FieldCards.Clear();
                break;
        }
    }

    private void ApplyTargetSpellEffect(Card spellCard, Card targetCard, SimPlayer currentPlayer, SimPlayer enemyPlayer)
    {
        switch (spellCard.Spell)
        {
            case Card.SpellType.HEAL_ALLY_CARD:
                IncreaseHP(targetCard, spellCard.SpellValue);
                break;

            case Card.SpellType.SHIELD_ON_ALLY_CARD:
                if (!targetCard.Abilities.Contains(Card.AbilityType.SHIELD))
                    targetCard.Abilities.Add(Card.AbilityType.SHIELD);
                break;

            case Card.SpellType.PROVOCATION_ON_ALLY_CARD:
                if (!targetCard.Abilities.Contains(Card.AbilityType.PROVOCATION))
                    targetCard.Abilities.Add(Card.AbilityType.PROVOCATION);
                break;

            case Card.SpellType.BUFF_CARD_DAMAGE:
                IncreaseAttack(targetCard, spellCard.SpellValue);
                break;

            case Card.SpellType.DEBUFF_CARD_DAMAGE:
                DecreaseAttack(targetCard, spellCard.SpellValue);
                break;

            case Card.SpellType.SILENCE:
                targetCard.Abilities.Clear();
                targetCard.Abilities.Add(Card.AbilityType.NO_ABILITY);
                break;
        }
    }

    private void IncreaseHP(Card card, int value)
    {
        card.HP += value;
        if (card.Abilities.Contains(Card.AbilityType.HORDE))
            card.Attack = card.HP;
    }

    private void IncreaseAttack(Card card, int value)
    {
        card.Attack += value;
        if (card.Abilities.Contains(Card.AbilityType.HORDE))
            card.HP = card.Attack;
    }

    private void DecreaseAttack(Card card, int value)
    {
        card.Attack = Math.Max(0, card.Attack - value);
        if (card.Abilities.Contains(Card.AbilityType.HORDE))
            card.HP = card.Attack;
        RemoveDeadCards();
    }

    private void DealDamage(Card card, int value)
    {
        card.HP -= value;
        if (card.Abilities.Contains(Card.AbilityType.HORDE))
            card.Attack = card.HP;
        RemoveDeadCards();
    }

    private void RemoveDeadCards()
    {
        Player1.FieldCards.RemoveAll(card => card == null || card.HP <= 0);
        Player2.FieldCards.RemoveAll(card => card == null || card.HP <= 0);
    }
}
