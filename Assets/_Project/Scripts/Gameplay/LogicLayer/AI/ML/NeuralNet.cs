using System;
using System.IO;

/// <summary>
/// Tiny in-engine forward pass for the distilled policy+value MLP
/// (400→Hidden→Hidden→[policy logits, value]). Pure C#, no inference runtime:
/// weights are loaded from the flat blob written by ml/train.py:export_weights.
/// Read-only after construction → safe to share across MCTS threads.
/// </summary>
public sealed class NeuralNet
{
    private const int Magic = 0x314E4D45;

    public int Features { get; }
    public int Hidden { get; }
    public int Actions { get; }

    private readonly float[] w0, b0, w1, b1, wp, bp, wv;
    private readonly float bv;

    /// <summary>Parses the weight blob (the bytes of the .bytes Resource).</summary>
    public NeuralNet(byte[] data)
    {
        using var r = new BinaryReader(new MemoryStream(data));
        if (r.ReadInt32() != Magic) throw new InvalidDataException("NeuralNet: bad magic");
        r.ReadInt32();                       // version
        Features = r.ReadInt32();
        Hidden = r.ReadInt32();
        Actions = r.ReadInt32();
        w0 = Read(r, Hidden * Features); b0 = Read(r, Hidden);
        w1 = Read(r, Hidden * Hidden); b1 = Read(r, Hidden);
        wp = Read(r, Actions * Hidden); bp = Read(r, Actions);
        wv = Read(r, Hidden); bv = Read(r, 1)[0];
    }

    private static float[] Read(BinaryReader r, int n)
    {
        float[] a = new float[n];
        for (int i = 0; i < n; i++) a[i] = r.ReadSingle();
        return a;
    }

    // Per-thread scratch — avoids per-call allocation under parallel MCTS.
    [ThreadStatic] private static float[] _h0;
    [ThreadStatic] private static float[] _h1;
    [ThreadStatic] private static int[] _nz;

    // Shared trunk: sparse layer-0 + dense layer-1 → h1 (ThreadStatic scratch). Reused by both
    // the full Forward and the legal-only ForwardLegal.
    private float[] Trunk(float[] x)
    {
        float[] h0 = _h0; if (h0 == null || h0.Length < Hidden) _h0 = h0 = new float[Hidden];
        float[] h1 = _h1; if (h1 == null || h1.Length < Hidden) _h1 = h1 = new float[Hidden];
        int[] nz = _nz; if (nz == null || nz.Length < Features) _nz = nz = new int[Features];

        int m = 0;
        for (int i = 0; i < Features; i++) if (x[i] != 0f) nz[m++] = i;

        for (int j = 0; j < Hidden; j++)
        {
            float s = b0[j]; int off = j * Features;
            for (int t = 0; t < m; t++) { int i = nz[t]; s += w0[off + i] * x[i]; }
            h0[j] = s > 0f ? s : 0f;                 // ReLU
        }
        for (int j = 0; j < Hidden; j++)
        {
            float s = b1[j]; int off = j * Hidden;
            for (int i = 0; i < Hidden; i++) s += w1[off + i] * h0[i];
            h1[j] = s > 0f ? s : 0f;                 // ReLU
        }
        return h1;
    }

    private float ValueFrom(float[] h1)
    {
        float v = bv;
        for (int i = 0; i < Hidden; i++) v += wv[i] * h1[i];
        return (float)Math.Tanh(v);
    }

    /// <summary>Full forward: all <see cref="Actions"/> policy logits + value. Fallback path.</summary>
    public void Forward(float[] x, float[] policyOut, out float value)
    {
        float[] h1 = Trunk(x);
        for (int k = 0; k < Actions; k++)
        {
            float s = bp[k]; int off = k * Hidden;
            for (int i = 0; i < Hidden; i++) s += wp[off + i] * h1[i];
            policyOut[k] = s;
        }
        value = ValueFrom(h1);
    }

    /// <summary>
    /// Value + policy logits computed ONLY at the given flat action indices (<paramref name="idx"/>[0..nIdx);
    /// entries &lt; 0 are skipped). Other positions of <paramref name="policyOut"/> are left untouched.
    /// Avoids the full Actions×Hidden policy matmul when few actions are legal.
    /// </summary>
    public void ForwardLegal(float[] x, int[] idx, int nIdx, float[] policyOut, out float value)
    {
        float[] h1 = Trunk(x);
        for (int t = 0; t < nIdx; t++)
        {
            int k = idx[t];
            if (k < 0) continue;
            float s = bp[k]; int off = k * Hidden;
            for (int i = 0; i < Hidden; i++) s += wp[off + i] * h1[i];
            policyOut[k] = s;
        }
        value = ValueFrom(h1);
    }
}
