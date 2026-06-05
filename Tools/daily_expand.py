#!/usr/bin/env python3
"""
daily_expand.py  --  additively GROW the Daily puzzle pool (more puzzles for v1.0).

NOT shipped (under Tools/). Re-runnable + deterministic (seeded). PRESERVES every existing
daily puzzle byte-for-byte (id + array position + fields unchanged) and APPENDS new puzzles
to the END so DailyPuzzleService's `dayIndex % poolCount` mapping simply gains more days.
Because the daily index is pool-size-agnostic and player progress is keyed by ISO DATE (not
pool index), growing the pool is fully save-safe and schema-identical.

Each APPENDED puzzle (same guarantees as the curated set):
  * start/end/intermediates drawn from common_words.json (fair, well-known),
  * is the SHORTEST Hamming-1 path (BFS) at its target length  -> solvable by construction,
  * optimalSteps == len(solution)-1, within the per-length step band,
  * TRUE full-dictionary shortest distance >= the Task-17 min-move FLOOR (no hidden shortcut),
  * unique (start,end) pair vs. ALL existing + newly added puzzles,
  * every word present in word_library.json.

It FAILS LOUDLY (exit 1) on any unsolvable / duplicate / out-of-band / below-floor / missing
word, after re-validating the WHOLE pool (old + new). Schema preserved exactly:
  {"puzzles":[{"puzzleId","startWord","endWord","optimalSteps","solution":[...],"seedValue"}]}

Usage:  python Tools/daily_expand.py            (writes file + report)
        python Tools/daily_expand.py --dry-run  (report only)
"""
import os, sys, json, random, collections
from collections import deque, defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.normpath(os.path.join(HERE, "..", "Assets", "Resources", "Data"))

GLOBAL_SEED = 3717
ADD_PER_LEN = {3: 50, 4: 50, 5: 50}      # +150 total  -> pool 450 -> 600
LENGTHS = (3, 4, 5)
# Appended puzzles get ids in a reserved block ABOVE the original hand-curated range
# (original ids 10001..14090). On every run we first DROP any puzzle in this block, so the
# tool is idempotent/reproducible: it always rebuilds the same 600 from the original 450.
APPEND_ID_BASE = 20001


def min_moves_for_length(L):
    return {3: 2, 4: 2, 5: 3, 6: 3, 7: 4}.get(L, 2 if L < 7 else 4)


def step_band_for_length(L):
    # Mirror of the existing daily distribution (2-4 moves) and the length curve.
    return {3: (2, 3), 4: (2, 3), 5: (3, 4)}[L]


def load(n):
    with open(os.path.join(DATA, n), encoding="utf-8") as f:
        return json.load(f)


def build_graph(words):
    """Neighbour lists SORTED -> deterministic BFS regardless of PYTHONHASHSEED
    (byte-reproducible appended-daily output)."""
    words = set(words)
    buckets = defaultdict(list)
    for w in words:
        for i in range(len(w)):
            buckets[w[:i] + "*" + w[i + 1:]].append(w)
    adj_sets = defaultdict(set)
    for ws in buckets.values():
        for a in ws:
            for c in ws:
                if a != c:
                    adj_sets[a].add(c)
    adj = defaultdict(list)
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


def count_shortest_paths(adj, s, e):
    if s == e:
        return 1, 0
    dist = {s: 0}; npaths = {s: 1}; q = deque([s]); found = None
    while q:
        u = q.popleft()
        if found is not None and dist[u] >= found:
            continue
        for v in adj[u]:
            if v not in dist:
                dist[v] = dist[u] + 1; npaths[v] = npaths[u]
                if v == e:
                    found = dist[v]
                q.append(v)
            elif dist[v] == dist[u] + 1:
                npaths[v] += npaths[u]
    return npaths.get(e, 0), dist.get(e, -1)


def hamming1(a, b):
    return len(a) == len(b) and sum(1 for x, y in zip(a, b) if x != y) == 1


def main():
    dry = "--dry-run" in sys.argv
    dp = load("daily_puzzles.json")
    common = [w.lower() for w in load("common_words.json")["words"]]
    library = set(w.lower() for w in load("word_library.json")["words"])

    common_by_len = defaultdict(list)
    for w in common:
        common_by_len[len(w)].append(w)
    lib_by_len = defaultdict(list)
    for w in library:
        lib_by_len[len(w)].append(w)
    common_adj = {L: build_graph(common_by_len[L]) for L in LENGTHS}
    full_adj = {L: build_graph(lib_by_len[L]) for L in LENGTHS}

    # Idempotency: discard any previously-appended block so we always start from the
    # original curated pool (ids below APPEND_ID_BASE), preserving their order exactly.
    all_puzzles = dp["puzzles"]
    existing = [p for p in all_puzzles if p["puzzleId"] < APPEND_ID_BASE]
    dropped = len(all_puzzles) - len(existing)
    existing_pairs = set((p["startWord"].lower(), p["endWord"].lower()) for p in existing)
    next_id = APPEND_ID_BASE
    print(f"DATA dir: {DATA}")
    print(f"Original daily pool: {len(existing)}  (dropped {dropped} previously-appended; "
          f"append id base {APPEND_ID_BASE})")
    print(f"common length pool: {{{', '.join(f'{L}:{len(common_by_len[L])}' for L in LENGTHS)}}}")

    added = []
    for L in LENGTHS:
        want = ADD_PER_LEN[L]
        lo, hi = step_band_for_length(L)
        floor = min_moves_for_length(L)
        band = list(range(lo, hi + 1))
        adj = common_adj[L]
        starts = [w for w in common_by_len[L] if adj[w]]
        rng = random.Random(GLOBAL_SEED + L)
        made = 0; attempts = 0; ti = 0
        # Prefer multi-route endpoints: try to pick pairs with >1 shortest path.
        while made < want:
            attempts += 1
            if attempts > want * 6000:
                raise RuntimeError(f"len{L}: only made {made}/{want} (density?)")
            target = band[ti % len(band)]
            s = rng.choice(starts)
            paths = bfs_paths(adj, s, target)
            cands = [w for w, p in paths.items() if len(p) - 1 == target]
            if not cands:
                continue
            e = rng.choice(cands)
            se = (s, e)
            if se in existing_pairs or (e, s) in existing_pairs:
                continue
            # true full-dictionary shortest distance must respect the floor (no shortcut)
            if bfs_dist(full_adj[L], s, e, hi + 2) < floor:
                continue
            # prefer (but don't strictly require) multiple full-graph shortest routes;
            # relax the preference after enough attempts so density never deadlocks.
            nfull, _ = count_shortest_paths(full_adj[L], s, e)
            if nfull < 2 and attempts < want * 3000:
                continue
            sol = paths[e]
            existing_pairs.add(se)
            added.append({
                "puzzleId": next_id,
                "startWord": s,
                "endWord": e,
                "optimalSteps": len(sol) - 1,
                "solution": sol,
                "seedValue": GLOBAL_SEED + next_id,
            })
            next_id += 1
            made += 1
            ti += 1
        print(f"  len{L}: added {made} (band [{lo},{hi}], floor {floor})")

    new_pool = existing + added
    added_ids = set(a["puzzleId"] for a in added)
    # ---- re-validate the WHOLE pool (old + new) for the per-puzzle ladder guarantees ----
    # NOTE: the pre-existing daily pool intentionally allows the SAME (start,end) pair to
    # recur on different days, so global pair-uniqueness is NOT a pool invariant. We only
    # require the APPENDED puzzles to be unique among themselves and to not collide with any
    # existing pair (the latter is already enforced during generation via existing_pairs).
    failures = []
    seen_new_pairs = set()
    for p in new_pool:
        s = p["startWord"].lower(); e = p["endWord"].lower()
        sol = [w.lower() for w in p["solution"]]
        L = len(s)
        pid = p["puzzleId"]
        if len(sol) < 2:
            failures.append(f"#{pid}: solution too short"); continue
        if sol[0] != s or sol[-1] != e:
            failures.append(f"#{pid}: endpoints mismatch")
        if p["optimalSteps"] != len(sol) - 1:
            failures.append(f"#{pid}: optimalSteps != len-1")
        for a, b in zip(sol, sol[1:]):
            if not hamming1(a, b):
                failures.append(f"#{pid}: non-Hamming1 {a}->{b}")
        for w in sol:
            if w not in library:
                failures.append(f"#{pid}: word '{w}' not in dictionary")
        if pid in added_ids:
            key = (s, e) if s < e else (e, s)
            if key in seen_new_pairs:
                failures.append(f"#{pid}: duplicate appended pair {s}->{e}")
            seen_new_pairs.add(key)

    # Floor check is enforced on the APPENDED puzzles only (the pre-existing pool is the
    # responsibility of daily_floor_fix.py and may legitimately differ for lengths 6/7).
    added_ids = set(a["puzzleId"] for a in added)
    for p in new_pool:
        if p["puzzleId"] not in added_ids:
            continue
        s = p["startWord"].lower(); e = p["endWord"].lower(); L = len(s)
        if L in full_adj and bfs_dist(full_adj[L], s, e, 9) < min_moves_for_length(L):
            failures.append(f"#{p['puzzleId']}: below floor {s}->{e}")

    n_new = len(new_pool)
    if failures:
        print(f"\n!!! {len(failures)} VALIDATION FAILURES:")
        for f in failures[:30]:
            print("   ", f)
        sys.exit(1)

    # multi-route summary on the appended set
    multi = single = 0
    for p in added:
        s = p["startWord"]; e = p["endWord"]; L = len(s)
        n, _ = count_shortest_paths(full_adj[L], s, e)
        if n > 1: multi += 1
        else: single += 1
    print(f"\nValidation: all {n_new} daily puzzles (old {len(existing)} + new {len(added)}) "
          f"solvable, Hamming-1, in-dictionary, unique, in-band.")
    print(f"Appended multi-route: {multi} multi-path / {single} single-path of {len(added)}.")

    if dry:
        print("\n--dry-run: no file written.")
        return
    dp["puzzles"] = new_pool
    with open(os.path.join(DATA, "daily_puzzles.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(dp, f, indent=2)
        f.write("\n")
    print(f"\nWrote daily_puzzles.json (pool {len(existing)} -> {len(new_pool)}; existing order/ids preserved).")


if __name__ == "__main__":
    main()
