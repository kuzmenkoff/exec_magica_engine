using UnityEngine;

public abstract class OpponentModelDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;

    [Header("In-game presentation")]
    [Tooltip("Off -> excluded from the setup-panel dropdowns (still usable in research/tournaments).")]
    [SerializeField] private bool showInGame = true;
    [Tooltip("Resources path to the hero portrait shown in gameplay for this opponent.")]
    [SerializeField] private string heroAvatarPath;

    [Header("Rating (auto-filled by the round-robin tournament)")]
    [SerializeField] private double eloRating = 0.0;   // Bradley-Terry -> Elo scale; 0 = unrated
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

    public abstract IGameActionPolicy CreatePolicy(int seed);
    public abstract ModelInfo BuildModelInfo();

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
