using System.Collections.Generic;

/// <summary>Catalog of debug/test scenarios that exercise specific engine rules.</summary>
public enum DebugScenarioType
{
    RealDeckStart,
    CoinTest,
    DealDamageEnemyCard,
    DealDamageEnemyHero,

    HealAllyCard,
    HealAllyHero,

    BuffSelectedAllyCard,
    AddKeywordSelectedAllyCard,
    DrawCardsOnPlay,
    SummonOnPlay,
    OnDeathSummon,
    AttackCard,
    AttackHero,

    AttackBlockedByProvocation,
    RushCannotAttackHero,
    RushCanAttackCard,
    ChargeCanAttackHero,
    CannotAttackTwice,

    BuffAttackSelectedAllyCard,
    BuffHealthSelectedAllyCard,
    DebuffAttackSelectedEnemyCard,

    DestroyEnemyCard,
    DestroyEnemyCardWithOnDeath,

    SilenceEnemyCard,
    SilenceEnemyCardRemovesOnDeath,

    ShieldBlocksSpellDamage,
    ShieldBlocksAttackDamage,

    LifestealAttackCard,
    LifestealAttackHero,
    LifestealBlockedByShield,

    DoubleAttackCanAttackTwice,
    DoubleAttackCannotAttackThirdTime,
    DoubleAttackRefreshesNextTurn,
}

/// <summary>
/// Creates controlled pure GameState scenarios for testing GameEngine logic.
/// This is not production gameplay code.
/// </summary>
public static class GameStateTestFactory
{
    public static GameState CreateDoubleAttackRefreshesNextTurnScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance attacker = CreateEntity(
            state,
            PlayerSide.Player,
            901604,
            "Debug Refreshed Double Attacker",
            2,
            5,
            5,
            4,
            GameZone.Field
        );

        attacker.Keywords.Add(KeywordType.DoubleAttack);
        attacker.CanAttack = false;
        attacker.CanAttackOnlyCardsThisTurn = false;
        attacker.RemainingAttacksThisTurn = 0;

        state.Player.Field.Add(attacker);

        // Make it the player's turn already started.
        state.ActiveSide = PlayerSide.Player;
        state.Player.TurnsStarted = 1;
        state.Enemy.TurnsStarted = 0;

        return state;
    }

    public static GameState CreateDoubleAttackCanAttackTwiceScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance attacker = CreateEntity(
            state,
            PlayerSide.Player,
            901601,
            "Debug Double Attacker",
            2,
            5,
            5,
            4,
            GameZone.Field
        );

        attacker.Keywords.Add(KeywordType.DoubleAttack);
        attacker.CanAttack = true;
        attacker.RemainingAttacksThisTurn = 2;

        CardInstance defenderA = CreateEntity(
            state,
            PlayerSide.Enemy,
            901602,
            "Debug First Target",
            1,
            5,
            5,
            2,
            GameZone.Field
        );

        CardInstance defenderB = CreateEntity(
            state,
            PlayerSide.Enemy,
            901603,
            "Debug Second Target",
            1,
            5,
            5,
            2,
            GameZone.Field
        );

        state.Player.Field.Add(attacker);
        state.Enemy.Field.Add(defenderA);
        state.Enemy.Field.Add(defenderB);

        return state;
    }

    public static GameState CreateLifestealBlockedByShieldScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        state.Player.HP = 24;

        CardInstance attacker = CreateEntity(
            state,
            PlayerSide.Player,
            901504,
            "Debug Shielded Drain Attacker",
            3,
            4,
            4,
            3,
            GameZone.Field
        );

        attacker.Keywords.Add(KeywordType.Lifesteal);
        attacker.CanAttack = true;
        attacker.RemainingAttacksThisTurn = 1;

        CardInstance defender = CreateEntity(
            state,
            PlayerSide.Enemy,
            901505,
            "Debug Shielded Drain Target",
            1,
            5,
            5,
            3,
            GameZone.Field
        );

        defender.Keywords.Add(KeywordType.Shield);

        state.Player.Field.Add(attacker);
        state.Enemy.Field.Add(defender);

        return state;
    }

    public static GameState CreateLifestealAttackHeroScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        state.Player.HP = 24;

        CardInstance attacker = CreateEntity(
            state,
            PlayerSide.Player,
            901503,
            "Debug Face Drainer",
            4,
            3,
            3,
            4,
            GameZone.Field
        );

        attacker.Keywords.Add(KeywordType.Lifesteal);
        attacker.CanAttack = true;
        attacker.RemainingAttacksThisTurn = 1;

        state.Player.Field.Add(attacker);

        return state;
    }

    public static GameState CreateLifestealAttackCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        state.Player.HP = 24;

        CardInstance attacker = CreateEntity(
            state,
            PlayerSide.Player,
            901501,
            "Debug Lifesteal Attacker",
            3,
            4,
            4,
            3,
            GameZone.Field
        );

        attacker.Keywords.Add(KeywordType.Lifesteal);
        attacker.CanAttack = true;
        attacker.RemainingAttacksThisTurn = 1;

        CardInstance defender = CreateEntity(
            state,
            PlayerSide.Enemy,
            901502,
            "Debug Drain Target",
            1,
            5,
            5,
            3,
            GameZone.Field
        );

        state.Player.Field.Add(attacker);
        state.Enemy.Field.Add(defender);

        return state;
    }

    public static GameState CreateShieldBlocksAttackDamageScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance attacker = CreateEntity(
            state,
            PlayerSide.Player,
            901403,
            "Debug Shield Attacker",
            3,
            3,
            3,
            3,
            GameZone.Field
        );

        attacker.CanAttack = true;
        attacker.RemainingAttacksThisTurn = 1;

        CardInstance shieldedDefender = CreateEntity(
            state,
            PlayerSide.Enemy,
            901404,
            "Debug Shielded Defender",
            2,
            4,
            4,
            3,
            GameZone.Field
        );

        shieldedDefender.Keywords.Add(KeywordType.Shield);

        state.Player.Field.Add(attacker);
        state.Enemy.Field.Add(shieldedDefender);

        return state;
    }

    public static GameState CreateShieldBlocksSpellDamageScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance damageSpell = CreateSpell(
            state,
            PlayerSide.Player,
            901401,
            "Debug Shield Breaker Spell",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.DealDamage,
                Target = EffectTarget.SelectedEnemyCard,
                Value = 3,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance shieldedEnemy = CreateEntity(
            state,
            PlayerSide.Enemy,
            901402,
            "Debug Shielded Enemy",
            2,
            4,
            4,
            3,
            GameZone.Field
        );

        shieldedEnemy.Keywords.Add(KeywordType.Shield);

        state.Player.Hand.Add(damageSpell);
        state.Enemy.Field.Add(shieldedEnemy);

        return state;
    }

    public static GameState CreateSilenceEnemyCardRemovesOnDeathScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance silenceSpell = CreateSpell(
            state,
            PlayerSide.Player,
            901303,
            "Debug First Silence",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.Silence,
                Target = EffectTarget.SelectedEnemyCard,
                Value = 0,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance destroySpell = CreateSpell(
            state,
            PlayerSide.Player,
            901304,
            "Debug Second Destroy",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.Destroy,
                Target = EffectTarget.SelectedEnemyCard,
                Value = 0,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance enemyEgg = CreateEntity(
            state,
            PlayerSide.Enemy,
            901305,
            "Debug Silent Egg",
            0,
            3,
            3,
            3,
            GameZone.Field
        );

        enemyEgg.Keywords.Add(KeywordType.Provocation);

        enemyEgg.Effects.Add(new CardEffect
        {
            Trigger = EffectTrigger.OnDeath,
            Type = EffectType.Summon,
            Target = EffectTarget.None,
            Value = 2,
            AttackValue = 0,
            HealthValue = 0,
            CardId = 901001,
            Keyword = KeywordType.None
        });

        state.Player.Hand.Add(silenceSpell);
        state.Player.Hand.Add(destroySpell);
        state.Enemy.Field.Add(enemyEgg);

        return state;
    }

    public static GameState CreateSilenceEnemyCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance silenceSpell = CreateSpell(
            state,
            PlayerSide.Player,
            901301,
            "Debug Mute Hex",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.Silence,
                Target = EffectTarget.SelectedEnemyCard,
                Value = 0,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance enemy = CreateEntity(
            state,
            PlayerSide.Enemy,
            901302,
            "Debug Loud Enemy",
            4,
            4,
            4,
            4,
            GameZone.Field
        );

        enemy.Keywords.Add(KeywordType.Provocation);
        enemy.Keywords.Add(KeywordType.Rush);

        enemy.Effects.Add(new CardEffect
        {
            Trigger = EffectTrigger.OnDeath,
            Type = EffectType.Summon,
            Target = EffectTarget.None,
            Value = 2,
            AttackValue = 0,
            HealthValue = 0,
            CardId = 901001,
            Keyword = KeywordType.None
        });

        state.Player.Hand.Add(silenceSpell);
        state.Enemy.Field.Add(enemy);

        return state;
    }

    public static GameState CreateDestroyEnemyCardWithOnDeathScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance destroySpell = CreateSpell(
            state,
            PlayerSide.Player,
            901203,
            "Debug Final Verdict",
            2,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.Destroy,
                Target = EffectTarget.SelectedEnemyCard,
                Value = 0,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance egg = CreateEntity(
            state,
            PlayerSide.Enemy,
            901204,
            "Debug Verdict Egg",
            0,
            3,
            3,
            3,
            GameZone.Field
        );

        egg.Effects.Add(new CardEffect
        {
            Trigger = EffectTrigger.OnDeath,
            Type = EffectType.Summon,
            Target = EffectTarget.None,
            Value = 2,
            AttackValue = 0,
            HealthValue = 0,
            CardId = 901001,
            Keyword = KeywordType.None
        });

        state.Player.Hand.Add(destroySpell);
        state.Enemy.Field.Add(egg);

        return state;
    }

    public static GameState CreateDestroyEnemyCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance destroySpell = CreateSpell(
            state,
            PlayerSide.Player,
            901201,
            "Debug Grave Sentence",
            2,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.Destroy,
                Target = EffectTarget.SelectedEnemyCard,
                Value = 0,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance enemy = CreateEntity(
            state,
            PlayerSide.Enemy,
            901202,
            "Debug Doomed Enemy",
            5,
            5,
            5,
            5,
            GameZone.Field
        );

        state.Player.Hand.Add(destroySpell);
        state.Enemy.Field.Add(enemy);

        return state;
    }

    public static GameState CreateDebuffAttackSelectedEnemyCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance spell = CreateSpell(
            state,
            PlayerSide.Player,
            901105,
            "Debug Rust Hex",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.DebuffAttack,
                Target = EffectTarget.SelectedEnemyCard,
                Value = 2,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance enemy = CreateEntity(
            state,
            PlayerSide.Enemy,
            901106,
            "Debug Angry Enemy",
            5,
            4,
            4,
            3,
            GameZone.Field
        );

        state.Player.Hand.Add(spell);
        state.Enemy.Field.Add(enemy);

        return state;
    }

    public static GameState CreateBuffHealthSelectedAllyCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance spell = CreateSpell(
            state,
            PlayerSide.Player,
            901103,
            "Debug Fortify Flesh",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.BuffHealth,
                Target = EffectTarget.SelectedAllyCard,
                Value = 2,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance ally = CreateEntity(
            state,
            PlayerSide.Player,
            901104,
            "Debug Fragile Ally",
            2,
            2,
            2,
            2,
            GameZone.Field
        );

        state.Player.Hand.Add(spell);
        state.Player.Field.Add(ally);

        return state;
    }

    public static GameState CreateBuffAttackSelectedAllyCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance spell = CreateSpell(
            state,
            PlayerSide.Player,
            901101,
            "Debug Sharpen Blade",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.BuffAttack,
                Target = EffectTarget.SelectedAllyCard,
                Value = 2,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance ally = CreateEntity(
            state,
            PlayerSide.Player,
            901102,
            "Debug Small Ally",
            1,
            3,
            3,
            2,
            GameZone.Field
        );

        state.Player.Hand.Add(spell);
        state.Player.Field.Add(ally);

        return state;
    }

    public static GameState CreateHealAllyHeroScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        state.Player.HP = 24;

        CardInstance healSpell = CreateSpell(
            state,
            PlayerSide.Player,
            900903,
            "Debug Prayer Light",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.Heal,
                Target = EffectTarget.SelectedAllyCharacter,
                Value = 4,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        state.Player.Hand.Add(healSpell);

        return state;
    }

    public static GameState CreateHealAllyCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance healSpell = CreateSpell(
            state,
            PlayerSide.Player,
            900901,
            "Debug Field Medic",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.Heal,
                Target = EffectTarget.SelectedAllyCard,
                Value = 2,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance damagedAlly = CreateEntity(
            state,
            PlayerSide.Player,
            900902,
            "Debug Wounded Ally",
            2,
            1,
            4,
            2,
            GameZone.Field
        );

        state.Player.Hand.Add(healSpell);
        state.Player.Field.Add(damagedAlly);

        return state;
    }

    public static GameState CreateCannotAttackTwiceScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance attacker = CreateEntity(
            state,
            PlayerSide.Player,
            900808,
            "Debug Tired Attacker",
            3,
            3,
            3,
            3,
            GameZone.Field
        );

        attacker.CanAttack = true;
        attacker.RemainingAttacksThisTurn = 1;

        CardInstance defender = CreateEntity(
            state,
            PlayerSide.Enemy,
            900809,
            "Debug First Defender",
            1,
            5,
            5,
            2,
            GameZone.Field
        );

        state.Player.Field.Add(attacker);
        state.Enemy.Field.Add(defender);

        return state;
    }

    public static GameState CreateChargeCanAttackHeroScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance chargeAttacker = CreateEntity(
            state,
            PlayerSide.Player,
            900807,
            "Debug Charge Attacker",
            4,
            2,
            2,
            4,
            GameZone.Field
        );

        chargeAttacker.Keywords.Add(KeywordType.Charge);
        chargeAttacker.CanAttack = true;
        chargeAttacker.RemainingAttacksThisTurn = 1;
        chargeAttacker.CanAttackOnlyCardsThisTurn = false;

        state.Player.Field.Add(chargeAttacker);

        return state;
    }

    public static GameState CreateRushCanAttackCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance rushAttacker = CreateEntity(
            state,
            PlayerSide.Player,
            900805,
            "Debug Rush Attacker",
            3,
            2,
            2,
            3,
            GameZone.Field
        );

        rushAttacker.Keywords.Add(KeywordType.Rush);
        rushAttacker.CanAttack = true;
        rushAttacker.RemainingAttacksThisTurn = 1;
        rushAttacker.CanAttackOnlyCardsThisTurn = true;

        CardInstance defender = CreateEntity(
            state,
            PlayerSide.Enemy,
            900806,
            "Debug Enemy Target",
            1,
            3,
            3,
            2,
            GameZone.Field
        );

        state.Player.Field.Add(rushAttacker);
        state.Enemy.Field.Add(defender);

        return state;
    }

    public static GameState CreateRushCannotAttackHeroScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance rushAttacker = CreateEntity(
            state,
            PlayerSide.Player,
            900804,
            "Debug Rush Attacker",
            3,
            2,
            2,
            3,
            GameZone.Field
        );

        rushAttacker.Keywords.Add(KeywordType.Rush);
        rushAttacker.CanAttack = true;
        rushAttacker.RemainingAttacksThisTurn = 1;
        rushAttacker.CanAttackOnlyCardsThisTurn = true;

        state.Player.Field.Add(rushAttacker);

        return state;
    }

    public static GameState CreateAttackBlockedByProvocationScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance attacker = CreateEntity(
            state,
            PlayerSide.Player,
            900801,
            "Debug Attacker",
            3,
            3,
            3,
            2,
            GameZone.Field
        );

        attacker.CanAttack = true;
        attacker.RemainingAttacksThisTurn = 1;

        CardInstance normalDefender = CreateEntity(
            state,
            PlayerSide.Enemy,
            900802,
            "Debug Normal Defender",
            1,
            3,
            3,
            2,
            GameZone.Field
        );

        CardInstance provocationDefender = CreateEntity(
            state,
            PlayerSide.Enemy,
            900803,
            "Debug Provocation Defender",
            1,
            4,
            4,
            3,
            GameZone.Field
        );

        provocationDefender.Keywords.Add(KeywordType.Provocation);

        state.Player.Field.Add(attacker);
        state.Enemy.Field.Add(normalDefender);
        state.Enemy.Field.Add(provocationDefender);

        return state;
    }
    public static GameState CreateAttackCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance attacker = CreateEntity(
            state,
            PlayerSide.Player,
            900701,
            "Debug Attacker",
            2,
            3,
            3,
            2,
            GameZone.Field
        );

        attacker.CanAttack = true;
        attacker.RemainingAttacksThisTurn = 1;

        CardInstance defender = CreateEntity(
            state,
            PlayerSide.Enemy,
            900702,
            "Debug Defender",
            1,
            2,
            2,
            2,
            GameZone.Field
        );

        state.Player.Field.Add(attacker);
        state.Enemy.Field.Add(defender);

        return state;
    }

    public static GameState CreateAttackHeroScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance attacker = CreateEntity(
            state,
            PlayerSide.Player,
            900703,
            "Debug Face Attacker",
            4,
            3,
            3,
            3,
            GameZone.Field
        );

        attacker.CanAttack = true;
        attacker.RemainingAttacksThisTurn = 1;

        state.Player.Field.Add(attacker);

        return state;
    }

    public static GameState CreateOnDeathSummonScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance damageSpell = CreateSpell(
            state,
            PlayerSide.Player,
            900601,
            "Debug Death Bolt",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.DealDamage,
                Target = EffectTarget.SelectedEnemyCard,
                Value = 2,
                AttackValue = 0,
                HealthValue = 0,
                Keyword = KeywordType.None
            }
        );

        CardInstance deathMinion = CreateEntity(
            state,
            PlayerSide.Enemy,
            900602,
            "Debug Egg Husk",
            0,
            2,
            2,
            2,
            GameZone.Field
        );

        deathMinion.Effects.Add(new CardEffect
        {
            Trigger = EffectTrigger.OnDeath,
            Type = EffectType.Summon,
            Target = EffectTarget.None,
            Value = 2,
            AttackValue = 0,
            HealthValue = 0,
            CardId = 901001,
            Keyword = KeywordType.None
        });

        state.Player.Hand.Add(damageSpell);
        state.Enemy.Field.Add(deathMinion);

        return state;
    }

    public static GameState CreateSummonOnPlayScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance summoner = CreateEntity(
            state,
            PlayerSide.Player,
            900501,
            "Debug Rat Caller",
            2,
            2,
            2,
            3,
            GameZone.Hand
        );

        summoner.Effects.Add(new CardEffect
        {
            Trigger = EffectTrigger.OnPlay,
            Type = EffectType.Summon,
            Target = EffectTarget.None,
            Value = 2,
            AttackValue = 0,
            HealthValue = 0,
            CardId = 901001,
            Keyword = KeywordType.None
        });

        state.Player.Hand.Add(summoner);

        return state;
    }

    public static GameState CreateDrawCardsOnPlayScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance drawMinion = CreateEntity(
            state,
            PlayerSide.Player,
            900401,
            "Debug Bookkeeper",
            2,
            2,
            2,
            2,
            GameZone.Hand
        );

        drawMinion.Effects.Add(new CardEffect
        {
            Trigger = EffectTrigger.OnPlay,
            Type = EffectType.DrawCards,
            Target = EffectTarget.SelfHero,
            Value = 1,
            AttackValue = 0,
            HealthValue = 0,
            Keyword = KeywordType.None
        });

        CardInstance deckCard = CreateEntity(
            state,
            PlayerSide.Player,
            900402,
            "Debug Deck Rat",
            1,
            1,
            1,
            1,
            GameZone.Deck
        );

        state.Player.Hand.Add(drawMinion);
        state.Player.Deck.Add(deckCard);

        return state;
    }

    public static GameState CreateDealDamageEnemyCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance damageSpell = CreateSpell(
            state,
            PlayerSide.Player,
            900001,
            "Debug Firebolt",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.DealDamage,
                Target = EffectTarget.SelectedEnemyCard,
                Value = 2
            }
        );

        CardInstance enemyMinion = CreateEntity(
            state,
            PlayerSide.Enemy,
            900101,
            "Debug Enemy Dummy",
            1,
            2,
            2,
            1,
            GameZone.Field
        );

        state.Player.Hand.Add(damageSpell);
        state.Enemy.Field.Add(enemyMinion);

        return state;
    }

    public static GameState CreateDealDamageEnemyHeroScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance damageSpell = CreateSpell(
            state,
            PlayerSide.Player,
            900002,
            "Debug Face Bolt",
            1,
            new CardEffect
            {
                Trigger = EffectTrigger.OnPlay,
                Type = EffectType.DealDamage,
                Target = EffectTarget.SelectedEnemyCharacter,
                Value = 3
            }
        );

        state.Player.Hand.Add(damageSpell);

        return state;
    }

    public static GameState CreateCoinScenario(Card coinDefinition)
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance coin = CardInstanceFactory.Create(
            coinDefinition,
            state.GenerateInstanceId(),
            PlayerSide.Player,
            GameZone.Hand
        );

        state.Player.Hand.Add(coin);

        return state;
    }

    public static GameState CreateBuffSelectedAllyCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance buffer = CreateEntity(
            state,
            PlayerSide.Player,
            900201,
            "Debug Blesser",
            3,
            2,
            2,
            3,
            GameZone.Hand
        );

        buffer.Effects.Add(new CardEffect
        {
            Trigger = EffectTrigger.OnPlay,
            Type = EffectType.BuffStats,
            Target = EffectTarget.SelectedAllyCard,
            Value = 0,
            AttackValue = 1,
            HealthValue = 1,
            Keyword = KeywordType.None
        });

        CardInstance allyTarget = CreateEntity(
            state,
            PlayerSide.Player,
            900202,
            "Debug Ally Dummy",
            1,
            2,
            2,
            1,
            GameZone.Field
        );

        state.Player.Hand.Add(buffer);
        state.Player.Field.Add(allyTarget);

        return state;
    }

    private static GameState CreateEmptyState(PlayerSide activeSide)
    {
        GameState state = new GameState
        {
            TurnNumber = 1,
            ActionIndex = 0,
            ActiveSide = activeSide,
            IsGameOver = false,
            Winner = null,
            NextInstanceId = 1,

            Player = CreatePlayerState(PlayerSide.Player),
            Enemy = CreatePlayerState(PlayerSide.Enemy),

            CardDatabase = CreateDebugCardDatabase(),
        };

        state.GetPlayerState(activeSide).TurnsStarted = 1;

        return state;
    }

    private static AllCards CreateDebugCardDatabase()
    {
        AllCards database = new AllCards();

        database.cards.Add(CreateDebugCardDefinition(
            901001,
            "Debug Summoned Rat",
            1,
            1,
            1,
            1
        ));

        return database;
    }

    private static Card CreateDebugCardDefinition(
        int cardId,
        string title,
        int attack,
        int hp,
        int maxHp,
        int manaCost)
    {
        return new Card
        {
            id = cardId,
            Title = title,
            Class = Card.CardClass.ENTITY,
            Attack = attack,
            HP = hp,
            MaxHP = maxHp,
            ManaCost = manaCost,
            Keywords = new List<KeywordType>(),
            Effects = new List<CardEffect>(),
            IsCollectible = false
        };
    }

    private static PlayerState CreatePlayerState(PlayerSide side)
    {
        return new PlayerState
        {
            Side = side,

            HP = GameRules.StartingHeroHealth,
            MaxHP = GameRules.StartingHeroHealth,

            Mana = 10,
            ManaPool = 10,
            PendingTemporaryMana = 0,
            TurnsStarted = 0,

            Deck = new List<CardInstance>(),
            Hand = new List<CardInstance>(),
            Field = new List<CardInstance>(),
            Graveyard = new List<CardInstance>()
        };
    }

    private static CardInstance CreateSpell(
        GameState state,
        PlayerSide owner,
        int cardId,
        string title,
        int manaCost,
        CardEffect effect)
    {
        return new CardInstance
        {
            InstanceId = state.GenerateInstanceId(),
            CardId = cardId,

            Title = title,
            Class = Card.CardClass.SPELL,

            Attack = 0,
            HP = 0,
            MaxHP = 0,
            ManaCost = manaCost,

            CanAttack = false,
            CanAttackOnlyCardsThisTurn = false,
            IsPlaced = false,

            RemainingAttacksThisTurn = 0,

            OwnerSide = owner,
            Zone = GameZone.Hand,

            Keywords = new List<KeywordType>(),
            Effects = new List<CardEffect> { effect }
        };
    }

    private static CardInstance CreateEntity(
        GameState state,
        PlayerSide owner,
        int cardId,
        string title,
        int attack,
        int hp,
        int maxHp,
        int manaCost,
        GameZone zone)
    {
        return new CardInstance
        {
            InstanceId = state.GenerateInstanceId(),
            CardId = cardId,

            Title = title,
            Class = Card.CardClass.ENTITY,

            Attack = attack,
            HP = hp,
            MaxHP = maxHp,
            ManaCost = manaCost,

            CanAttack = false,
            CanAttackOnlyCardsThisTurn = false,
            RemainingAttacksThisTurn = 0,

            IsPlaced = zone == GameZone.Field,

            OwnerSide = owner,
            Zone = zone,

            Keywords = new List<KeywordType>(),
            Effects = new List<CardEffect>()
        };
    }

    public static GameState CreateAddKeywordSelectedAllyCardScenario()
    {
        GameState state = CreateEmptyState(PlayerSide.Player);

        CardInstance keywordGiver = CreateEntity(
            state,
            PlayerSide.Player,
            900301,
            "Debug Banner Giver",
            3,
            3,
            3,
            3,
            GameZone.Hand
        );

        keywordGiver.Effects.Add(new CardEffect
        {
            Trigger = EffectTrigger.OnPlay,
            Type = EffectType.AddKeyword,
            Target = EffectTarget.SelectedAllyCard,
            Value = 0,
            AttackValue = 0,
            HealthValue = 0,
            Keyword = KeywordType.Provocation
        });

        CardInstance allyTarget = CreateEntity(
            state,
            PlayerSide.Player,
            900302,
            "Debug Ally Guard",
            2,
            2,
            2,
            2,
            GameZone.Field
        );

        state.Player.Hand.Add(keywordGiver);
        state.Player.Field.Add(allyTarget);

        return state;
    }
}
