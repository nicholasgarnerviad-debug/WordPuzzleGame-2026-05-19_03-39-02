#!/usr/bin/env python3
"""
daily_floor_fix.py  --  TASK 17C: fix Daily puzzles that are solvable in < 2 moves.

The hand-curated daily_puzzles.json contains a handful of puzzles whose start/end words
are one letter apart (true full-dictionary shortest path = 1 move) even though their stored
solution takes a detour. Those are 1-move puzzles to the player and violate the Task 17 floor.

DAILY REWORK (2026-06): every daily must now have optimalSteps >= 4 (FLOOR raised 2 -> 4) so
the par-scored mode never serves a trivial ladder. This tool ONLY replaces puzzles whose TRUE
full-dictionary shortest distance < FLOOR. For each, it generates a fresh same-length ladder
from the common-word pool with true shortest distance >= FLOOR (target 4-5 moves), keeping the
original puzzleId AND array position so ordering / DailyPuzzleService indexing is unchanged.
Compliant puzzles are left byte-for-byte intact. Deterministic (seeded per puzzleId) and
re-runnable (idempotent once compliant). Exits 1 LOUDLY if the pool cannot be filled.
DAILY pool only — tier/Classic/TimeAttack floors are untouched (BalanceConfig curve stands).

Usage:  python Tools/daily_floor_fix.py [--dry-run]
"""
import os, sys, json, random, collections

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.normpath(os.path.join(HERE, "..", "Assets", "Resources", "Data"))
FLOOR = 4   # daily-specific minimum (raised from the generic 2): par >= 4 on every daily


def min_moves_for_length(L):
    return {3: 2, 4: 2, 5: 3, 6: 3, 7: 4}.get(L, 2 if L < 7 else 4)


def target_moves_for_length(L):
    # Replacement ladders sit just above the daily floor (4-5 moves) regardless of length;
    # the per-length curve still applies where it is stricter (it never is, at FLOOR=4).
    m = max(FLOOR, min_moves_for_length(L))
    return [m, m + 1]


def load(n):
    with open(os.path.join(DATA, n), encoding="utf-8") as f:
        return json.load(f)


def build_graph(words):
    """Neighbour lists SORTED -> deterministic BFS regardless of PYTHONHASHSEED."""
    words = set(words)
    buckets = collections.defaultdict(list)
    for w in words:
        for i in range(len(w)):
            buckets[w[:i] + "*" + w[i + 1:]].append(w)
    adj_sets = collections.defaultdict(set)
    for ws in buckets.values():
        for a in ws:
            for c in ws:
                if a != c:
                    adj_sets[a].add(c)
    adj = collections.defaultdict(list)
    for k, vs in adj_sets.items():
        adj[k] = sorted(vs)
    return adj


def bfs_paths(adj, start, max_depth):
    paths = {start: [start]}
    frontier = [start]
    d = 0
    while frontier and d < max_depth:
        d += 1
        nxt = []
        for u in frontier:
            for v in adj[u]:
                if v not in paths:
                    paths[v] = paths[u] + [v]
                    nxt.append(v)
        frontier = nxt
    return paths


def bfs_dist(adj, s, e, cap):
    if s == e:
        return 0
    seen = {s}; frontier = [s]; d = 0
    while frontier and d < cap:
        d += 1; nxt = []
        for u in frontier:
            for v in adj[u]:
                if v == e:
                    return d
                if v not in seen:
                    seen.add(v); nxt.append(v)
        frontier = nxt
    return -1


def main():
    dry = "--dry-run" in sys.argv
    dp = load("daily_puzzles.json")
    common = [w.lower() for w in load("common_words.json")["words"]]
    library = set(w.lower() for w in load("word_library.json")["words"])

    common_by_len = collections.defaultdict(list)
    for w in common:
        common_by_len[len(w)].append(w)
    lib_by_len = collections.defaultdict(list)
    for w in library:
        lib_by_len[len(w)].append(w)
    full_by_len = {L: build_graph(lib_by_len[L]) for L in lib_by_len}
    common_adj = {L: build_graph(common_by_len[L]) for L in common_by_len}

    existing_pairs = set((p["startWord"], p["endWord"]) for p in dp["puzzles"])
    fixed = 0
    examples = []

    for p in dp["puzzles"]:
        s, e = p["startWord"].lower(), p["endWord"].lower()
        L = len(s)
        if L not in full_by_len:
            continue
        true_dist = bfs_dist(full_by_len[L], s, e, 8)
        # Replace when below the daily floor OR when the stored par is DISHONEST (the true
        # full-dictionary shortest differs from optimalSteps — par must be beatable-exactly).
        if true_dist >= FLOOR and true_dist == p["optimalSteps"]:
            continue  # already fine

        # Generate a replacement same-length ladder with true distance >= FLOOR.
        rng = random.Random(p["puzzleId"])
        adj = common_adj[L]
        starts = [w for w in common_by_len[L] if adj[w]]
        replacement = None
        for _ in range(20000):
            ns = rng.choice(starts)
            tgts = target_moves_for_length(L)
            target = tgts[rng.randrange(len(tgts))]
            paths = bfs_paths(adj, ns, target)
            cands = [w for w, pth in paths.items() if len(pth) - 1 == target]
            if not cands:
                continue
            ne = rng.choice(cands)
            if (ns, ne) in existing_pairs or (ne, ns) in existing_pairs:
                continue
            # HONEST PAR: the true full-dictionary shortest must EQUAL the stored solution
            # length (>= FLOOR by construction) — a full-graph shortcut would let players
            # beat par with non-common words, so reject anything shorter than the target.
            if bfs_dist(full_by_len[L], ns, ne, 8) != target:
                continue
            replacement = (ns, ne, paths[ne])
            break
        if replacement is None:
            print(f"  !! could not replace puzzle {p['puzzleId']} ({s}->{e})")
            continue

        ns, ne, sol = replacement
        if len(examples) < 8:
            examples.append(f"{p['puzzleId']}: {s}->{e} (< {FLOOR} moves)  =>  {ns}->{ne} ({len(sol)-1} moves)")
        existing_pairs.discard((s, e))
        existing_pairs.add((ns, ne))
        p["startWord"] = ns
        p["endWord"] = ne
        p["optimalSteps"] = len(sol) - 1
        p["solution"] = sol
        fixed += 1

    print(f"Daily puzzles fixed (were < {FLOOR} moves): {fixed}")
    for ex in examples:
        print("   ", ex)

    # Re-validate: nothing below the floor AND every stored par equals the true shortest.
    remaining = 0
    for p in dp["puzzles"]:
        td = bfs_dist(full_by_len[len(p['startWord'])], p["startWord"].lower(), p["endWord"].lower(), 8)
        if td < FLOOR or td != p["optimalSteps"]:
            remaining += 1
    print(f"Daily puzzles violating floor>={FLOOR} or honest-par after fix: {remaining}")
    if remaining:
        sys.exit(1)

    if dry:
        print("--dry-run: no file written.")
        return
    with open(os.path.join(DATA, "daily_puzzles.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(dp, f, indent=2)
        f.write("\n")
    print(f"Wrote daily_puzzles.json ({len(dp['puzzles'])} puzzles, order/ids preserved).")


if __name__ == "__main__":
    main()
