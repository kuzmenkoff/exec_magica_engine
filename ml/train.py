# Phase 1: distill MCTS self-play into a policy+value net. Importable + CLI.
import glob, json, os, argparse, struct
import numpy as np
import torch, torch.nn as nn, torch.nn.functional as F
from torch.utils.data import Dataset, DataLoader, random_split

FEATURES = 1216          # gen1 (v2); gen0 was 400
HAND, FIELD = 10, 7
OFF_END       = 0
OFF_PLAY_NONE = 1
OFF_PLAY_MYF  = OFF_PLAY_NONE + HAND
OFF_PLAY_OPF  = OFF_PLAY_MYF  + HAND * FIELD
OFF_PLAY_MYH  = OFF_PLAY_OPF  + HAND * FIELD
OFF_PLAY_OPH  = OFF_PLAY_MYH  + HAND
OFF_ATK_CARD  = OFF_PLAY_OPH  + HAND
OFF_ATK_HERO  = OFF_ATK_CARD  + FIELD * FIELD
ACTIONS       = OFF_ATK_HERO  + FIELD          # 227

def action_index(t, src, tz, ti):
    if t == 0: return OFF_END
    if t == 1:
        if not (0 <= src < HAND): return -1
        if tz == 0: return OFF_PLAY_NONE + src
        if tz == 1: return OFF_PLAY_MYF + src*FIELD + ti if 0 <= ti < FIELD else -1
        if tz == 2: return OFF_PLAY_OPF + src*FIELD + ti if 0 <= ti < FIELD else -1
        if tz == 3: return OFF_PLAY_MYH + src
        if tz == 4: return OFF_PLAY_OPH + src
        return -1
    if t == 2: return OFF_ATK_CARD + src*FIELD + ti if (0 <= src < FIELD and 0 <= ti < FIELD) else -1
    if t == 3: return OFF_ATK_HERO + src if 0 <= src < FIELD else -1
    return -1

class SelfPlay(Dataset):
    def __init__(self, data_dir, gamma=1.0, temp=1.0):
        X, PI, MASK, V = [], [], [], []
        files = glob.glob(os.path.join(data_dir, "*.jsonl"))
        if not files:
            raise FileNotFoundError(f"No .jsonl in {os.path.abspath(data_dir)}")
        for fn in files:
            for line in open(fn, encoding="utf-8"):
                if not line.strip(): continue
                r = json.loads(line)
                pi = np.zeros(ACTIONS, np.float32); mask = np.zeros(ACTIONS, np.float32); tot = 0
                for t, src, tz, ti, v in r["policy"]:
                    idx = action_index(t, src, tz, ti)
                    if 0 <= idx < ACTIONS:
                        mask[idx] = 1.0; pi[idx] += v; tot += v
                if tot <= 0: continue
                pi /= tot
                if temp != 1.0:
                    pi = pi ** (1.0 / temp); pi /= pi.sum()   # temp>1 = softer targets
                value = float(r["z"]) * (gamma ** int(r.get("t", 0)))
                X.append(np.asarray(r["features"], np.float32)); PI.append(pi)
                MASK.append(mask); V.append(np.float32(value))
        self.X, self.PI, self.MASK, self.V = map(np.stack, (X, PI, MASK, V))
        if not X:
            raise ValueError(f"No usable samples in {os.path.abspath(data_dir)} ({len(files)} files)")
        print(f"loaded {len(self.X)} samples from {len(files)} files")
    def __len__(self): return len(self.X)
    def __getitem__(self, i): return self.X[i], self.PI[i], self.MASK[i], self.V[i]

class Net(nn.Module):
    def __init__(self, h=512):
        super().__init__()
        self.trunk = nn.Sequential(nn.Linear(FEATURES, h), nn.ReLU(), nn.Linear(h, h), nn.ReLU())
        self.policy = nn.Linear(h, ACTIONS)
        self.value  = nn.Linear(h, 1)
    def forward(self, x):
        z = self.trunk(x)
        return self.policy(z), torch.tanh(self.value(z)).squeeze(-1)

def policy_loss(logits, mask, target):
    neg = torch.finfo(logits.dtype).min
    logits = torch.where(mask > 0, logits, torch.full_like(logits, neg))
    return -(target * F.log_softmax(logits, dim=1)).sum(dim=1).mean()

@torch.no_grad()
def evaluate(net, loader, dev):
    net.eval(); pl = vl = correct = n = 0
    for X, PI, M, V in loader:
        X, PI, M, V = (t.to(dev) for t in (X, PI, M, V))
        logits, val = net(X)
        pl += policy_loss(logits, M, PI).item() * len(X)
        vl += F.mse_loss(val, V, reduction="sum").item()
        masked = torch.where(M > 0, logits, torch.full_like(logits, float("-inf")))
        correct += (masked.argmax(1) == PI.argmax(1)).sum().item()  # top-1 = matches MCTS's move
        n += len(X)
    return pl / n, vl / n, correct / n

@torch.no_grad()
def value_accuracy(net, loader, dev=None):
    """Fraction of DECISIVE positions where the value head predicts the correct winner."""
    dev = dev or ("cuda" if torch.cuda.is_available() else "cpu"); net.eval().to(dev)
    correct = n = 0
    for X, PI, M, V in loader:
        X, V = X.to(dev), V.to(dev)
        _, val = net(X)
        dec = V != 0                                   # decisive games only (skip draws)
        correct += ((val.sign() == V.sign()) & dec).sum().item()
        n += dec.sum().item()
    return correct / n if n else float("nan")

def make_loaders(data_dir=None, gamma=1.0, batch=1024, val_frac=0.1, seed=0, ds=None):
    if ds is None: ds = SelfPlay(data_dir, gamma)
    n_val = max(1, int(len(ds) * val_frac)); n_tr = len(ds) - n_val
    tr, va = random_split(ds, [n_tr, n_val], generator=torch.Generator().manual_seed(seed))
    return (DataLoader(tr, batch_size=batch, shuffle=True, drop_last=True),
            DataLoader(va, batch_size=batch))

def train(net, train_loader, val_loader=None, dev=None, epochs=30, lr=1e-3, vw=1.0,
          best_path=None, verbose=True):
    dev = dev or ("cuda" if torch.cuda.is_available() else "cpu"); net.to(dev)
    opt = torch.optim.Adam(net.parameters(), lr=lr, weight_decay=1e-4)
    hist = {k: [] for k in ("train_policy", "train_value", "val_policy", "val_value", "val_acc")}
    best = float("inf")
    for ep in range(epochs):
        net.train(); tp = tv = nb = 0
        for X, PI, M, V in train_loader:
            X, PI, M, V = (t.to(dev) for t in (X, PI, M, V))
            logits, val = net(X)
            lp = policy_loss(logits, M, PI); lv = F.mse_loss(val, V)
            (lp + vw * lv).backward(); opt.step(); opt.zero_grad()
            tp += lp.item(); tv += lv.item(); nb += 1
        vp, vv, acc = evaluate(net, val_loader, dev) if val_loader else (float("nan"),) * 3
        for k, x in zip(hist, (tp/nb, tv/nb, vp, vv, acc)): hist[k].append(x)
        if verbose:
            print(f"ep {ep+1}/{epochs}  train P {tp/nb:.3f} V {tv/nb:.3f} | val P {vp:.3f} V {vv:.3f} acc {acc:.3f}")
        if best_path and vp < best:
            best = vp; os.makedirs(os.path.dirname(best_path), exist_ok=True)
            torch.save(net.state_dict(), best_path)
    return hist

def export_onnx(net, path, dev="cpu"):
    os.makedirs(os.path.dirname(path), exist_ok=True); net.eval().to(dev)
    torch.onnx.export(net, torch.zeros(1, FEATURES, device=dev), path,
        input_names=["features"], output_names=["policy_logits", "value"],
        dynamic_axes={"features": {0: "batch"}, "policy_logits": {0: "batch"}, "value": {0: "batch"}},
        opset_version=17)
    
MODEL_MAGIC = 0x314E4D45  # must match C# NeuralNet.Magic

def export_weights(net, path):
    """Dump weights as a flat little-endian float32 blob for the C# forward pass."""
    import os
    os.makedirs(os.path.dirname(path), exist_ok=True)
    net.eval().cpu(); sd = net.state_dict()
    g = lambda n: sd[n].numpy().astype('<f4')
    W0, b0 = g("trunk.0.weight"), g("trunk.0.bias")
    W1, b1 = g("trunk.2.weight"), g("trunk.2.bias")
    Wp, bp = g("policy.weight"),  g("policy.bias")
    Wv, bv = g("value.weight"),   g("value.bias")
    with open(path, "wb") as f:
        f.write(struct.pack("<5i", MODEL_MAGIC, 1, W0.shape[1], W0.shape[0], Wp.shape[0]))
        for a in (W0, b0, W1, b1, Wp, bp, Wv, bv):
            f.write(np.ascontiguousarray(a).tobytes())
    print(f"wrote {path}  features={W0.shape[1]} hidden={W0.shape[0]} actions={Wp.shape[0]}")

def dump_parity_sample(net, ds, i=0):
    import torch
    x = torch.from_numpy(ds.X[i:i+1])
    with torch.no_grad():
        logits, v = net(x)
    print("x[:8] =", ds.X[i][:8].tolist())
    print("logits[:5] =", logits[0][:5].tolist())
    print("value =", float(v))

def parity_synthetic(net):
    import numpy as np
    x = (np.arange(FEATURES, dtype=np.float32) % 10) * 0.1   # 0,.1,.2,...,.9,0,...
    with torch.no_grad():
        logits, v = net(torch.from_numpy(x[None]))
    print("logits[:5] =", logits[0][:5].tolist())
    print("value      =", float(v))

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", default="Runs/SelfPlayData/gen0")
    ap.add_argument("--out",  default="ml/models/gen0.onnx")
    ap.add_argument("--epochs", type=int, default=30)
    ap.add_argument("--batch",  type=int, default=1024)
    ap.add_argument("--lr",     type=float, default=1e-3)
    ap.add_argument("--gamma",  type=float, default=1.0)
    ap.add_argument("--vw",     type=float, default=1.0)
    ap.add_argument("--val",    type=float, default=0.1)
    a = ap.parse_args()
    tr, va = make_loaders(a.data, a.gamma, a.batch, a.val)
    net = Net()
    best = a.out.replace(".onnx", ".best.pt")
    train(net, tr, va, epochs=a.epochs, lr=a.lr, vw=a.vw, best_path=best)
    net.load_state_dict(torch.load(best)); export_onnx(net, a.out)
    print("exported", a.out)

if __name__ == "__main__":
    main()