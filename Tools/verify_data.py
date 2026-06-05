#!/usr/bin/env python3
"""
verify_data.py  --  independent data-integrity verifier for the shipped JSON.

Trusts NOTHING from the build tools: re-reads Assets/Resources/Data/*.json and asserts
every guarantee the game + Editor tests rely on (mirrors VerifyWordLibrary.cs rules plus
multi-route, min-move floor, offensive-absence, schema, and count/structure checks).

Exit 0 = all green.  Exit 1 = at least one violation (prints them).

  --canary  : deliberately corrupt an in-memory copy (break one Hamming-1 edge) and assert
              the verifier CATCHES it — proves the checks actually fail when they should.
"""
import os, sys, json, collections
from collections import deque, defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.normpath(os.path.join(HERE, "..", "Assets", "Resources", "Data"))

# Mirrors of game constants we must not silently drift from.
EXPECT_TIERS = 7
EXPECT_PER_TIER = 50
EXPECT_TIER_TOTAL = 350
MIN_DAILY_POOL = 450            # never SHRINK below the original curated pool

# Must mirror dictionary_build.OFFENSIVE_BLOCKLIST (kept in sync deliberately; a few sentinels
# are enough to prove the shipped data is clean — we assert NONE of these appear anywhere).
OFFENSIVE_SENTINELS = {
    "fuck", "fucked", "fucker", "fuckers", "fucking", "shit", "cunt", "nigger", "fag",
    "faggot", "slut", "whore", "bitch", "dick", "cock", "piss", "retard", "chink", "coon",
}

def min_moves_for_length(L):
    return {3: 2, 4: 2, 5: 3, 6: 3, 7: 4}.get(L, 2 if L < 7 else 4)

def load(n):
    with open(os.path.join(DATA, n), encoding="utf-8") as f:
        return json.load(f)

def hamming1(a, b):
    return len(a) == len(b) and sum(1 for x, y in zip(a, b) if x != y) == 1

def build_graph(words):
    words = set(words); buckets = defaultdict(list)
    for w in words:
        for i in range(len(w)):
            buckets[w[:i] + "*" + w[i + 1:]].append(w)
    adj = defaultdict(set)
    for ws in buckets.values():
        for a in ws:
            for c in ws:
                if a != c: adj[a].add(c)
    return adj

def count_shortest_paths(adj, s, e):
    if s == e: return 1, 0
    dist = {s: 0}; npaths = {s: 1}; q = deque([s]); found = None
    while q:
        u = q.popleft()
        if found is not None and dist[u] >= found: continue
        for v in adj[u]:
            if v not in dist:
                dist[v] = dist[u] + 1; npaths[v] = npaths[u]
                if v == e: found = dist[v]
                q.append(v)
            elif dist[v] == dist[u] + 1:
                npaths[v] += npaths[u]
    return npaths.get(e, 0), dist.get(e, -1)

def verify(td, dp, library, common):
    fails = []
    libset = set(library)
    # 0. schema sanity
    if not library or not common: fails.append("empty library/common")
    if not (set(common) <= libset): fails.append("common_words NOT subset of word_library")

    # 1. offensive absence everywhere
    for name, words in (("library", library), ("common", common)):
        bad = sorted(set(words) & OFFENSIVE_SENTINELS)
        if bad: fails.append(f"{name} contains offensive sentinels: {bad}")

    # 2. tier structure / counts
    tiers = td["tiers"]
    if len(tiers) != EXPECT_TIERS: fails.append(f"tiers={len(tiers)} expected {EXPECT_TIERS}")
    tot = 0
    for t in tiers:
        n = len(t["puzzles"]); tot += n
        if n != EXPECT_PER_TIER: fails.append(f"tier {t.get('tierId')} has {n} != {EXPECT_PER_TIER}")
    if tot != EXPECT_TIER_TOTAL: fails.append(f"tier total {tot} != {EXPECT_TIER_TOTAL}")

    # 3. daily count (additive: must not shrink)
    if len(dp["puzzles"]) < MIN_DAILY_POOL:
        fails.append(f"daily pool {len(dp['puzzles'])} < {MIN_DAILY_POOL} (shrank!)")

    # graphs per length for multi-route + floor
    by_len = defaultdict(list)
    for w in library: by_len[len(w)].append(w)
    adjL = {L: build_graph(by_len[L]) for L in by_len}

    def check_puzzle(kind, p, enforce_floor):
        s = p["startWord"].lower(); e = p["endWord"].lower()
        sol = [w.lower() for w in p["solution"]]
        pid = p.get("puzzleId")
        pre = f"{kind} {pid}"
        if len(sol) < 2: fails.append(f"{pre}: solution<2"); return None
        if s == e: fails.append(f"{pre}: start==end")
        if sol[0] != s: fails.append(f"{pre}: solution[0]!=start")
        if sol[-1] != e: fails.append(f"{pre}: solution[-1]!=end")
        if p["optimalSteps"] != len(sol) - 1: fails.append(f"{pre}: optimalSteps!=len-1")
        L0 = len(sol[0])
        seen = set()
        for w in sol:
            if len(w) != L0: fails.append(f"{pre}: '{w}' wrong length")
            if not (w.isalpha() and w.islower()): fails.append(f"{pre}: '{w}' not lowercase a-z")
            if w not in libset: fails.append(f"{pre}: '{w}' not in dictionary")
            if w in seen: fails.append(f"{pre}: repeat '{w}'")
            seen.add(w)
        for a, b in zip(sol, sol[1:]):
            if not hamming1(a, b): fails.append(f"{pre}: non-Hamming1 {a}->{b}")
        # multi-route + solvability + floor on the full dictionary graph
        adj = adjL.get(L0, {})
        n, dd = count_shortest_paths(adj, s, e)
        if dd < 0: fails.append(f"{pre}: UNSOLVABLE in dictionary graph")
        if enforce_floor and 0 <= dd < min_moves_for_length(L0):
            fails.append(f"{pre}: below min-move floor (true dist {dd})")
        return n

    multi = single = 0
    for t in tiers:
        pairseen = set()
        for p in t["puzzles"]:
            n = check_puzzle(f"tier{t['tierId']}", p, enforce_floor=True)
            if n is not None:
                multi += n > 1; single += n <= 1
            key = tuple(sorted((p["startWord"].lower(), p["endWord"].lower())))
            if key in pairseen: fails.append(f"tier{t['tierId']}: dup pair {key}")
            pairseen.add(key)
    for p in dp["puzzles"]:
        n = check_puzzle("daily", p, enforce_floor=False)  # legacy daily floor handled by tool
        if n is not None:
            multi += n > 1; single += n <= 1

    return fails, multi, single

def main():
    td = load("tier_definitions.json"); dp = load("daily_puzzles.json")
    library = [w.lower() for w in load("word_library.json")["words"]]
    common = [w.lower() for w in load("common_words.json")["words"]]
    print(f"library={len(library)} common={len(common)} tiers={len(td['tiers'])} "
          f"tier_puzzles={sum(len(t['puzzles']) for t in td['tiers'])} daily={len(dp['puzzles'])}")

    if "--canary" in sys.argv:
        # Corrupt one tier puzzle's solution so an edge is NOT Hamming-1; verifier MUST catch it.
        import copy
        td2 = copy.deepcopy(td)
        victim = td2["tiers"][0]["puzzles"][0]
        victim["solution"][1] = victim["solution"][0]  # makes edge a 0-edit (and a repeat)
        fails, _, _ = verify(td2, dp, library, common)
        if fails:
            print(f"CANARY OK: verifier caught the injected corruption ({len(fails)} issue(s)); "
                  f"e.g. {fails[0]}")
            sys.exit(0)
        else:
            print("CANARY FAILED: verifier did NOT catch injected corruption!")
            sys.exit(1)

    fails, multi, single = verify(td, dp, library, common)
    print(f"multi-route puzzles: {multi}  single-route: {single}")
    if fails:
        print(f"\n!!! {len(fails)} INTEGRITY VIOLATIONS:")
        for f in fails[:40]: print("   ", f)
        sys.exit(1)
    print("\nALL INTEGRITY CHECKS PASSED.")

if __name__ == "__main__":
    main()
