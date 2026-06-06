#!/usr/bin/env python3
"""
puzzleshow_build.py  --  reproducible Puzzle Show tier generator + validator.

NOT shipped (lives under Tools/, outside Assets/). Re-runnable and deterministic
(seeded RNG): regenerates Assets/Resources/Data/tier_definitions.json as
7 tiers x 100 = 700 curated word-ladder puzzles following a length/step difficulty curve.

Every puzzle:
  * draws start/end (and intermediates) from common_words.json (fair, well-known)
  * stores a SHORTEST Hamming-1 path whose length equals the TRUE full-dictionary
    shortest distance  -> solution length == optimalSteps == true shortest (no shortcut)
  * has optimalSteps within the tier's step band, and >= the per-length min-move floor
  * is unique within its tier (no repeated start/end pair)
  * uses only words present in word_library.json (the validator dictionary)
  * has >= 2 DISTINCT optimal-length routes through the FULL dictionary graph
    (the "multiple ways to solve" guarantee; single-route pairs are flagged & skipped)

The tool FAILS LOUDLY (exit 1) on any unsolvable / duplicate / out-of-length / out-of-band /
single-route entry, and prints a per-tier report incl. how many single-route candidates were
rejected/replaced. Schema preserved exactly for the JsonUtility loaders
(GameBootstrap.LoadTierData + PuzzleLibraryScreen):
  {"tiers":[{"tierId","isUnlocked","unlockedTimestamp",
             "puzzles":[{"puzzleId","startWord","endWord","optimalSteps","solution":[...],"seedValue"}]}]}

Usage:  python Tools/puzzleshow_build.py            (writes file + report)
        python Tools/puzzleshow_build.py --dry-run  (report only)
"""
import os, sys, json, random, collections
from collections import deque

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.normpath(os.path.join(HERE, "..", "Assets", "Resources", "Data"))

PUZZLES_PER_TIER = 100
TIER_TOTAL = 700
GLOBAL_SEED = 1515

# Bounded enumeration cap for the distinct-optimal-route counter. We only need to prove
# ">= 2", so any count is clamped here — keeps the DAG path-DP from producing huge integers
# on highly-connected short words without changing the >=2 decision.
ROUTE_COUNT_CAP = 256

# (tierId): (lengths, min_steps, max_steps). Mixed-length tiers split evenly.
# Density verified: common-word ladders exist in abundance at every (len, step).
# Every tier's MIN meets max(2, length-curve): 3,4-letter >=2; 5,6-letter >=3; 7-letter >=4.
TIER_CURVE = {
    1: ([3],    2, 3),
    2: ([4],    2, 3),
    3: ([5],    3, 4),
    4: ([5, 6], 3, 4),
    5: ([6],    4, 5),
    6: ([6, 7], 4, 6),
    7: ([7],    4, 8),   # hardest — skewed toward 6/7/8-step ladders
}

# Per-tier endpoint-reuse cap (how many times a single word may serve as a start OR end across
# the tier). Tier 1 has only ~890 three-letter words, so without a cap the 100 puzzles cluster on
# the same common letter-swap families (cat/cot/cog ...). Cap=2 forces distinct start/end families
# while still leaving ample headroom (100 puzzles -> >=100 distinct endpoint words out of 890).
# Other tiers have thousands of words per length and stay uncapped.
ENDPOINT_USE_CAP = {1: 2}


# Task 17 — length → minimum TRUE-shortest moves (mirror of BalanceConfig.MinMovesForLength).
def min_moves_for_length(L):
    return {3: 2, 4: 2, 5: 3, 6: 3, 7: 4}.get(L, 2 if L < 7 else 4)


def load(name):
    with open(os.path.join(DATA, name), encoding="utf-8") as f:
        return json.load(f)


def build_graph(words):
    """Hamming-1 adjacency via wildcard bucketing (same shape as WordGraph).
    Neighbour lists are SORTED so BFS traversal order is fully deterministic and
    independent of Python's per-process string-hash randomization (PYTHONHASHSEED) —
    required for byte-reproducible tier output across runs."""
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
    """BFS from start; return {word: shortest_path_list} up to max_depth (common graph)."""
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


def hamming1(a, b):
    return len(a) == len(b) and sum(1 for x, y in zip(a, b) if x != y) == 1


def count_optimal_routes(adj, s, e, cap=ROUTE_COUNT_CAP):
    """Over the FULL-dictionary graph, return (num_distinct_shortest_paths, shortest_distance).

    Single bounded BFS + DAG path-count DP: each node records its shortest distance and the
    number of shortest paths reaching it (clamped to `cap`). Exploration stops past the layer
    in which `e` is first found, so it never explodes. distance == -1 if e is unreachable.
    'Distinct' = different word sequences of optimal length (standard shortest-path count)."""
    if s == e:
        return 1, 0
    dist = {s: 0}
    npaths = {s: 1}
    q = deque([s])
    target = None
    while q:
        u = q.popleft()
        du = dist[u]
        if target is not None and du >= target:
            continue
        for v in adj[u]:
            if v not in dist:
                dist[v] = du + 1
                npaths[v] = npaths[u]
                if v == e:
                    target = dist[v]
                q.append(v)
            elif dist[v] == du + 1:
                npaths[v] = min(cap, npaths[v] + npaths[u])
    return npaths.get(e, 0), dist.get(e, -1)


def gen_tier(tier_id, lengths, lo, hi, common_by_len, full_by_len, rng):
    """Generate PUZZLES_PER_TIER unique shortest-path ladders for one tier.

    Returns (puzzles, single_route_rejected) where single_route_rejected counts otherwise-valid
    candidate pairs discarded purely because they had only ONE optimal route (i.e. would have
    been forced single-path puzzles), so the caller can report how many were flagged/replaced."""
    band = list(range(lo, hi + 1))
    # split count across the tier's lengths
    per_len = [PUZZLES_PER_TIER // len(lengths)] * len(lengths)
    for i in range(PUZZLES_PER_TIER - sum(per_len)):
        per_len[i] += 1

    ep_cap = ENDPOINT_USE_CAP.get(tier_id)        # None => uncapped
    endpoint_use = collections.Counter()
    single_route_rejected = 0

    out = []
    used_pairs = set()
    for li, L in enumerate(lengths):
        words = common_by_len[L]
        adj = build_graph(words)
        full_adj = full_by_len[L]                 # full-dictionary graph for true-distance + routes
        floor = min_moves_for_length(L)
        starts = [w for w in words if adj[w]]
        want = per_len[li]
        made = 0
        attempts = 0
        ti = 0
        while made < want:
            attempts += 1
            if attempts > want * 8000:
                raise RuntimeError(
                    f"Tier {tier_id} len{L}: only made {made}/{want} "
                    f"(density/route/variety constraints too tight?)")
            target = band[ti % len(band)]
            s = rng.choice(starts)
            if ep_cap is not None and endpoint_use[s] >= ep_cap:
                continue
            paths = bfs_paths(adj, s, target)
            cands = [w for w, p in paths.items() if len(p) - 1 == target]
            if ep_cap is not None:
                cands = [w for w in cands if endpoint_use[w] < ep_cap and w != s]
            if not cands:
                continue
            e = rng.choice(cands)
            key = (s, e)
            if key in used_pairs or (e, s) in used_pairs:
                continue
            # FULL-dictionary truth: distinct optimal routes + true shortest distance in ONE pass.
            nroutes, true_d = count_optimal_routes(full_adj, s, e)
            # Stored solution must BE a true optimal path: its length (target) must equal the
            # true full-dictionary shortest distance (reject "looks like N, really fewer" shortcuts).
            if true_d != target:
                continue
            if true_d < floor:
                continue
            # The new GUARANTEE: every puzzle must have >= 2 distinct optimal-length routes.
            if nroutes < 2:
                single_route_rejected += 1
                continue
            sol = paths[e]                        # canonical path: deterministic common-graph BFS
            used_pairs.add(key)
            endpoint_use[s] += 1
            endpoint_use[e] += 1
            out.append((s, e, sol))
            made += 1
            ti += 1
    rng.shuffle(out)
    return out, single_route_rejected


def main():
    dry = "--dry-run" in sys.argv
    common = [w.lower() for w in load("common_words.json")["words"]]
    library = set(w.lower() for w in load("word_library.json")["words"])
    common_by_len = collections.defaultdict(list)
    for w in common:
        common_by_len[len(w)].append(w)

    # Full-dictionary graphs per length — used to verify TRUE shortest distance + count routes.
    lib_by_len = collections.defaultdict(list)
    for w in library:
        lib_by_len[len(w)].append(w)
    full_by_len = {L: build_graph(lib_by_len[L]) for L in (3, 4, 5, 6, 7)}

    print(f"DATA dir: {DATA}")
    print("common length pool:", {L: len(common_by_len[L]) for L in (3, 4, 5, 6, 7)})

    tiers_json = []
    failures = []
    report = []
    total_single_rejected = 0
    pid = 0
    for tier_id in range(1, 8):
        lengths, lo, hi = TIER_CURVE[tier_id]
        rng = random.Random(GLOBAL_SEED + tier_id)
        puzzles, single_rej = gen_tier(tier_id, lengths, lo, hi, common_by_len, full_by_len, rng)
        total_single_rejected += single_rej

        pj = []
        steps_seen = []
        seen_pairs = set()
        for (s, e, sol) in puzzles:
            pid += 1
            steps = len(sol) - 1
            steps_seen.append(steps)
            # --- validation ---
            if (s, e) in seen_pairs:
                failures.append(f"tier{tier_id} dup pair {s}->{e}")
            seen_pairs.add((s, e))
            if not (lo <= steps <= hi):
                failures.append(f"tier{tier_id} #{pid} steps {steps} out of band [{lo},{hi}]")
            if len(s) not in lengths or len(e) not in lengths:
                failures.append(f"tier{tier_id} #{pid} length not in {lengths}: {s}->{e}")
            if sol[0] != s or sol[-1] != e:
                failures.append(f"tier{tier_id} #{pid} endpoints mismatch")
            for a, b in zip(sol, sol[1:]):
                if not hamming1(a, b):
                    failures.append(f"tier{tier_id} #{pid} non-Hamming1 {a}->{b}")
            for w in sol:
                if w not in library:
                    failures.append(f"tier{tier_id} #{pid} word '{w}' not in dictionary")
            # re-confirm the >=2-route + true-distance guarantee on the FULL graph
            nroutes, true_d = count_optimal_routes(full_by_len[len(s)], s, e)
            if true_d != steps:
                failures.append(f"tier{tier_id} #{pid} optimalSteps {steps} != true dist {true_d}")
            if nroutes < 2:
                failures.append(f"tier{tier_id} #{pid} only {nroutes} optimal route(s) (need >=2)")
            pj.append({
                "puzzleId": pid,
                "startWord": s,
                "endWord": e,
                "optimalSteps": steps,
                "solution": sol,
                "seedValue": GLOBAL_SEED + pid,
            })
        tiers_json.append({
            "tierId": tier_id,
            "isUnlocked": tier_id == 1,
            "unlockedTimestamp": 0,
            "puzzles": pj,
        })
        dist = dict(sorted(collections.Counter(steps_seen).items()))
        report.append(f"  Tier {tier_id}: len{lengths} band[{lo},{hi}]  n={len(pj)}  "
                      f"step-dist={dist}  single-route-rejected={single_rej}")

    print("\nGenerated tiers:")
    print("\n".join(report))

    # cross-tier: min steps non-decreasing
    min_steps = [min(p["optimalSteps"] for p in t["puzzles"]) for t in tiers_json]
    print("\nper-tier MIN steps:", min_steps, "(must be non-decreasing)")
    for i in range(1, len(min_steps)):
        if min_steps[i] < min_steps[i - 1]:
            failures.append(f"min steps decreased at tier {i+1}: {min_steps}")

    total = sum(len(t["puzzles"]) for t in tiers_json)
    print(f"\nTOTAL puzzles: {total} (expect {TIER_TOTAL})")
    print(f"Single-route candidates flagged & replaced across all tiers: {total_single_rejected}")
    if total != TIER_TOTAL or any(len(t["puzzles"]) != PUZZLES_PER_TIER for t in tiers_json):
        failures.append("tier/puzzle counts wrong")

    if failures:
        print(f"\n!!! {len(failures)} VALIDATION FAILURES:")
        for f in failures[:30]:
            print("   ", f)
        sys.exit(1)
    print(f"\nValidation: all {TIER_TOTAL} puzzles solvable, Hamming-1, in-dictionary, unique, "
          f"in-band, true-shortest, and >=2 optimal routes.")

    if dry:
        print("\n--dry-run: no file written.")
        return
    out = {"tiers": tiers_json}
    with open(os.path.join(DATA, "tier_definitions.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(out, f, indent=2)
        f.write("\n")
    print(f"\nWrote tier_definitions.json (7 tiers x {PUZZLES_PER_TIER} = {TIER_TOTAL}).")


if __name__ == "__main__":
    main()
