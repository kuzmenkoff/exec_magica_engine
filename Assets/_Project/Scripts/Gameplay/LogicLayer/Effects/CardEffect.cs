using System;

/// <summary>
/// Describes a single effect attached to a card.
/// 
/// This model is intended for the future effects-based card system.
/// It does not execute anything by itself.
/// </summary>
[Serializable]
public class CardEffect
{
    /// <summary>
    /// Defines when this effect should be triggered.
    /// </summary>
    public EffectTrigger Trigger;

    /// <summary>
    /// Defines what this effect does.
    /// </summary>
    public EffectType Type;

    /// <summary>
    /// Defines what this effect targets.
    /// </summary>
    public EffectTarget Target;

    /// <summary>
    /// Main numeric value of the effect.
    /// Example: damage amount, healing amount, number of cards to draw.
    /// </summary>
    public int Value;

    /// <summary>
    /// Optional attack value used by BuffStats effects.
    /// </summary>
    public int AttackValue;

    /// <summary>
    /// Optional health value used by BuffStats effects.
    /// </summary>
    public int HealthValue;

    /// <summary>
    /// Optional card id used by effects such as Summon.
    /// </summary>
    public int CardId;

    /// <summary>
    /// Optional keyword used by AddKeyword effects.
    /// </summary>
    public KeywordType Keyword;

    /// <summary>
    /// Optional condition text or key.
    /// This is reserved for future conditional effects.
    /// </summary>
    public string Condition;
}
