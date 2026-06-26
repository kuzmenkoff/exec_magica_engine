using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity-facing adapter over the pure GameEngine.
/// 
/// This class is the runtime bridge:
/// Unity input creates GameAction ->
/// adapter sends it to GameEngine ->
/// Unity visual layer reads GameStepResult.Events and GameState.
/// 
/// No gameplay rules should be implemented here.
/// </summary>
public class UnityGameEngineAdapter
{
    public GameEngine Engine { get; private set; }

    public GameState State
    {
        get { return Engine != null ? Engine.State : null; }
    }

    public bool IsInitialized
    {
        get { return Engine != null && Engine.State != null; }
    }

    public void Initialize(GameState initialState)
    {
        if (initialState == null)
        {
            Debug.LogError("[UnityGameEngineAdapter] Cannot initialize with null GameState.");
            return;
        }

        Engine = new GameEngine(initialState);

        Debug.Log(
            "[UnityGameEngineAdapter] Initialized. " +
            $"ActiveSide: {initialState.ActiveSide}, Turn: {initialState.TurnNumber}"
        );
    }

    public List<GameAction> GetLegalActions()
    {
        if (!IsInitialized)
            return new List<GameAction>();

        return Engine.GetLegalActions();
    }

    public List<GameAction> GetLegalActions(PlayerSide side)
    {
        if (!IsInitialized)
            return new List<GameAction>();

        return Engine.GetLegalActions(side);
    }

    public GameStepResult ApplyAction(GameAction action)
    {
        if (!IsInitialized)
            return GameStepResult.Failure("UnityGameEngineAdapter is not initialized.");

        GameStepResult result = Engine.ApplyAction(action);

        if (!result.Success)
        {
            Debug.LogWarning(
                "[UnityGameEngineAdapter] Action failed: " +
                action + " | " + result.ErrorMessage
            );
        }

        return result;
    }
}
