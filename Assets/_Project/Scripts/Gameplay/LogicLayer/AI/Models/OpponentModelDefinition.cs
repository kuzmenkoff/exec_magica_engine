using UnityEngine;

/// <summary>
/// ScriptableObject defining a selectable AI opponent: identity, in-game presentation, rating,
/// and how to build its runtime policy. Concrete subclasses (Random/Greedy/MCTS) supply the
/// actual <see cref="IGameActionPolicy"/> and parameter metadata.
/// </summary>
public abstract class OpponentModelDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;

    [Header("In-game presentation")]
    [Tooltip("Off -> excluded from the setup-panel dropdowns (still usable in research/tournaments).")]
    /// <summary>Whether this model appears in the in-game setup dropdowns (still usable in research/tournaments).</summary>
    [SerializeField] private bool showInGame = true;
    [Tooltip("Resources path to the hero portrait shown in gameplay for this opponent.")]
    /// <summary>Resources path to the hero portrait shown for this opponent.</summary>
    [SerializeField] private string heroAvatarPath;

    /// <summary>Bradley–Terry ? Elo rating filled by the rating tournament; 0 = unrated.</summary>
    [Header("Rating (auto-filled by the round-robin tournament)")]
    [SerializeField] private double eloRating = 0.0;
    /// <summary>Whether this model has a computed rating.</summary>
    [SerializeField] private bool rated = false;

    [Tooltip("Friendly description shown as a sticky toast when the model is selected.")]
    [TextArea][SerializeField] private string description;
    public string Description => description;

    public string Id => id;
    public string DisplayName => displayName;
    public bool ShowInGame => showInGame;
    public string HeroAvatarPath => heroAvatarPath;
    public double EloRating => eloRating;
    public bool Rated => rated;

    /// <summary>Builds the runtime AI policy for this model. <paramref name="seed"/> 0 = non-deterministic.</summary>
    public abstract IGameActionPolicy CreatePolicy(int seed);
    /// <summary>Returns telemetry metadata (model id + parameters) recorded with each session.</summary>
    public abstract ModelInfo BuildModelInfo();
    /// <summary>Serializable spec for the .NET bench runner and research tooling; mirrors CreatePolicy.</summary>
    public abstract AgentSpec ToAgentSpec();

#if UNITY_EDITOR
    /// <summary>Called by the Tournament+Rating Editor tool to persist computed ratings.</summary>
    public void SetRating(double elo)
    {
        eloRating = elo;
        rated = true;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
