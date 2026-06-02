#!/usr/bin/env python3
"""
puzzleshow_build.py  --  TASK 15A reproducible Puzzle Show tier generator + validator.

NOT shipped (lives under Tools/, outside Assets/). Re-runnable and deterministic
(seeded RNG): regenerates Assets/Resources/Data/tier_definitions.json as
7 tiers x 50 = 350 curated word-ladder puzzles following a length/step difficulty curve.

Every puzzle:
  * draws start/end (and intermediates) from common_words.json (fair, well-known)
  * is the SHORTEST Hamming-1 path (BFS) at the intended length  -> solvable by construction
  * has optimalSteps == len(solution) - 1, within the tier's step band
  * is unique within its tier (no repeated start/end pair)
  * uses only words present in word_library.json (the validator dictionary)

The tool FAILS LOUDLY (exit 1) on any unsolvable / duplicate / out-of-length / out-of-band
entry, and prints a per-tier report. Schema preserved exactly for the JsonUtility loaders
(GameBootstrap.LoadTierData + PuzzleLibraryScreen):
  {"tiers":[{"tierId","isUnlocked","unlockedTimestamp",
             "puzzles":[{"puzzleId","startWord","endWord","optimalSteps","solution":[...],"seedValue"}]}]}

Usage:  python Tools/puzzleshow_build.py            (writes file + report)
        python Tools/puzzleshow_build.py --dry-run  (report only)
"""
import os, sys, json, random, collections

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.normpath(os.path.join(HERE, "..", "Assets", "Resources", "Data"))

PUZZLES_PER_TIER = 50
GLOBAL_SEED = 1515

# (tierId): (lengths, min_steps, max_steps). Mixed-length tiers split evenly.
# Density verified (Task 15 PLAN): common-word ladders exist in abundance at every (len, step).
TIER_CURVE = {
    1: ([3],    1, 2),
    2: ([4],    2, 3),
    3: ([5],    2, 3),
    4: ([5, 6], 3, 4),
    5: ([6],    4, 5),
    6: ([6, 7], 4, 6),
    7: ([7],    4, 8),   # hardest — skewed toward 6/7/8-step ladders
}


def load(name):
    with open(os.path.join(DATA, name), encoding="utf-8") as f:
        return json.load(f)


def build_graph(words):
    """Hamming-1 adjacency via wildcard bucketing (same shape as WordGraph)."""
    words = set(words)
    buckets = collections.defaultdict(list)
    for w in words:
        for i in range(len(w)):
            buckets[w[:i] + "*" + w[i + 1:]].append(w)
    adj = collections.defaultdict(set)
    for ws in buckets.values():
        for a in ws:
            for c in ws:
                if a != c:
                    adj[a].add(c)
    return adj


def bfs_paths(adj, start, max_depth):
    """BFS from start; return {word: shortest_path_list} up to max_depth."""
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


def gen_tier(tier_id, lengths, lo, hi, common_by_len, rng):
    """Generate PUZZLES_PER_TIER unique shortest-path ladders for one tier."""
    # Spread targets evenly across the band so harder tiers include their longest ladders.
    band = list(range(lo, hi + 1))
    # split count across the tier's lengths
    per_len = [PUZZLES_PER_TIER // len(lengths)] * len(lengths)
    for i in range(PUZZLES_PER_TIER - sum(per_len)):
        per_len[i] += 1

    out = []
    used_pairs = set()
    for li, L in enumerate(lengths):
        words = common_by_len[L]
        adj = build_graph(words)
        starts = [w for w in words if adj[w]]
        want = per_len[li]
        made = 0
        attempts = 0
        ti = 0
        while made < want:
            attempts += 1
            if attempts > want * 4000:
                raise RuntimeError(f"Tier {tier_id} len{L}: only made {made}/{want} (density?)")
            target = band[ti % len(band)]
            s = rng.choice(starts)
            paths = bfs_paths(adj, s, target)
            cands = [w for w, p in paths.items() if len(p) - 1 == target]
            if not cands:
                continue
            e = rng.choice(cands)
            key = (s, e)
            if key in used_pairs or (e, s) in used_pairs:
                continue
            sol = paths[e]
            used_pairs.add(key)
            out.append((s, e, sol))
            made += 1
            ti += 1
    rng.shuffle(out)
    return out


def main():
    dry = "--dry-run" in sys.argv
    common = [w.lower() for w in load("common_words.json")["words"]]
    library = set(w.lower() for w in load("word_library.json")["words"])
    common_by_len = collections.defaultdict(list)
    for w in common:
        common_by_len[len(w)].append(w)

    print(f"DATA dir: {DATA}")
    print("common length pool:", {L: len(common_by_len[L]) for L in (3, 4, 5, 6, 7)})

    tiers_json = []
    failures = []
    report = []
    pid = 0
    for tier_id in range(1, 8):
        lengths, lo, hi = TIER_CURVE[tier_id]
        rng = random.Random(GLOBAL_SEED + tier_id)
        puzzles = gen_tier(tier_id, lengths, lo, hi, common_by_len, rng)

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
        report.append(f"  Tier {tier_id}: len{lengths} band[{lo},{hi}]  n={len(pj)}  step-dist={dist}")

    print("\nGenerated tiers:")
    print("\n".join(report))

    # cross-tier: min steps non-decreasing
    min_steps = [min(p["optimalSteps"] for p in t["puzzles"]) for t in tiers_json]
    print("\nper-tier MIN steps:", min_steps, "(must be non-decreasing)")
    for i in range(1, len(min_steps)):
        if min_steps[i] < min_steps[i - 1]:
            failures.append(f"min steps decreased at tier {i+1}: {min_steps}")

    total = sum(len(t["puzzles"]) for t in tiers_json)
    print(f"\nTOTAL puzzles: {total} (expect 350)")
    if total != 350 or any(len(t["puzzles"]) != 50 for t in tiers_json):
        failures.append("tier/puzzle counts wrong")

    if failures:
        print(f"\n!!! {len(failures)} VALIDATION FAILURES:")
        for f in failures[:30]:
            print("   ", f)
        sys.exit(1)
    print("\nValidation: all 350 puzzles solvable, Hamming-1, in-dictionary, unique, in-band.")

    if dry:
        print("\n--dry-run: no file written.")
        return
    out = {"tiers": tiers_json}
    with open(os.path.join(DATA, "tier_definitions.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(out, f, indent=2)
        f.write("\n")
    print("\nWrote tier_definitions.json (7 tiers x 50 = 350).")


if __name__ == "__main__":
    main()
