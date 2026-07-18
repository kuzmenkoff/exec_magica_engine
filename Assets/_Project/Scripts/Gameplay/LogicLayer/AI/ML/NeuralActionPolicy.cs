using System;
using System.Collections.Generic;

/// <summary>
/// Pure network policy (no search): encodes the state, runs the distilled net, and picks the
/// legal action with the highest policy logit (or samples from the masked softmax). The "NN"
/// baseline for the ladder — one forward pass per move, no MCTS.
/// </summary>
public class NeuralActionPolicy : IGameActionPolicy
{
    private readonly NeuralNet net;
    private readonly System.Random rng;
    private readonly bool sample;       // false = argmax (deterministic), true = softmax sample

    /// <summary>Creates the policy. <paramref name="seed"/> 0 = non-deterministic (only matters if sampling).</summary>
    public NeuralActionPolicy(NeuralNet net, int seed = 0, bool sample = false)
    {
        this.net = net;
        this.sample = sample;
        rng = seed == 0 ? new System.Random() : new System.Random(seed);
    }

    /// <inheritdoc/>
    public GameAction ChooseAction(GameState state, List<GameAction> legalActions, PlayerSide actorSide)
    {
        if (legalActions == null || legalActions.Count == 0) return null;
        if (legalActions.Count == 1) return legalActions[0];
        if (net == null) return legalActions[rng.Next(legalActions.Count)];   // fallback

        float[] feat = StateEncoder.EncodeFor(net.Features, state, actorSide);

        int[] idxs = new int[legalActions.Count];
        for (int i = 0; i < legalActions.Count; i++)
            idxs[i] = ActionEncoding.IndexOf(legalActions[i], state, actorSide);

        float[] logits = new float[net.Actions];
        net.ForwardLegal(feat, idxs, idxs.Length, logits, out float _);

        double[] sc = new double[legalActions.Count];
        double max = double.NegativeInfinity;
        for (int i = 0; i < legalActions.Count; i++)
        {
            sc[i] = idxs[i] >= 0 ? logits[idxs[i]] : double.NegativeInfinity;
            if (sc[i] > max) max = sc[i];
        }

        if (!sample)
        {
            int bi = 0;
            for (int i = 1; i < sc.Length; i++) if (sc[i] > sc[bi]) bi = i;
            return legalActions[bi];
        }

        double sum = 0;
        for (int i = 0; i < sc.Length; i++) { sc[i] = Math.Exp(sc[i] - max); sum += sc[i]; }
        double r = rng.NextDouble() * sum, acc = 0;
        for (int i = 0; i < sc.Length; i++) { acc += sc[i]; if (r <= acc) return legalActions[i]; }
        return legalActions[legalActions.Count - 1];
    }
}
