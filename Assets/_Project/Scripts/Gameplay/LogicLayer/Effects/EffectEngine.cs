using System.Collections.Generic;

/// <summary>
/// Resolves card effects against pure GameState.
/// 
/// This class must not depend on Unity objects.
/// </summary>
public static class EffectEngine
{
    public static void ResolveEffects(
        GameState state,
        CardInstance source,
        EffectTrigger trigger,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (state == null || source == null || source.Effects == null)
            return;

        foreach (CardEffect effect in source.Effects)
        {
            if (effect == null)
                continue;

            if (effect.Trigger != trigger)
                continue;

            ResolveEffect(state, source, effect, trigger, selectedTarget, events);
        }
    }

    private static void ResolveEffect(
        GameState state,
        CardInstance source,
        CardEffect effect,
        EffectTrigger trigger,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (IsSelectedTargetEffect(effect) &&
            (selectedTarget == null || selectedTarget.TargetType == PlayTargetType.None))
        {
            events?.Add(GameEvent.Create(
                state,
                GameEventType.EffectResolved,
                source.OwnerSide,
                $"Effect {effect.Type} was skipped because no target was selected.",
                sourceInstanceId: source.InstanceId
            ));

            return;
        }

        switch (effect.Type)
        {
            case EffectType.GainTemporaryMana:
                ResolveGainTemporaryMana(state, source, effect, events);
                break;

            case EffectType.GainTemporaryManaNextTurn:
                ResolveGainTemporaryManaNextTurn(state, source, effect, selectedTarget, events);
                break;

            case EffectType.DealDamage:
                ResolveDealDamage(state, source, effect, selectedTarget, events);
                break;

            case EffectType.Heal:
                ResolveHeal(state, source, effect, selectedTarget, events);
                break;

            case EffectType.Destroy:
                ResolveDestroy(state, source, effect, selectedTarget, events);
                break;

            case EffectType.Silence:
                ResolveSilence(state, source, effect, selectedTarget, events);
                break;

            case EffectType.BuffStats:
                ResolveBuffStats(state, source, effect, selectedTarget, events);
                break;

            case EffectType.BuffAttack:
                ResolveBuffAttack(state, source, effect, selectedTarget, events);
                break;

            case EffectType.BuffHealth:
                ResolveBuffHealth(state, source, effect, selectedTarget, events);
                break;

            case EffectType.DebuffAttack:
                ResolveDebuffAttack(state, source, effect, selectedTarget, events);
                break;

            case EffectType.AddKeyword:
                ResolveAddKeyword(state, source, effect, selectedTarget, events);
                break;

            case EffectType.DrawCards:
                ResolveDrawCards(state, source, effect, events);
                break;

            case EffectType.Summon:
                ResolveSummon(state, source, effect, trigger, events);
                break;


            default:
                events?.Add(GameEvent.Create(
                    state,
                    GameEventType.EffectResolved,
                    source.OwnerSide,
                    $"Effect {effect.Type} is not implemented in pure EffectEngine yet.",
                    sourceInstanceId: source.InstanceId
                ));
                break;
        }
    }

    private static void ResolveSilence(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        List<CardInstance> targetCards = GetTargetCards(
            state,
            source,
            effect,
            selectedTarget
        );

        foreach (CardInstance targetCard in targetCards)
        {
            SilenceCard(
                state,
                source,
                targetCard,
                events
            );
        }
    }

    private static void ResolveDestroy(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        List<CardInstance> targetCards = GetTargetCards(
            state,
            source,
            effect,
            selectedTarget
        );

        foreach (CardInstance targetCard in targetCards)
        {
            DestroyCard(
                state,
                source,
                targetCard,
                events
            );
        }

        ResolveDeaths(state, events);
    }

    private static void ResolveDebuffAttack(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        int amount = effect.AttackValue != 0 ? effect.AttackValue : effect.Value;

        if (amount <= 0)
            return;

        List<CardInstance> targetCards = GetTargetCards(
            state,
            source,
            effect,
            selectedTarget
        );

        foreach (CardInstance targetCard in targetCards)
        {
            DebuffCardAttack(
                state,
                source,
                targetCard,
                amount,
                events
            );
        }
    }

    private static void ResolveBuffHealth(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        int amount = effect.HealthValue != 0 ? effect.HealthValue : effect.Value;

        if (amount == 0)
            return;

        List<CardInstance> targetCards = GetTargetCards(
            state,
            source,
            effect,
            selectedTarget
        );

        foreach (CardInstance targetCard in targetCards)
        {
            BuffCardStats(
                state,
                source,
                targetCard,
                0,
                amount,
                events
            );
        }
    }

    private static void ResolveBuffAttack(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        int amount = effect.AttackValue != 0 ? effect.AttackValue : effect.Value;

        if (amount == 0)
            return;

        List<CardInstance> targetCards = GetTargetCards(
            state,
            source,
            effect,
            selectedTarget
        );

        foreach (CardInstance targetCard in targetCards)
        {
            BuffCardStats(
                state,
                source,
                targetCard,
                amount,
                0,
                events
            );
        }
    }

    private static void ResolveHeal(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        if (effect.Value <= 0)
            return;

        List<CardInstance> targetCards = GetTargetCards(
            state,
            source,
            effect,
            selectedTarget
        );

        foreach (CardInstance targetCard in targetCards)
        {
            HealCard(
                state,
                source,
                targetCard,
                effect.Value,
                events
            );
        }

        PlayerSide? targetHeroSide = GetTargetHeroSide(
            state,
            source,
            effect,
            selectedTarget
        );

        if (targetHeroSide.HasValue)
        {
            HealHero(
                state,
                source,
                targetHeroSide.Value,
                effect.Value,
                events
            );
        }
    }

    private static void ResolveSummon(
        GameState state,
        CardInstance source,
        CardEffect effect,
        EffectTrigger trigger,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        if (effect.CardId <= 0)
            return;

        int summonCount = effect.Value > 0 ? effect.Value : 1;

        PlayerState owner = state.GetPlayerState(source.OwnerSide);

        if (owner == null)
            return;

        for (int i = 0; i < summonCount; i++)
        {
            int fieldIndex = GetSummonFieldIndex(
                state,
                source,
                trigger,
                i
            );

            SummonOneCard(
                state,
                source,
                owner,
                effect.CardId,
                events,
                fieldIndex
            );
        }
    }

    private static void ResolveDrawCards(
        GameState state,
        CardInstance source,
        CardEffect effect,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        if (effect.Value <= 0)
            return;

        PlayerSide targetSide = source.OwnerSide;

        switch (effect.Target)
        {
            case EffectTarget.SelfHero:
            case EffectTarget.None:
                targetSide = source.OwnerSide;
                break;

            case EffectTarget.EnemyHero:
                targetSide = state.GetOpponentSide(source.OwnerSide);
                break;

            default:
                targetSide = source.OwnerSide;
                break;
        }

        PlayerState targetPlayer = state.GetPlayerState(targetSide);

        if (targetPlayer == null)
            return;

        for (int i = 0; i < effect.Value; i++)
        {
            CardZoneService.DrawCard(
                state,
                targetPlayer,
                source,
                events
            );
        }
    }

    private static void ResolveAddKeyword(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        if (effect.Keyword == KeywordType.None)
            return;

        List<CardInstance> targetCards = GetTargetCards(
            state,
            source,
            effect,
            selectedTarget
        );

        foreach (CardInstance targetCard in targetCards)
        {
            AddKeywordToCard(
                state,
                source,
                targetCard,
                effect.Keyword,
                events
            );
        }
    }

    private static void ResolveBuffStats(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        List<CardInstance> targetCards = GetTargetCards(
            state,
            source,
            effect,
            selectedTarget
        );

        foreach (CardInstance targetCard in targetCards)
        {
            BuffCardStats(
                state,
                source,
                targetCard,
                effect.AttackValue,
                effect.HealthValue,
                events
            );
        }
    }

    private static void ResolveGainTemporaryMana(
        GameState state,
        CardInstance source,
        CardEffect effect,
        List<GameEvent> events)
    {
        if (effect.Value <= 0)
            return;

        PlayerState owner = state.GetPlayerState(source.OwnerSide);

        if (owner == null)
            return;

        int oldMana = owner.Mana;

        owner.Mana += effect.Value;

        if (owner.Mana > GameRules.MaxManaPool)
            owner.Mana = GameRules.MaxManaPool;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.TemporaryManaGained,
            source.OwnerSide,
            $"{source.OwnerSide} gained {owner.Mana - oldMana} temporary mana.",
            owner.Mana - oldMana,
            sourceInstanceId: source.InstanceId
        ));
    }

    private static void ResolveGainTemporaryManaNextTurn(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (effect.Value <= 0)
            return;

        PlayerSide targetSide;

        switch (effect.Target)
        {
            case EffectTarget.SelfHero:
            case EffectTarget.None:
                targetSide = source.OwnerSide;
                break;

            case EffectTarget.EnemyHero:
                targetSide = state.GetOpponentSide(source.OwnerSide);
                break;

            case EffectTarget.SelectedAllyCharacter:
            case EffectTarget.SelectedEnemyCharacter:
                if (selectedTarget == null ||
                    selectedTarget.TargetType != PlayTargetType.Hero ||
                    !selectedTarget.TargetHeroSide.HasValue)
                {
                    return;
                }

                targetSide = selectedTarget.TargetHeroSide.Value;
                break;

            default:
                return;
        }

        PlayerState targetPlayer = state.GetPlayerState(targetSide);

        if (targetPlayer == null)
            return;

        targetPlayer.PendingTemporaryMana += effect.Value;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.TemporaryManaNextTurnAdded,
            source.OwnerSide,
            $"{targetSide} will gain {effect.Value} temporary mana next turn.",
            effect.Value,
            sourceInstanceId: source.InstanceId,
            targetHeroSide: targetSide
        ));
    }

    private static void ResolveDealDamage(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget,
        List<GameEvent> events)
    {
        if (state == null || source == null || effect == null)
            return;

        if (effect.Value <= 0)
            return;

        List<CardInstance> targetCards = GetTargetCards(
            state,
            source,
            effect,
            selectedTarget
        );

        foreach (CardInstance targetCard in targetCards)
        {
            DealDamageToCard(
                state,
                source,
                targetCard,
                effect.Value,
                events
            );
        }

        ResolveDeadCards(state, events);

        PlayerSide? targetHeroSide = GetTargetHeroSide(
            state,
            source,
            effect,
            selectedTarget
        );

        if (targetHeroSide.HasValue)
        {
            DealDamageToHero(
                state,
                source,
                targetHeroSide.Value,
                effect.Value,
                events
            );
        }

        ResolveDeadCards(state, events);
    }

    private static void SilenceCard(
        GameState state,
        CardInstance source,
        CardInstance target,
        List<GameEvent> events)
    {
        if (state == null || source == null || target == null)
            return;

        if (!target.IsEntity)
            return;

        if (target.Zone != GameZone.Field)
            return;

        KeywordService.ClearKeywords(target);

        if (target.Effects != null)
            target.Effects.Clear();

        target.CanAttackOnlyCardsThisTurn = false;
        target.IsSilenced = true;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.CardSilenced,
            source.OwnerSide,
            $"{source.Title} silenced {target.Title}.",
            0,
            sourceInstanceId: source.InstanceId,
            targetInstanceId: target.InstanceId
        ));
    }

    private static void DestroyCard(
        GameState state,
        CardInstance source,
        CardInstance target,
        List<GameEvent> events)
    {
        if (state == null || source == null || target == null)
            return;

        if (!target.IsEntity)
            return;

        if (target.Zone != GameZone.Field)
            return;

        target.HP = 0;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.CardDestroyed,
            source.OwnerSide,
            $"{source.Title} destroyed {target.Title}.",
            0,
            sourceInstanceId: source.InstanceId,
            targetInstanceId: target.InstanceId
        ));
    }

    private static void DebuffCardAttack(
        GameState state,
        CardInstance source,
        CardInstance target,
        int amount,
        List<GameEvent> events)
    {
        if (state == null || source == null || target == null)
            return;

        if (!target.IsEntity)
            return;

        if (amount <= 0)
            return;

        int oldAttack = target.Attack;

        target.Attack -= amount;

        if (target.Attack < 0)
            target.Attack = 0;

        int actualReduction = oldAttack - target.Attack;

        if (actualReduction <= 0)
            return;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.CardStatsDebuffed,
            source.OwnerSide,
            $"{source.Title} reduced {target.Title} attack by {actualReduction}.",
            actualReduction,
            sourceInstanceId: source.InstanceId,
            targetInstanceId: target.InstanceId
        ));
    }

    private static void HealHero(
        GameState state,
        CardInstance source,
        PlayerSide targetHeroSide,
        int amount,
        List<GameEvent> events)
    {
        if (state == null || source == null || amount <= 0)
            return;

        PlayerState targetPlayer = state.GetPlayerState(targetHeroSide);

        if (targetPlayer == null)
            return;

        if (targetPlayer.HP <= 0)
            return;

        int oldHp = targetPlayer.HP;

        targetPlayer.HP += amount;

        if (targetPlayer.HP > targetPlayer.MaxHP)
            targetPlayer.HP = targetPlayer.MaxHP;

        int healedAmount = targetPlayer.HP - oldHp;

        if (healedAmount <= 0)
            return;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.HeroHealed,
            source.OwnerSide,
            $"{source.Title} healed {targetHeroSide} hero for {healedAmount}.",
            healedAmount,
            sourceInstanceId: source.InstanceId,
            targetHeroSide: targetHeroSide
        ));
    }

    private static void HealCard(
        GameState state,
        CardInstance source,
        CardInstance target,
        int amount,
        List<GameEvent> events)
    {
        if (state == null || source == null || target == null || amount <= 0)
            return;

        if (!target.IsEntity)
            return;

        if (target.HP <= 0)
            return;

        int oldHp = target.HP;

        target.HP += amount;

        if (target.HP > target.MaxHP)
            target.HP = target.MaxHP;

        int healedAmount = target.HP - oldHp;

        if (healedAmount <= 0)
            return;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.CardHealed,
            source.OwnerSide,
            $"{source.Title} healed {target.Title} for {healedAmount}.",
            healedAmount,
            sourceInstanceId: source.InstanceId,
            targetInstanceId: target.InstanceId
        ));
    }

    private static void SummonOneCard(
        GameState state,
        CardInstance source,
        PlayerState owner,
        int cardId,
        List<GameEvent> events,
        int? fieldIndex = null)
    {
        if (state == null || source == null || owner == null)
            return;

        Card definition = state.CardDatabase != null
            ? state.CardDatabase.FindCardDefinition(cardId)
            : null;

        if (definition == null)
        {
            events?.Add(GameEvent.Create(
                state,
                GameEventType.SummonSkippedMissingCardDefinition,
                source.OwnerSide,
                $"{source.Title} could not summon CardId {cardId}: card definition not found.",
                sourceInstanceId: source.InstanceId
            ));

            return;
        }

        CardInstance summonedCard = CardInstanceFactory.Create(
            definition,
            state.GenerateInstanceId(),
            owner.Side,
            GameZone.Field
        );

        CardZoneService.SummonToField(
            state,
            owner,
            summonedCard,
            source,
            events,
            fieldIndex
        );
    }

    private static int GetSummonFieldIndex(
        GameState state,
        CardInstance source,
        EffectTrigger trigger,
        int summonOffset)
    {
        if (state == null || source == null)
            return 0;

        PlayerState owner = state.GetPlayerState(source.OwnerSide);

        if (owner == null || owner.Field == null)
            return 0;

        if (trigger == EffectTrigger.OnDeath)
        {
            int deathIndex = source.LastKnownFieldIndex;

            if (deathIndex < 0)
                deathIndex = owner.Field.Count;

            return ClampFieldIndex(
                deathIndex + summonOffset,
                owner
            );
        }

        if (trigger == EffectTrigger.OnPlay && source.IsEntity)
        {
            int sourceIndex = owner.Field.IndexOf(source);

            if (sourceIndex >= 0)
            {
                // Flank the summoner: even offsets go to its right, odd to its
                // left, so multiple tokens appear on both sides instead of
                // stacking on one. (Field order is cosmetic — no positional rules.)
                int sideStep = summonOffset / 2;

                int index = (summonOffset % 2) == 0
                    ? sourceIndex + 1 + sideStep   // right side
                    : sourceIndex - sideStep;      // left side

                return ClampFieldIndex(index, owner);
            }
        }

        return owner.Field.Count;
    }

    private static int ClampFieldIndex(int index, PlayerState owner)
    {
        int count = owner != null && owner.Field != null
            ? owner.Field.Count
            : 0;

        if (index < 0)
            return 0;

        if (index > count)
            return count;

        return index;
    }

    private static void AddKeywordToCard(
        GameState state,
        CardInstance source,
        CardInstance target,
        KeywordType keyword,
        List<GameEvent> events)
    {
        if (state == null || source == null || target == null)
            return;

        if (!target.IsEntity)
            return;

        bool added = KeywordService.AddKeyword(target, keyword);

        if (!added)
            return;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.KeywordAdded,
            source.OwnerSide,
            $"{source.Title} added {keyword} to {target.Title}.",
            0,
            sourceInstanceId: source.InstanceId,
            targetInstanceId: target.InstanceId
        ));
    }

    private static void BuffCardStats(
        GameState state,
        CardInstance source,
        CardInstance target,
        int attackValue,
        int healthValue,
        List<GameEvent> events)
    {
        if (state == null || source == null || target == null)
            return;

        if (!target.IsEntity)
            return;

        if (attackValue == 0 && healthValue == 0)
            return;

        target.Attack += attackValue;

        if (healthValue != 0)
        {
            target.MaxHP += healthValue;
            target.HP += healthValue;

            if (target.HP > target.MaxHP)
                target.HP = target.MaxHP;

            if (target.HP < 0)
                target.HP = 0;
        }

        events?.Add(GameEvent.Create(
            state,
            GameEventType.CardStatsBuffed,
            source.OwnerSide,
            $"{source.Title} changed {target.Title} stats by ATK {attackValue}, HP {healthValue}.",
            0,
            sourceInstanceId: source.InstanceId,
            targetInstanceId: target.InstanceId
        ));

        ResolveDeadCards(state, events);
    }

    private static List<CardInstance> GetTargetCards(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget)
    {
        List<CardInstance> result = new List<CardInstance>();

        if (state == null || source == null || effect == null)
            return result;

        PlayerState owner = state.GetPlayerState(source.OwnerSide);
        PlayerState opponent = state.GetOpponentState(source.OwnerSide);

        switch (effect.Target)
        {
            case EffectTarget.SelectedAllyCard:
            case EffectTarget.SelectedEnemyCard:
                if (selectedTarget != null &&
                    selectedTarget.TargetType == PlayTargetType.Card &&
                    selectedTarget.TargetInstanceId.HasValue)
                {
                    CardInstance selectedCard = GameStateCardFinder.FindCard(
                        state,
                        selectedTarget.TargetInstanceId.Value
                    );

                    if (selectedCard != null)
                        result.Add(selectedCard);
                }
                break;

            case EffectTarget.SelectedAllyCharacter:
            case EffectTarget.SelectedEnemyCharacter:
                if (selectedTarget != null &&
                    selectedTarget.TargetType == PlayTargetType.Card &&
                    selectedTarget.TargetInstanceId.HasValue)
                {
                    CardInstance selectedCard = GameStateCardFinder.FindCard(
                        state,
                        selectedTarget.TargetInstanceId.Value
                    );

                    if (selectedCard != null)
                        result.Add(selectedCard);
                }
                break;

            case EffectTarget.AllAllyCards:
                if (owner?.Field != null)
                    result.AddRange(owner.Field);
                break;

            case EffectTarget.AllEnemyCards:
                if (opponent?.Field != null)
                    result.AddRange(opponent.Field);
                break;

            case EffectTarget.AllCards:
                if (owner?.Field != null)
                    result.AddRange(owner.Field);

                if (opponent?.Field != null)
                    result.AddRange(opponent.Field);
                break;

            case EffectTarget.OtherAllyCards:
                if (owner?.Field != null)
                {
                    foreach (CardInstance card in owner.Field)
                    {
                        if (card != null && card.InstanceId != source.InstanceId)
                            result.Add(card);
                    }
                }
                break;
        }

        return result;
    }

    private static PlayerSide? GetTargetHeroSide(
        GameState state,
        CardInstance source,
        CardEffect effect,
        GameTarget selectedTarget)
    {
        if (state == null || source == null || effect == null)
            return null;

        switch (effect.Target)
        {
            case EffectTarget.SelfHero:
                return source.OwnerSide;

            case EffectTarget.EnemyHero:
                return state.GetOpponentSide(source.OwnerSide);

            case EffectTarget.SelectedAllyCharacter:
            case EffectTarget.SelectedEnemyCharacter:
                if (selectedTarget != null &&
                    selectedTarget.TargetType == PlayTargetType.Hero &&
                    selectedTarget.TargetHeroSide.HasValue)
                {
                    return selectedTarget.TargetHeroSide.Value;
                }

                return null;

            default:
                return null;
        }
    }

    public static int DealDamageToCard(
        GameState state,
        CardInstance source,
        CardInstance target,
        int amount,
        List<GameEvent> events)
    {
        if (state == null || source == null || target == null || amount <= 0)
            return 0;

        if (!target.IsEntity)
            return 0;

        if (target.Zone != GameZone.Field)
            return 0;

        if (KeywordService.HasKeyword(target, KeywordType.Shield))
        {
            KeywordService.RemoveKeyword(target, KeywordType.Shield);

            events?.Add(GameEvent.Create(
                state,
                GameEventType.ShieldBroken,
                source.OwnerSide,
                $"{source.Title} broke {target.Title}'s Shield.",
                0,
                sourceInstanceId: source.InstanceId,
                targetInstanceId: target.InstanceId
            ));

            events?.Add(GameEvent.Create(
                state,
                GameEventType.DamagePrevented,
                source.OwnerSide,
                $"{target.Title}'s Shield prevented {amount} damage.",
                amount,
                sourceInstanceId: source.InstanceId,
                targetInstanceId: target.InstanceId
            ));

            return 0;
        }

        target.HP -= amount;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.DamageDealt,
            source.OwnerSide,
            $"{source.Title} dealt {amount} damage to {target.Title}.",
            amount,
            sourceInstanceId: source.InstanceId,
            targetInstanceId: target.InstanceId
        ));

        ApplyLifestealIfNeeded(
            state,
            source,
            amount,
            events
        );

        return amount;
    }


    public static int DealDamageToHero(
        GameState state,
        CardInstance source,
        PlayerSide targetHeroSide,
        int amount,
        List<GameEvent> events)
    {
        if (state == null || source == null || amount <= 0)
            return 0;

        PlayerState targetPlayer = state.GetPlayerState(targetHeroSide);

        if (targetPlayer == null)
            return 0;

        int oldHp = targetPlayer.HP;

        targetPlayer.HP -= amount;

        if (targetPlayer.HP < 0)
            targetPlayer.HP = 0;

        int actualDamage = oldHp - targetPlayer.HP;

        if (actualDamage <= 0)
            return 0;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.HeroDamaged,
            source.OwnerSide,
            $"{source.Title} dealt {actualDamage} damage to {targetHeroSide} hero.",
            actualDamage,
            sourceInstanceId: source.InstanceId,
            targetHeroSide: targetHeroSide
        ));

        ApplyLifestealIfNeeded(
            state,
            source,
            actualDamage,
            events
        );

        if (targetPlayer.HP <= 0)
        {
            state.IsGameOver = true;
            state.Winner = state.GetOpponentSide(targetHeroSide);

            events?.Add(GameEvent.Create(
                state,
                GameEventType.GameEnded,
                source.OwnerSide,
                $"{state.Winner.Value} wins.",
                targetHeroSide: targetHeroSide
            ));
        }

        return actualDamage;
    }

    private static void ResolveDeadCards(
        GameState state,
        List<GameEvent> events)
    {
        if (state == null)
            return;

        const int maxDeathResolutionLoops = 20;
        int loop = 0;

        while (loop < maxDeathResolutionLoops)
        {
            loop++;

            List<CardInstance> deadCards = CollectDeadCards(state);

            if (deadCards.Count == 0)
                break;

            foreach (CardInstance deadCard in deadCards)
            {
                MoveDeadCardToGraveyard(state, deadCard, events);

                if (HasEffectsForTrigger(deadCard, EffectTrigger.OnDeath))
                {
                    EffectEngine.ResolveEffects(
                        state,
                        deadCard,
                        EffectTrigger.OnDeath,
                        GameTarget.None(),
                        events
                    );

                    events?.Add(GameEvent.Create(
                        state,
                        GameEventType.OnDeathResolved,
                        deadCard.OwnerSide,
                        $"{deadCard.Title} OnDeath effects resolved.",
                        sourceInstanceId: deadCard.InstanceId
                    ));
                }
            }
        }
    }

    private static bool HasEffectsForTrigger(
        CardInstance card,
        EffectTrigger trigger)
    {
        if (card == null || card.Effects == null)
            return false;

        foreach (CardEffect effect in card.Effects)
        {
            if (effect != null && effect.Trigger == trigger)
                return true;
        }

        return false;
    }

    private static List<CardInstance> CollectDeadCards(GameState state)
    {
        List<CardInstance> result = new List<CardInstance>();

        if (state == null)
            return result;

        CollectDeadCardsFromPlayer(state.Player, result);
        CollectDeadCardsFromPlayer(state.Enemy, result);

        return result;
    }

    private static void CollectDeadCardsFromPlayer(
        PlayerState player,
        List<CardInstance> result)
    {
        if (player == null || player.Field == null || result == null)
            return;

        foreach (CardInstance card in player.Field)
        {
            if (card == null)
                continue;

            if (!card.IsEntity)
                continue;

            if (card.HP <= 0)
                result.Add(card);
        }
    }

    private static void MoveDeadCardToGraveyard(
        GameState state,
        CardInstance deadCard,
        List<GameEvent> events)
    {
        if (state == null || deadCard == null)
            return;

        PlayerState owner = state.GetPlayerState(deadCard.OwnerSide);

        if (owner == null)
            return;

        if (owner.Field != null)
            deadCard.LastKnownFieldIndex = owner.Field.IndexOf(deadCard);

        CardZoneService.MoveToGraveyard(
            state,
            owner,
            deadCard,
            events,
            GameEventType.CardDied,
            $"{deadCard.Title} died."
        );
    }

    private static bool IsSelectedTargetEffect(CardEffect effect)
    {
        if (effect == null)
            return false;

        switch (effect.Target)
        {
            case EffectTarget.SelectedAllyCard:
            case EffectTarget.SelectedEnemyCard:
            case EffectTarget.SelectedAllyCharacter:
            case EffectTarget.SelectedEnemyCharacter:
                return true;

            default:
                return false;
        }
    }

    private static void ApplyLifestealIfNeeded(
        GameState state,
        CardInstance source,
        int actualDamage,
        List<GameEvent> events)
    {
        if (state == null || source == null || actualDamage <= 0)
            return;

        if (!KeywordService.HasKeyword(source, KeywordType.Lifesteal))
            return;

        PlayerState owner = state.GetPlayerState(source.OwnerSide);

        if (owner == null)
            return;

        int oldHp = owner.HP;

        owner.HP += actualDamage;

        if (owner.HP > owner.MaxHP)
            owner.HP = owner.MaxHP;

        int healedAmount = owner.HP - oldHp;

        if (healedAmount <= 0)
            return;

        events?.Add(GameEvent.Create(
            state,
            GameEventType.HeroHealed,
            source.OwnerSide,
            $"{source.Title} restored {healedAmount} health to {source.OwnerSide} hero with Lifesteal.",
            healedAmount,
            sourceInstanceId: source.InstanceId,
            targetHeroSide: source.OwnerSide
        ));
    }

    public static void ResolveDeaths(
        GameState state,
        List<GameEvent> events)
    {
        ResolveDeadCards(state, events);
    }
}
